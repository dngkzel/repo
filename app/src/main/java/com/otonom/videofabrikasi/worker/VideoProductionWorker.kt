package com.otonom.videofabrikasi.worker

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import androidx.work.workDataOf
import com.otonom.videofabrikasi.data.LLMSettings
import com.otonom.videofabrikasi.data.SettingsRepository
import com.otonom.videofabrikasi.orchestrator.CloudProductionOrchestrator
import kotlinx.coroutines.flow.first

class VideoProductionWorker(
    private val appContext: Context,
    params: WorkerParameters
) : CoroutineWorker(appContext, params) {

    companion object {
        const val KEY_TOPIC = "topic"
        const val KEY_STATUS = "status"
        const val KEY_VIDEO_URL = "video_url"
    }

    override suspend fun doWork(): Result {
        val topic = inputData.getString(KEY_TOPIC)
            ?: return Result.failure(workDataOf(KEY_STATUS to "Konu eksik"))

        val settings = SettingsRepository(appContext).settingsFlow.first()
        val orchestrator = CloudProductionOrchestrator(appContext)

        var finalResult: Result = Result.failure()

        orchestrator.executePipeline(topic, settings) { status ->
            when {
                status.startsWith("DONE:") -> {
                    val videoUrl = status.removePrefix("DONE:")
                    finalResult = Result.success(
                        workDataOf(KEY_VIDEO_URL to videoUrl)
                    )
                }
                status.startsWith("ERROR:") -> {
                    finalResult = Result.failure(
                        workDataOf(KEY_STATUS to status.removePrefix("ERROR:"))
                    )
                }
            }
        }

        return finalResult
    }
}
