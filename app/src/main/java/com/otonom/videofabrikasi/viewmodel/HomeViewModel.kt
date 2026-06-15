package com.otonom.videofabrikasi.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.otonom.videofabrikasi.data.PipelineStatus
import com.otonom.videofabrikasi.data.SettingsRepository
import com.otonom.videofabrikasi.orchestrator.CloudProductionOrchestrator
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch

class HomeViewModel(application: Application) : AndroidViewModel(application) {

    private val settingsRepository = SettingsRepository(application)
    private val orchestrator = CloudProductionOrchestrator(application)

    private val _status = MutableStateFlow(PipelineStatus.IDLE)
    val status: StateFlow<PipelineStatus> = _status

    private val _statusMessage = MutableStateFlow("Hazır")
    val statusMessage: StateFlow<String> = _statusMessage

    private val _videoUrl = MutableStateFlow<String?>(null)
    val videoUrl: StateFlow<String?> = _videoUrl

    fun startPipeline(topic: String) {
        if (_status.value != PipelineStatus.IDLE && _status.value != PipelineStatus.DONE && _status.value != PipelineStatus.ERROR) return

        viewModelScope.launch {
            val settings = settingsRepository.settingsFlow.first()
            _videoUrl.value = null

            orchestrator.executePipeline(topic, settings) { update ->
                when {
                    update.startsWith("DONE:") -> {
                        _videoUrl.value = update.removePrefix("DONE:")
                        _status.value = PipelineStatus.DONE
                        _statusMessage.value = "Tamamlandı!"
                    }
                    update.startsWith("ERROR:") -> {
                        _status.value = PipelineStatus.ERROR
                        _statusMessage.value = "Hata: ${update.removePrefix("ERROR:")}"
                    }
                    update.contains("Senaryo") -> {
                        _status.value = PipelineStatus.FETCHING_SCRIPT
                        _statusMessage.value = update
                    }
                    update.contains("Ses") -> {
                        _status.value = PipelineStatus.GENERATING_MEDIA
                        _statusMessage.value = update
                    }
                    update.contains("render") -> {
                        _status.value = PipelineStatus.RENDERING
                        _statusMessage.value = update
                    }
                    update.contains("Sosyal") -> {
                        _status.value = PipelineStatus.PUBLISHING
                        _statusMessage.value = update
                    }
                }
            }
        }
    }
}
