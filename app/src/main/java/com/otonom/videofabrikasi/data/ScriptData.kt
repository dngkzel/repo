package com.otonom.videofabrikasi.data

data class ScriptData(
    val text: String,
    val prompts: List<String>,
    val hashtags: List<String>
)

enum class PipelineStatus {
    IDLE,
    FETCHING_SCRIPT,
    GENERATING_MEDIA,
    RENDERING,
    PUBLISHING,
    DONE,
    ERROR
}
