const functions = require("firebase-functions");
const admin = require("firebase-admin");
const ffmpeg = require("fluent-ffmpeg");
const ffmpegStatic = require("ffmpeg-static");
const path = require("path");
const os = require("os");
const fs = require("fs");

admin.initializeApp();
ffmpeg.setFfmpegPath(ffmpegStatic);

/**
 * renderVideo — Cloud Function (HTTPS Callable)
 *
 * Gelen ses URL'si ve görsel URL listesini alarak FFmpeg ile
 * slideshow video oluşturur ve Firebase Storage'a yükler.
 *
 * Giriş: { audioUrl: string, imageUrls: string[] }
 * Çıkış: { videoUrl: string }
 */
exports.renderVideo = functions
    .runWith({timeoutSeconds: 300, memory: "1GB"})
    .https.onCall(async (data, context) => {
        const {audioUrl, imageUrls} = data;

        if (!audioUrl || !Array.isArray(imageUrls) || imageUrls.length === 0) {
            throw new functions.https.HttpsError(
                "invalid-argument",
                "audioUrl ve imageUrls zorunludur",
            );
        }

        const tempVideoPath = path.join(
            os.tmpdir(),
            `output_${Date.now()}.mp4`,
        );

        await new Promise((resolve, reject) => {
            let command = ffmpeg();

            // Her görsel 5 saniye gösterilir
            imageUrls.forEach((url) => command.input(url).loop(5));
            command.input(audioUrl);

            command
                .outputOptions([
                    "-c:v libx264",
                    "-tune stillimage",
                    "-c:a aac",
                    "-b:a 192k",
                    "-pix_fmt yuv420p",
                    "-shortest",
                ])
                .save(tempVideoPath)
                .on("end", resolve)
                .on("error", (err) => reject(
                    new functions.https.HttpsError("internal", "FFmpeg hatası", err.message),
                ));
        });

        const bucket = admin.storage().bucket();
        const destination = `videos/output_${Date.now()}.mp4`;

        const [uploadedFile] = await bucket.upload(tempVideoPath, {
            destination,
            metadata: {contentType: "video/mp4"},
        });

        const [signedUrl] = await uploadedFile.getSignedUrl({
            action: "read",
            expires: "01-01-2030",
        });

        fs.unlinkSync(tempVideoPath);

        return {videoUrl: signedUrl};
    });
