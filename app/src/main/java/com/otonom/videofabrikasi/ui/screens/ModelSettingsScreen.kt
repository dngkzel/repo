package com.otonom.videofabrikasi.ui.screens

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import com.otonom.videofabrikasi.viewmodel.SettingsViewModel

@Composable
fun ModelSettingsScreen(viewModel: SettingsViewModel) {
    val settings by viewModel.settings.collectAsState()
    var tempModelName by remember(settings.modelName) { mutableStateOf(settings.modelName) }
    var tempApiKey by remember(settings.apiKey) { mutableStateOf(settings.apiKey) }
    var apiKeyVisible by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier
            .padding(16.dp)
            .fillMaxWidth()
    ) {
        Text(
            text = "Sistem Yapılandırması",
            style = MaterialTheme.typography.titleLarge
        )

        Spacer(modifier = Modifier.height(24.dp))

        OutlinedTextField(
            value = tempModelName,
            onValueChange = { tempModelName = it },
            label = { Text("Model Versiyonu") },
            placeholder = { Text("gemini-1.5-flash") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(12.dp))

        OutlinedTextField(
            value = tempApiKey,
            onValueChange = { tempApiKey = it },
            label = { Text("API Anahtarı") },
            singleLine = true,
            visualTransformation = if (apiKeyVisible) VisualTransformation.None else PasswordVisualTransformation(),
            trailingIcon = {
                IconButton(onClick = { apiKeyVisible = !apiKeyVisible }) {
                    Icon(
                        imageVector = if (apiKeyVisible) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                        contentDescription = if (apiKeyVisible) "Gizle" else "Göster"
                    )
                }
            },
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = { viewModel.saveSettings(tempModelName, tempApiKey) },
            enabled = tempModelName.isNotBlank() && tempApiKey.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Sistemi Güncelle")
        }

        Spacer(modifier = Modifier.height(16.dp))

        Text(
            text = "Desteklenen modeller: gemini-1.5-flash, gemini-1.5-pro",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
        )
    }
}
