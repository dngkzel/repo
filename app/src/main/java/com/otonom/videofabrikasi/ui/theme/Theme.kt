package com.otonom.videofabrikasi.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = NeonCyan,
    secondary = ElectricBlue,
    background = DeepBlue,
    surface = DarkSurface,
    onPrimary = DeepBlue,
    onSecondary = DeepBlue,
    onBackground = NeonCyan,
    onSurface = NeonCyan
)

private val LightColorScheme = lightColorScheme(
    primary = ElectricBlue,
    secondary = PurpleGrey40,
    tertiary = Pink40
)

@Composable
fun OtonomVideoTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
