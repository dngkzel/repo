package com.otonom.videofabrikasi.orchestrator

import android.content.Context
import android.speech.tts.TextToSpeech
import android.speech.tts.UtteranceProgressListener
import com.google.firebase.functions.FirebaseFunctions
import com.google.firebase.storage.FirebaseStorage
import com.otonom.videofabrikasi.data.LLMSettings
import com.otonom.videofabrikasi.data.ScriptData
import com.otonom.videofabrikasi.network.GeminiClient
import com.otonom.videofabrikasi.network.GeminiContent
import com.otonom.videofabrikasi.network.GeminiPart
import com.otonom.videofabrikasi.network.GeminiRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import java.io.File
import java.util.Locale
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

class CloudProductionOrchestrator(private val context: Context) {

    private val functions = FirebaseFunctions.getInstance()
    private val storage = FirebaseStorage.getInstance()

    // Üstel geciktirmeli yeniden deneme
    private suspend fun <T> retryWithBackoff(
        times: Int = 3,
        initialDelay: Long = 2000,
        factor: Double = 2.0,
        block: suspend () -> T
    ): T {
        var currentDelay = initialDelay
        repeat(times - 1) {
            try {
                return block()
            } catch (e: Exception) {
                delay(currentDelay)
                currentDelay = (currentDelay * factor).toLong()
            }
        }
        return block()
    }

    suspend fun executePipeline(
        topic: String,
        settings: LLMSettings,
        onStatusUpdate: (String) -> Unit
    ) = withContext(Dispatchers.IO) {
        try {
            // 1. Gemini'den senaryo al
            onStatusUpdate("Senaryo üretiliyor…")
            val script = retryWithBackoff { getScriptFromGemini(topic, settings) }

            // 2. Ses ve görsel üretimini paralel yürüt
            onStatusUpdate("Ses ve görseller üretiliyor…")
            val (audioUrl, imageUrls) = coroutineScope {
                val audioDeferred = async { generateAndroidTTSAndUpload(script.text) }
                val imagesDeferred = async { retryWithBackoff { generateImagesAndUpload(script.prompts) } }
                Pair(audioDeferred.await(), imagesDeferred.await())
            }

            // 3. Bulut render
            onStatusUpdate("Bulut render başlatıldı…")
            val videoUrl = retryWithBackoff { triggerCloudRender(audioUrl, imageUrls) }

            // 4. Sosyal medyaya yayınla
            onStatusUpdate("Sosyal medyaya yayınlanıyor…")
            retryWithBackoff { publishToSocials(videoUrl, script.hashtags) }

            onStatusUpdate("DONE:$videoUrl")

        } catch (e: Exception) {
            onStatusUpdate("ERROR:${e.message}")
        }
    }

    // Gemini API'ye bağlanarak senaryo, görsel promptları ve hashtag'ler alır
    private suspend fun getScriptFromGemini(topic: String, settings: LLMSettings): ScriptData {
        val prompt = """
            Konu: "$topic"

            Aşağıdaki JSON formatında bir sosyal medya videosu için içerik üret:
            {
              "text": "<60 saniyelik anlatı metni>",
              "prompts": ["<görsel 1 için İngilizce prompt>", "<görsel 2>", "<görsel 3>"],
              "hashtags": ["#etiket1", "#etiket2", "#etiket3"]
            }
            Yalnızca JSON döndür, başka metin ekleme.
        """.trimIndent()

        val request = GeminiRequest(
            contents = listOf(GeminiContent(parts = listOf(GeminiPart(text = prompt))))
        )

        val response = GeminiClient.service.generateContent(
            model = settings.modelName,
            apiKey = settings.apiKey,
            request = request
        )

        val rawJson = response.candidates
            ?.firstOrNull()
            ?.content
            ?.parts
            ?.firstOrNull()
            ?.text
            ?: throw IllegalStateException("Gemini boş yanıt döndürdü")

        return parseScriptJson(rawJson)
    }

    private fun parseScriptJson(json: String): ScriptData {
        val gson = com.google.gson.Gson()
        return gson.fromJson(json.trim(), ScriptData::class.java)
    }

    // Android TTS ile ses üretip Firebase Storage'a yükler
    private suspend fun generateAndroidTTSAndUpload(text: String): String =
        suspendCancellableCoroutine { continuation ->
            var tts: TextToSpeech? = null
            tts = TextToSpeech(context) { status ->
                if (status == TextToSpeech.ERROR) {
                    continuation.resumeWithException(IllegalStateException("TTS başlatılamadı"))
                    return@TextToSpeech
                }

                tts?.language = Locale("tr", "TR")

                val outputFile = File(context.cacheDir, "tts_${System.currentTimeMillis()}.wav")
                val utteranceId = "tts_upload"

                tts?.setOnUtteranceProgressListener(object : UtteranceProgressListener() {
                    override fun onStart(utteranceId: String?) {}
                    override fun onDone(utteranceId: String?) {
                        uploadFileToStorage(outputFile, "audio") { url ->
                            tts?.shutdown()
                            outputFile.delete()
                            continuation.resume(url)
                        }
                    }
                    override fun onError(utteranceId: String?) {
                        tts?.shutdown()
                        continuation.resumeWithException(IllegalStateException("TTS sentezi başarısız"))
                    }
                })

                tts?.synthesizeToFile(text, null, outputFile, utteranceId)
            }
        }

    // Görsel URL'lerini Firebase Storage'a yükler (üretim için Imagen/Stable Diffusion API eklenecek)
    private suspend fun generateImagesAndUpload(prompts: List<String>): List<String> {
        // V1.0: Placeholder görseller döndürülür. Gerçek entegrasyon için
        // Google Imagen veya başka bir görsel API'si kullanılacak.
        return prompts.mapIndexed { index, _ ->
            "https://picsum.photos/seed/${index + System.currentTimeMillis()}/1280/720"
        }
    }

    private suspend fun triggerCloudRender(audioUrl: String, imageUrls: List<String>): String =
        suspendCancellableCoroutine { continuation ->
            val data = hashMapOf("audioUrl" to audioUrl, "imageUrls" to imageUrls)
            functions.getHttpsCallable("renderVideo").call(data)
                .addOnSuccessListener { result ->
                    @Suppress("UNCHECKED_CAST")
                    val resultMap = result.data as Map<String, Any>
                    continuation.resume(resultMap["videoUrl"] as String)
                }
                .addOnFailureListener { continuation.resumeWithException(it) }
        }

    // Sosyal medya yayınlama — platforma özel API entegrasyonu burada yapılır
    private suspend fun publishToSocials(videoUrl: String, hashtags: List<String>) {
        // V1.0: Log çıktısı. Instagram Graph API ve YouTube Data API v3
        // entegrasyonu için ilgili OAuth akışı ve endpoint çağrıları eklenecek.
        android.util.Log.d("OtonomVideo", "Yayınlanıyor: $videoUrl | ${hashtags.joinToString()}")
    }

    private fun uploadFileToStorage(file: File, folder: String, onSuccess: (String) -> Unit) {
        val ref = storage.reference.child("$folder/${file.name}")
        ref.putFile(android.net.Uri.fromFile(file))
            .continueWithTask { task ->
                if (!task.isSuccessful) throw task.exception!!
                ref.downloadUrl
            }
            .addOnSuccessListener { uri -> onSuccess(uri.toString()) }
    }
}
