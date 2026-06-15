# Otonom Video Fabrikası — CLAUDE.md

Android uygulamasının genel yapısı, geliştirme akışları ve AI asistanları için kurallar.

## Proje Özeti

Konu alarak otomatik video üreten Android uygulaması. Pipeline:
1. Gemini LLM → senaryo + görsel promptları + hashtag'ler
2. Android TTS → ses dosyası → Firebase Storage
3. Görsel API (placeholder/Imagen) → Firebase Storage
4. Firebase Cloud Function (`renderVideo`) → FFmpeg ile MP4
5. Sosyal medya yayını (Instagram/YouTube)

## Dizin Yapısı

```
app/src/main/java/com/otonom/videofabrikasi/
├── data/               # Veri modelleri ve DataStore repository
├── network/            # Retrofit Gemini API istemcisi
├── orchestrator/       # Pipeline koordinatörü (retryWithBackoff)
├── ui/
│   ├── screens/        # Compose ekranları (Home, Settings)
│   └── theme/          # Material3 tema (Color, Type, Theme)
├── viewmodel/          # HomeViewModel, SettingsViewModel
├── worker/             # WorkManager arka plan görevi
└── MainActivity.kt     # Navigation + Scaffold

functions/              # Firebase Cloud Functions (Node.js 20)
├── index.js            # renderVideo callable function
└── package.json
```

## Paket Adı

`com.otonom.videofabrikasi` — tüm sınıflarda bu namespace kullanılır.

## Temel Sınıflar

| Dosya | Sorumluluk |
|---|---|
| `CloudProductionOrchestrator` | Pipeline yönetimi, exponential backoff |
| `SettingsRepository` | DataStore ile kalıcı LLM ayarları |
| `HomeViewModel` | Pipeline durumu (StateFlow) |
| `SettingsViewModel` | Model adı ve API anahtarı |
| `VideoProductionWorker` | WorkManager ile arka plan çalışma |
| `GeminiClient` | Retrofit singleton |

## Geliştirme Kuralları

- Tüm ağ çağrıları `Dispatchers.IO` üzerinde, UI güncellemeleri `StateFlow` ile
- Yeni API entegrasyonları `CloudProductionOrchestrator` içine eklenir
- Hata mesajları `ERROR:` prefixi, başarı `DONE:` prefixi ile gönderilir
- `retryWithBackoff` her dış servis çağrısını sarar

## Kurulum Adımları

1. `google-services.json.example` dosyasını kopyala → `app/google-services.json`
2. Firebase Console'dan Storage ve Functions'ı etkinleştir (Blaze planı)
3. Google AI Studio'dan Gemini API anahtarı al
4. `cd functions && npm install && firebase deploy --only functions`
5. Uygulamayı aç → Ayarlar → model adı + API anahtarını gir

## TODO (V2 için)

- [ ] Google Imagen API ile gerçek görsel üretimi
- [ ] Instagram Graph API OAuth akışı
- [ ] YouTube Data API v3 upload endpoint
- [ ] Firebase Authentication ile kullanıcı girişi
- [ ] Jetpack DataStore → gizli API anahtarı için EncryptedSharedPreferences
