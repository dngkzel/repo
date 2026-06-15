package com.otonom.videofabrikasi.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.otonom.videofabrikasi.data.LLMSettings
import com.otonom.videofabrikasi.data.SettingsRepository
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class SettingsViewModel(application: Application) : AndroidViewModel(application) {

    private val repository = SettingsRepository(application)

    val settings: StateFlow<LLMSettings> = repository.settingsFlow
        .stateIn(
            scope = viewModelScope,
            started = SharingStarted.WhileSubscribed(5_000),
            initialValue = LLMSettings()
        )

    fun saveSettings(modelName: String, apiKey: String) {
        viewModelScope.launch {
            repository.saveSettings(LLMSettings(modelName = modelName, apiKey = apiKey))
        }
    }
}
