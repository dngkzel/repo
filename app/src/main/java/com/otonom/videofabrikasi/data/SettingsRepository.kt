package com.otonom.videofabrikasi.data

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "llm_settings")

class SettingsRepository(private val context: Context) {

    private object Keys {
        val MODEL_NAME = stringPreferencesKey("model_name")
        val API_KEY = stringPreferencesKey("api_key")
    }

    val settingsFlow: Flow<LLMSettings> = context.dataStore.data.map { prefs ->
        LLMSettings(
            modelName = prefs[Keys.MODEL_NAME] ?: "gemini-1.5-flash",
            apiKey = prefs[Keys.API_KEY] ?: ""
        )
    }

    suspend fun saveSettings(settings: LLMSettings) {
        context.dataStore.edit { prefs ->
            prefs[Keys.MODEL_NAME] = settings.modelName
            prefs[Keys.API_KEY] = settings.apiKey
        }
    }
}
