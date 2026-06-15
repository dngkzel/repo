# Football Manager – Unity Kurulum Rehberi (Türkçe)

Bu rehber, GitHub reposunu Unity'ye sıfırdan eklemek ve çalışır hale getirmek için adım adım talimatlar içerir.

---

## İçindekiler
1. [Gereksinimler](#1-gereksinimler)
2. [Repoyu Bilgisayara Çekme](#2-repoyu-bilgisayara-çekme)
3. [Unity Hub'a Proje Ekleme](#3-unity-huba-proje-ekleme)
4. [Paket Kurulumları](#4-paket-kurulumları)
5. [Firebase Kurulumu](#5-firebase-kurulumu)
6. [Google Sign-In Plugin](#6-google-sign-in-plugin)
7. [Sahneleri Build Settings'e Ekleme](#7-sahneleri-build-settingse-ekleme)
8. [GameManager GameObject Kurulumu](#8-gamemanager-gameobject-kurulumu)
9. [Sahne İçi Inspector Bağlantıları](#9-sahne-i̇çi-inspector-bağlantıları)
10. [Android Build Ayarları](#10-android-build-ayarları)
11. [İlk Build Alma](#11-i̇lk-build-alma)
12. [Sık Karşılaşılan Hatalar](#12-sık-karşılaşılan-hatalar)

---

## 1. Gereksinimler

Başlamadan önce aşağıdakilerin kurulu olduğundan emin ol:

| Program | Versiyon | İndirme |
|---------|----------|---------|
| Unity Hub | Son sürüm | unity.com/download |
| Unity Editor | **2020.3 LTS** veya üzeri | Unity Hub üzerinden |
| Git | Son sürüm | git-scm.com |
| Android Build Support | ✓ | Unity Hub → Installs → Modüller |
| JDK | 11 | Unity ile birlikte gelir |
| Firebase Unity SDK | 11.x | Firebase Console'dan |
| Google Sign-In Plugin | 1.0.4+ | GitHub'dan |

> **Not:** Unity'yi kurarken **Android Build Support**, **Android SDK & NDK Tools** ve **OpenJDK** modüllerini seç.

---

## 2. Repoyu Bilgisayara Çekme

### Yöntem A — Git Komut Satırı (Önerilen)

```bash
# 1. İstediğin bir klasöre git
cd Belgeler

# 2. Repoyu klonla
git clone https://github.com/dngkzel/repo.git FootballManager

# 3. Proje klasörüne gir
cd FootballManager

# 4. Güncel branch'e geç
git checkout claude/2d-football-game-play-0we9ft
```

### Yöntem B — GitHub Desktop

1. GitHub Desktop'ı aç
2. **File → Clone Repository** → URL sekmesi
3. URL: `https://github.com/dngkzel/repo.git`
4. Yerel klasör seç → **Clone**
5. Sol üstteki branch menüsünden `claude/2d-football-game-play-0we9ft` seç

### Yöntem C — ZIP İndir

1. GitHub'da **Code → Download ZIP**
2. ZIP'i aç → istediğin klasöre koy

> Branch: **claude/2d-football-game-play-0we9ft** — tüm kodlar bu branch'te.

---

## 3. Unity Hub'a Proje Ekleme

1. **Unity Hub**'ı aç
2. Sol menüden **Projects** → sağ üstte **Add** (veya **Open**) butonuna tıkla
3. Az önce klonladığın klasörü seç (içinde `Assets/`, `Packages/`, `ProjectSettings/` klasörleri olan yer)
4. **Unity 2020.3 LTS** (veya kurulu versiyonun) seçili olduğunu kontrol et
5. **Open** → Unity açılacak ve paketleri otomatik indirecek

> İlk açılışta Unity birkaç dakika bekleyebilir — bu normal. "Compiling Scripts" yazısı kaybolana kadar bekle.

---

## 4. Paket Kurulumları

`Packages/manifest.json` dosyası zaten hazır, ancak aşağıdakileri Unity içinde doğrula:

### Otomatik Gelen Paketler (manifest.json'da mevcut)
- `com.unity.textmeshpro` 3.0.6
- `com.unity.purchasing` 4.11.0
- `com.unity.addressables` 1.21.21
- `com.unity.nuget.newtonsoft-json` 3.2.1
- `com.unity.inputsystem` 1.7.0

### Kontrol Etmek İçin
1. **Window → Package Manager**
2. Sol üstten **In Project** seç
3. Yukarıdaki paketlerin listelendiğini gör

### TextMeshPro Kurulumu (Zorunlu)
1. **Window → TextMeshPro → Import TMP Essential Resources**
2. Açılan pencerede **Import** butonuna tıkla
3. Bitince tekrar **Window → TextMeshPro → Import TMP Examples & Extras** → Import

---

## 5. Firebase Kurulumu

### 5.1 Firebase Projesi Oluştur

1. [console.firebase.google.com](https://console.firebase.google.com) adresine git
2. **Add Project** → Proje adı: `FootballManager` → Devam et
3. Google Analytics'i istersen aç/kapat → **Create Project**

### 5.2 Android Uygulaması Ekle

1. Proje sayfasında Android simgesine tıkla
2. **Android package name:** `com.yourcompany.footballmanager`
   > ⚠️ Bu ismi `Edit → Project Settings → Player → Android → Package Name` ile aynı yapacaksın
3. **App nickname:** Football Manager
4. **SHA-1** (isteğe bağlı, Google Sign-In için zorunlu): Aşağıdaki komutla al:
   ```bash
   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
   ```
5. **Register App** → **Download google-services.json**

### 5.3 google-services.json'ı Projeye Ekle

İndirilen `google-services.json` dosyasını:
```
FootballManager/Assets/google-services.json
```
konumuna kopyala (Assets klasörünün doğrudan içine).

### 5.4 Firebase Servislerini Aç

Firebase Console'da:

**Authentication:**
1. Sol menü → **Authentication → Get Started**
2. **Sign-in method** sekmesi
3. **Email/Password** → Enable → Save
4. **Google** → Enable → **Web client ID**'yi kopyala (sonra lazım olacak)

**Realtime Database:**
1. Sol menü → **Realtime Database → Create Database**
2. **Start in test mode** seç → Next → **Enable**
3. Database URL'ini not al: `https://your-project-default-rtdb.firebaseio.com/`

**Firestore:**
1. Sol menü → **Firestore Database → Create Database**
2. **Start in test mode** → Next → **Enable**

### 5.5 Firebase Unity SDK'yı İndir ve Kur

1. [firebase.google.com/download/unity](https://firebase.google.com/download/unity) adresine git
2. `firebase_unity_sdk_11.x.x.zip` dosyasını indir ve aç
3. Unity'de **Assets → Import Package → Custom Package**
4. Şu dosyaları **sırayla** import et:
   - `FirebaseAuth.unitypackage`
   - `FirebaseDatabase.unitypackage`
   - `FirebaseFirestore.unitypackage`
5. Her birinde açılan pencerede **Import** butonuna tıkla
6. Import bittikten sonra:
   **Assets → External Dependency Manager → Android Resolver → Force Resolve**

> ⚠️ Resolve işlemi 1-2 dakika sürebilir. Console'da hata yoksa devam et.

---

## 6. Google Sign-In Plugin

### 6.1 Plugin'i İndir

1. [github.com/googlesamples/google-signin-unity/releases](https://github.com/googlesamples/google-signin-unity/releases) adresine git
2. `google-signin-plugin-1.0.4.unitypackage` dosyasını indir

### 6.2 Unity'ye Ekle

1. **Assets → Import Package → Custom Package**
2. İndirilen `.unitypackage` dosyasını seç → **Open → Import**

### 6.3 Web Client ID Ayarı

1. Unity'de Hierarchy'de veya Prefab'de `AuthManager` bileşenini bul
2. Inspector'da **Google Web Client Id** alanına Firebase Console'dan kopyaladığın Web Client ID'yi yapıştır
   - Format: `123456789-xxxx.apps.googleusercontent.com`

---

## 7. Sahneleri Build Settings'e Ekleme

1. **File → Build Settings**
2. Platform olarak **Android** seçili değilse seç → **Switch Platform** (birkaç dakika sürer)
3. **Scenes In Build** listesinin aşağıdaki sırayla olmasını sağla:

| Index | Sahne Adı | Dosya Yolu |
|-------|-----------|------------|
| 0 | Loading | `Assets/Scenes/Loading.unity` |
| 1 | Authentication | `Assets/Scenes/Authentication.unity` |
| 2 | Registration | `Assets/Scenes/Registration.unity` |
| 3 | MainMenu | `Assets/Scenes/MainMenu.unity` |
| 4 | Match | `Assets/Scenes/Match.unity` |
| 5 | Market | `Assets/Scenes/Market.unity` |
| 6 | Rankings | `Assets/Scenes/Rankings.unity` |
| 7 | Settings | `Assets/Scenes/Settings.unity` |
| 8 | DailyReward | `Assets/Scenes/DailyReward.unity` |

**Sahne Eklemek İçin:**
- Her `.unity` dosyasını Project penceresinden **Scenes In Build** listesine sürükle
- Veya sahneyi aç → **Add Open Scenes** butonuna tıkla
- Yukarı/aşağı okla sıralamayı düzenle

> ⚠️ Sahne adları büyük/küçük harf dahil tam olarak yukarıdaki gibi olmalı. `SceneName` enum'u ile eşleşmeli.

---

## 8. GameManager GameObject Kurulumu

**Loading** sahnesine bir boş GameObject oluştur ve ona tüm sistem bileşenlerini ekle:

1. **Loading** sahnesini aç
2. Hierarchy'de sağ tıkla → **Create Empty**
3. Adını `GameManager` koy
4. Bu GameObject'e aşağıdaki bileşenleri ekle (**Add Component** butonu ile):

| Bileşen | Script Dosyası |
|---------|---------------|
| `GameManager` | Core/GameManager.cs |
| `DataManager` | Core/DataManager.cs |
| `LocalizationManager` | Core/LocalizationManager.cs |
| `AudioManager` | Audio/AudioManager.cs |
| `TokenSystem` | Economy/TokenSystem.cs |
| `DailyRewardSystem` | Economy/DailyRewardSystem.cs |
| `RankingSystem` | Ranking/RankingSystem.cs |
| `PremiumSystem` | Economy/PremiumSystem.cs |
| `IAPManager` | Economy/IAPManager.cs |
| `MarketSystem` | Economy/MarketSystem.cs |
| `TransferSystem` | Player/TransferSystem.cs |
| `RegistrationManager` | Authentication/RegistrationManager.cs |
| `AuthManager` | Authentication/AuthManager.cs |
| `GameSceneManager` | Core/GameSceneManager.cs |
| `AudioSource` (SFX için) | Unity built-in |
| `AudioSource` (Müzik için) | Unity built-in |

### AudioManager Inspector Ayarları

`AudioManager` bileşeninde:
1. **Sfx Source** alanına → SFX için oluşturduğun AudioSource'u sürükle
2. **Music Source** alanına → Müzik için oluşturduğun AudioSource'u sürükle
3. Ses dosyalarını (`.wav`, `.mp3`) Project penceresinden ilgili alanlara sürükle:
   - `sfxGoal`, `sfxWhistle`, `sfxButtonClick`, `sfxGoal` vb.

### AuthManager Inspector Ayarı

`AuthManager` bileşeninde:
- **Google Web Client Id** → Firebase Console'dan aldığın Web Client ID

### Prefab Olarak Kaydet (Önerilir)

1. Hierarchy'deki `GameManager` GameObject'ini Project penceresindeki bir klasöre sürükle
2. Böylece diğer sahnelerde de kullanabilirsin (ama DontDestroyOnLoad zaten taşıyor)

---

## 9. Sahne İçi Inspector Bağlantıları

Her sahneyi aç ve UI bileşenlerini Inspector'dan bağla.

### Authentication Sahnesi
- `AuthUI` bileşenine gir
- Her `TMP_InputField`, `Button`, `TextMeshProUGUI` objesini ilgili alana sürükle
- Login, Register, Reset panellerini bağla

### MainMenu Sahnesi
- `MainMenuUI` bileşenine gir
- `txtDisplayName`, `txtTeamName`, `txtTokenBalance`, `txtRank` → TextMeshProUGUI objeleri
- `btnPlay`, `btnMarket`, `btnRankings`, `btnSettings`, `btnDailyReward`, `btnLogout` → Button objeleri
- `dailyRewardDot` → Bildirim noktası GameObject'i
- `imgPremiumBadge` → Premium rozet Image'ı

### Match Sahnesi
Match sahnesine ekstra 2 GameObject daha koy:
- `MatchController` bileşeni olan bir GameObject
- `MatchSimulator` bileşeni olan bir GameObject
- `MatchSetup` bileşeni olan bir GameObject

`MatchUI` bileşenine:
- Skor, dakika, takım adı Text'lerini bağla
- Yorum listesi Container ve Prefab'ını bağla
- Pause, Speed butonlarını bağla
- Substitution Panel'i bağla
- Result Panel'i bağla

### Market Sahnesi
`MarketUI` bileşenine:
- Token bakiye Text'i
- Refresh butonu ve maliyet Text'i
- Tier ve Pozisyon Dropdown'ları
- Liste Container ve Card Prefab'ı
- Confirm Panel ve butonları

### Prefab Gereksinimleri

**PlayerCard Prefab** (Market için):
- Root'ta `Button` ve `Image` bileşeni
- 5 adet `TextMeshProUGUI` child: Ad, Pozisyon+OVR, PAC/SHO/PAS, DRI/DEF/PHY, Fiyat

**PlayerRow Prefab** (Match substitution için):
- Root'ta `Button` ve `Image` bileşeni
- 3 adet `TextMeshProUGUI` child: Ad, Pozisyon, Overall

**CommentaryItem Prefab** (Match yorumları için):
- 1 adet `TextMeshProUGUI` child

**RankRow Prefab** (Rankings için):
- 6 adet `TextMeshProUGUI` child: Sıra, Ad, Takım, Puan, G/B/M, GF:GA

**DaySlot** (DailyReward için):
- 2 adet `TextMeshProUGUI`: gün etiketi ve ödül
- 2 adet `GameObject`: claimed overlay ve active glow

---

## 10. Android Build Ayarları

### Player Settings
**Edit → Project Settings → Player → Android sekmesi:**

| Ayar | Değer |
|------|-------|
| Company Name | `com.yourcompany` |
| Product Name | `Football Manager` |
| Package Name | `com.yourcompany.footballmanager` |
| Minimum API Level | **API 24 (Android 7.0)** |
| Target API Level | API 33 veya üzeri |
| Scripting Backend | **IL2CPP** |
| Target Architectures | ✓ ARMv7 + ✓ ARM64 |
| Internet Access | **Required** |

### AndroidManifest.xml Ekle

`Assets/Plugins/Android/` klasörünü oluştur (yoksa) ve içine `AndroidManifest.xml` koy:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
          package="com.yourcompany.footballmanager">
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <application
        android:label="Football Manager"
        android:icon="@mipmap/ic_launcher"
        android:theme="@style/UnityThemeSelector">
        <activity
            android:name="com.unity3d.player.UnityPlayerActivity"
            android:screenOrientation="portrait"
            android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"
            android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>
        <meta-data android:name="com.google.android.gms.version"
                   android:value="@integer/google_play_services_version" />
    </application>
</manifest>
```

---

## 11. İlk Build Alma

### Test Build (APK)
1. **File → Build Settings → Android**
2. **Development Build** işaretli olsun
3. Cihazını USB ile bağla (USB Debugging açık)
4. **Build And Run** → APK oluşacak ve cihaza yüklenecek

### Release Build (AAB — Google Play için)
1. **Edit → Project Settings → Player → Android → Publishing Settings**
2. **Custom Keystore** aç → yeni keystore oluştur (şifreyi kaydet!)
3. **File → Build Settings → Build** → `.aab` dosyası oluşacak
4. Bu `.aab` dosyasını Google Play Console'a yükle

---

## 12. Sık Karşılaşılan Hatalar

### "Assembly has reference to non-existent assembly 'Firebase.Auth'"
**Çözüm:** Firebase SDK import edilmemiş.
→ Adım 5.5'i tekrar yap

### "The type or namespace name 'Google' could not be found"
**Çözüm:** Google Sign-In plugin eksik.
→ Adım 6.2'yi tekrar yap

### "NullReferenceException: FirebaseApp"
**Çözüm:** `google-services.json` eksik veya yanlış yerde.
→ Dosyanın `Assets/google-services.json` konumunda olduğunu kontrol et

### "Unable to resolve packages"
**Çözüm:** Unity Hub internet bağlantısı problemi.
→ **Edit → Project Settings → Package Manager** → cache temizle
→ Veya `Library/` klasörünü sil, Unity'yi yeniden aç

### "Scene 'Match' couldn't be loaded"
**Çözüm:** Sahne Build Settings'e eklenmemiş.
→ Adım 7'deki sıralamayı kontrol et, sahne adlarının tam eşleştiğini doğrula

### "Could not find stored account information" (Google Sign-In)
**Çözüm:** SHA-1 parmak izi Firebase'e eklenmemiş.
→ Firebase Console → Authentication → Settings → SHA certificate fingerprints → Add

### Android Resolver Hatası
**Çözüm:**
1. **Assets → External Dependency Manager → Android Resolver → Delete Resolved Libraries**
2. **Assets → External Dependency Manager → Android Resolver → Force Resolve**

### IL2CPP Build Hatası
**Çözüm:** NDK eksik.
→ Unity Hub → Installs → Unity versiyonunun yanındaki üç nokta → **Add Modules** → Android NDK kurulu olduğunu kontrol et

---

## Hızlı Kontrol Listesi

Build almadan önce şunları kontrol et:

- [ ] `Assets/google-services.json` mevcut
- [ ] Firebase Auth: Email/Password ve Google açık
- [ ] AuthManager Inspector'da Web Client ID dolu
- [ ] `Assets/Resources/Localization/` içinde 9 JSON dosyası var (en, tr, es, de, fr, it, pt, ar, th)
- [ ] 9 sahne Build Settings'te doğru sırada
- [ ] GameManager GameObject'inde tüm bileşenler var
- [ ] AudioManager'da sfxSource ve musicSource dolu
- [ ] TextMeshPro Essential Resources import edildi
- [ ] Minimum API Level = 24
- [ ] Scripting Backend = IL2CPP
- [ ] Package Name tüm yerlerde aynı
- [ ] Firebase Resolver hatasız tamamlandı

---

## Destek

Sorun yaşarsan:
- Firebase sorunları: [firebase.google.com/docs/unity](https://firebase.google.com/docs/unity)
- Unity IAP sorunları: [docs.unity3d.com/Packages/com.unity.purchasing](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0)
- Google Sign-In: [github.com/googlesamples/google-signin-unity](https://github.com/googlesamples/google-signin-unity)
