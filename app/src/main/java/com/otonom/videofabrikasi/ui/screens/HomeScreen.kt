package com.otonom.videofabrikasi.ui.screens

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Error
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.otonom.videofabrikasi.data.PipelineStatus
import com.otonom.videofabrikasi.viewmodel.HomeViewModel

@Composable
fun HomeScreen(viewModel: HomeViewModel) {
    val status by viewModel.status.collectAsState()
    val statusMessage by viewModel.statusMessage.collectAsState()
    val videoUrl by viewModel.videoUrl.collectAsState()
    var topic by remember { mutableStateOf("") }

    val isRunning = status !in listOf(PipelineStatus.IDLE, PipelineStatus.DONE, PipelineStatus.ERROR)

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(
            text = "Video Fabrikası",
            style = MaterialTheme.typography.headlineMedium
        )

        OutlinedTextField(
            value = topic,
            onValueChange = { topic = it },
            label = { Text("Video konusu") },
            placeholder = { Text("ör: Yapay zekanın geleceği") },
            enabled = !isRunning,
            singleLine = true,
            modifier = Modifier.fillMaxWidth()
        )

        Button(
            onClick = { viewModel.startPipeline(topic.trim()) },
            enabled = topic.isNotBlank() && !isRunning,
            modifier = Modifier.fillMaxWidth()
        ) {
            Icon(
                imageVector = Icons.Default.PlayArrow,
                contentDescription = null,
                modifier = Modifier.padding(end = 8.dp)
            )
            Text("Üretimi Başlat")
        }

        AnimatedVisibility(visible = isRunning || status == PipelineStatus.DONE || status == PipelineStatus.ERROR) {
            PipelineStatusCard(status = status, message = statusMessage, videoUrl = videoUrl)
        }
    }
}

@Composable
private fun PipelineStatusCard(
    status: PipelineStatus,
    message: String,
    videoUrl: String?
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 8.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        when (status) {
            PipelineStatus.DONE -> {
                Icon(
                    imageVector = Icons.Default.CheckCircle,
                    contentDescription = "Tamamlandı",
                    tint = Color(0xFF4CAF50),
                    modifier = Modifier.size(48.dp)
                )
                Text(
                    text = "Video Hazır!",
                    style = MaterialTheme.typography.titleMedium,
                    color = Color(0xFF4CAF50)
                )
                videoUrl?.let {
                    Text(
                        text = it,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                    )
                }
            }

            PipelineStatus.ERROR -> {
                Icon(
                    imageVector = Icons.Default.Error,
                    contentDescription = "Hata",
                    tint = MaterialTheme.colorScheme.error,
                    modifier = Modifier.size(48.dp)
                )
                Text(
                    text = message,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.error
                )
            }

            else -> {
                val infiniteTransition = rememberInfiniteTransition(label = "spinner")
                val rotation by infiniteTransition.animateFloat(
                    initialValue = 0f,
                    targetValue = 360f,
                    animationSpec = infiniteRepeatable(
                        animation = tween(1000, easing = LinearEasing),
                        repeatMode = RepeatMode.Restart
                    ),
                    label = "rotation"
                )

                CircularProgressIndicator(
                    modifier = Modifier.size(48.dp).rotate(rotation),
                    color = MaterialTheme.colorScheme.primary
                )
                Text(
                    text = message,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }
    }
}
