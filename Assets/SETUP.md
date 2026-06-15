# Football Manager – Unity Setup Guide

Complete step-by-step instructions for configuring the project from a fresh Unity install to a build-ready Android APK.

---

## Table of Contents
1. [Prerequisites](#1-prerequisites)
2. [Unity Project Settings](#2-unity-project-settings)
3. [Package Manager – Required Packages](#3-package-manager--required-packages)
4. [Firebase Setup](#4-firebase-setup)
5. [Google Sign-In Plugin](#5-google-sign-in-plugin)
6. [Unity IAP Setup](#6-unity-iap-setup)
7. [TextMeshPro Setup](#7-textmeshpro-setup)
8. [Scene Setup](#8-scene-setup)
9. [Persistent GameManager GameObject](#9-persistent-gamemanager-gameobject)
10. [Scene-by-Scene Inspector Wiring](#10-scene-by-scene-inspector-wiring)
11. [Localization JSON Files](#11-localization-json-files)
12. [Android Build Settings](#12-android-build-settings)
13. [AndroidManifest.xml](#13-androidmanifestxml)
14. [Firebase Security Rules](#14-firebase-security-rules)
15. [Google Play Console](#15-google-play-console)
16. [Quick Checklist Before Build](#16-quick-checklist-before-build)

---

## 1. Prerequisites

| Tool | Version |
|------|---------|
| Unity | 2020.3 LTS or newer |
| Android Build Support module | ✓ (install via Unity Hub) |
| Android SDK / NDK / JDK | Bundled with Unity or system |
| Firebase Unity SDK | 11.x (download from Firebase console) |
| Google Sign-In Unity Plugin | 1.0.4+ |
| JDK | 11 (required for Firebase) |

---

## 2. Unity Project Settings

### Player Settings (Edit → Project Settings → Player)

**Android tab:**
- Company Name: `com.yourcompany`
- Product Name: `FootballManager`
- Package Name: `com.yourcompany.footballmanager`
- Minimum API Level: **24** (Android 7.0)
- Target API Level: **33** (Android 13) or latest
- Scripting Backend: **IL2CPP**
- Target Architectures: ✓ ARMv7, ✓ ARM64
- Internet Access: **Required**
- Write Permission: **External (SD Card)** (for saves)
- Active Input Handling: **Input System Package (New)** or **Both**

**Other Settings:**
- Color Space: **Linear**
- Graphics APIs: **OpenGLES3** (remove Vulkan if targeting wide device range)
- Scripting Define Symbols: _(none required by default)_

### Quality Settings
- Pixel Light Count: 2
- Texture Quality: Full Res
- Anisotropic Textures: Per Texture
- Anti Aliasing: 2x Multi Sampling

---

## 3. Package Manager – Required Packages

Open **Window → Package Manager**.

### Via Package Manager (Unity Registry):
| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.purchasing` | 4.11.0 | Unity IAP |
| `com.unity.textmeshpro` | 3.0.6 | UI Text |
| `com.unity.inputsystem` | 1.7.0 | Input |
| `com.unity.addressables` | 1.21.21 | Asset loading |

### Via manifest.json (add manually):
Open `Packages/manifest.json` and add to the `"dependencies"` block:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

Full `manifest.json` dependencies:
```json
{
  "dependencies": {
    "com.unity.addressables": "1.21.21",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.purchasing": "4.11.0",
    "com.unity.textmeshpro": "3.0.6"
  }
}
```

---

## 4. Firebase Setup

### 4.1 Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project (e.g., `FootballManager`)
3. Enable **Google Analytics** (optional)

### 4.2 Add Android App

1. Click **Add App → Android**
2. Android package name: `com.yourcompany.footballmanager`
3. App nickname: `Football Manager`
4. SHA-1 fingerprint: add your debug keystore SHA-1 (run `keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android`)
5. Download `google-services.json`
6. Place it at: `Assets/google-services.json`

### 4.3 Enable Firebase Services

In Firebase Console, enable:

**Authentication:**
- Go to Authentication → Sign-in method
- Enable **Email/Password**
- Enable **Google**
- Copy the **Web Client ID** from the Google provider (needed later)

**Realtime Database:**
- Go to Realtime Database → Create Database
- Start in **test mode** (tighten rules before release — see §14)
- Note your database URL: `https://your-project-default-rtdb.firebaseio.com/`

**Firestore:**
- Go to Firestore Database → Create Database
- Start in **test mode**
- Used for: market listings (`market/{userId}`)

### 4.4 Import Firebase Unity SDK

1. Download [Firebase Unity SDK](https://firebase.google.com/download/unity) (version 11.x)
2. Extract the zip
3. In Unity: **Assets → Import Package → Custom Package**
4. Import these `.unitypackage` files **in this order**:
   - `FirebaseAuth.unitypackage`
   - `FirebaseDatabase.unitypackage`
   - `FirebaseFirestore.unitypackage`
   - `FirebaseAnalytics.unitypackage` _(optional)_

5. When prompted, run **Android Resolver** (or it runs automatically)
6. If using External Dependency Manager (EDM4U): **Assets → External Dependency Manager → Android Resolver → Resolve**

### 4.5 Verify Firebase Installation

After import, `Assets/google-services.json` must exist and Unity must not show Firebase-related errors in the console.

---

## 5. Google Sign-In Plugin

### 5.1 Download

Download the **Google Sign-In Unity Plugin**:
- [GitHub – googlesamples/google-signin-unity](https://github.com/googlesamples/google-signin-unity/releases)
- Download `google-signin-plugin-1.0.4.unitypackage` (or latest)

### 5.2 Import

**Assets → Import Package → Custom Package** → select the downloaded `.unitypackage`

### 5.3 Configure Web Client ID

1. In the Firebase Console → Authentication → Sign-in method → Google
2. Copy the **Web client ID** (format: `123456789-xxxx.apps.googleusercontent.com`)
3. In Unity, find the `AuthManager` GameObject (in your Auth scene or persistent)
4. In the Inspector, paste the Web Client ID into **Google Web Client Id** field

---

## 6. Unity IAP Setup

### 6.1 Enable IAP

1. **Window → Package Manager** → search for **In App Purchasing** → Install
2. **Edit → Project Settings → Services** → Link to your Unity Project
3. **Edit → Project Settings → Monetization** → Enable IAP

### 6.2 Configure Products in Google Play

In Google Play Console → Your App → Monetize → Products, create these products:

| Product ID | Type | Price (USD) | Tokens Granted |
|------------|------|-------------|----------------|
| `com.footballgame.tokens_100` | Consumable | $0.99 | 100 |
| `com.footballgame.tokens_500` | Consumable | $4.99 | 550 |
| `com.footballgame.tokens_1200` | Consumable | $9.99 | 1,350 |
| `com.footballgame.tokens_2500` | Consumable | $19.99 | 2,900 |
| `com.footballgame.tokens_6000` | Consumable | $49.99 | 7,200 |
| `com.footballgame.premium_monthly` | Subscription | $2.99/mo | 30 days premium |
| `com.footballgame.premium_yearly` | Subscription | $19.99/yr | 365 days premium |

### 6.3 Add IAP Products to Unity

The `IAPManager.cs` script auto-registers all products in `InitIAP()`. No manual configuration needed — just ensure the package is installed.

---

## 7. TextMeshPro Setup

1. **Window → TextMeshPro → Import TMP Essential Resources** — click Import
2. **Window → TextMeshPro → Import TMP Examples & Extras** — optional but recommended
3. For Arabic support: Import the **Arabic font asset** (NotoSansArabic or similar) via TMP Font Asset Creator

---

## 8. Scene Setup

Create these scenes under `Assets/Scenes/` and add them to the Build Settings:

**File → Build Settings → Scenes In Build (in order):**

| Index | Scene Name | File |
|-------|------------|------|
| 0 | Loading | `Assets/Scenes/Loading.unity` |
| 1 | Authentication | `Assets/Scenes/Authentication.unity` |
| 2 | Registration | `Assets/Scenes/Registration.unity` |
| 3 | MainMenu | `Assets/Scenes/MainMenu.unity` |
| 4 | Match | `Assets/Scenes/Match.unity` |
| 5 | Market | `Assets/Scenes/Market.unity` |
| 6 | Rankings | `Assets/Scenes/Rankings.unity` |
| 7 | Settings | `Assets/Scenes/Settings.unity` |
| 8 | DailyReward | `Assets/Scenes/DailyReward.unity` |

> **Important:** Scene names must exactly match the `SceneName` enum values in `GameSceneManager.cs`.

---

## 9. Persistent GameManager GameObject

The `GameManager` is a singleton that persists across all scenes. Create it in your first scene (`Loading`) or as a prefab:

### Required Components on `GameManager` GameObject:

| Component | Notes |
|-----------|-------|
| `GameManager` | Core singleton |
| `DataManager` | Firebase RTDB access |
| `LocalizationManager` | 9-language support |
| `AudioManager` | SFX + Music |
| `TokenSystem` | Token balance |
| `DailyRewardSystem` | Daily login rewards |
| `RankingSystem` | World/Country/City ranks |
| `PremiumSystem` | Subscription status |
| `IAPManager` | In-app purchases |
| `MarketSystem` | Transfer market |
| `TransferSystem` | Player transfers |
| `RegistrationManager` | New user registration |
| `AuthManager` | Firebase Auth + Google |
| `GameSceneManager` | Scene transitions |

All of these use `DontDestroyOnLoad` and are safe to attach to a single GameObject.

### AudioManager Inspector Wiring:

Drag your audio clips into the AudioManager fields:

| Field | Suggested Clip |
|-------|---------------|
| sfxGoal | crowd_cheer.wav |
| sfxWhistle | referee_whistle.wav |
| sfxYellowCard | card_show.wav |
| sfxRedCard | card_show.wav |
| sfxPenalty | penalty_kick.wav |
| sfxButtonClick | ui_click.wav |
| sfxCoinCollect | coin.wav |
| sfxPackOpen | pack_open.wav |
| sfxMatchKickoff | kickoff_whistle.wav |
| sfxHalfTime | half_time_whistle.wav |
| sfxFullTime | full_time_whistle.wav |
| sfxSubstitution | substitution.wav |
| sfxCorner | corner_flag.wav |
| sfxFoul | foul_whistle.wav |
| sfxCheer | crowd_cheer.wav |
| sfxBoo | crowd_boo.wav |
| musicMenu | menu_music.mp3 |
| musicMatch | match_music.mp3 |
| musicVictory | victory_fanfare.mp3 |
| musicDefeat | defeat_music.mp3 |

Add two `AudioSource` components to the GameObject and assign them to **sfxSource** and **musicSource** fields.

---

## 10. Scene-by-Scene Inspector Wiring

### Authentication Scene

**Hierarchy:**
```
Canvas
  └─ AuthUI (component: AuthUI)
       ├─ LoginPanel
       │    ├─ InputField_Email        → loginEmail
       │    ├─ InputField_Password     → loginPassword
       │    ├─ Button_Login            → btnLogin
       │    ├─ Button_GoogleLogin      → btnGoogleLogin
       │    ├─ Button_ToRegister       → btnToRegister
       │    ├─ Button_ToReset          → btnToReset
       │    └─ Text_LoginError         → txtLoginError
       ├─ RegisterPanel
       │    ├─ InputField_Email        → regEmail
       │    ├─ InputField_Password     → regPassword
       │    ├─ InputField_DisplayName  → regDisplayName
       │    ├─ InputField_TeamName     → regTeamName
       │    ├─ InputField_Country      → regCountry
       │    ├─ InputField_City         → regCity
       │    ├─ Button_Register         → btnRegister
       │    ├─ Button_ToLogin          → btnToLogin
       │    └─ Text_RegError           → txtRegError
       └─ ResetPanel
            ├─ InputField_Email        → resetEmail
            ├─ Button_SendReset        → btnSendReset
            ├─ Button_BackToLogin      → btnBackToLogin
            └─ Text_ResetMsg           → txtResetMsg
```

### TeamSelection Scene

**Hierarchy:**
```
Canvas
  └─ TeamSelectionUI (component: TeamSelectionUI)
       ├─ InputField_TeamName         → inputTeamName
       ├─ InputField_Country          → inputCountry
       ├─ InputField_City             → inputCity
       ├─ Image_PrimaryColor          → primaryColorPreview
       ├─ Image_SecondaryColor        → secondaryColorPreview
       ├─ Slider_PrimaryR             → sliderPrimaryR
       ├─ Slider_PrimaryG             → sliderPrimaryG
       ├─ Slider_PrimaryB             → sliderPrimaryB
       ├─ Slider_SecondaryR           → sliderSecondaryR
       ├─ Slider_SecondaryG           → sliderSecondaryG
       ├─ Slider_SecondaryB           → sliderSecondaryB
       ├─ Button_Confirm              → btnConfirm
       └─ Text_Error                  → txtError
```

### MainMenu Scene

**Hierarchy:**
```
Canvas
  └─ MainMenuUI (component: MainMenuUI)
       ├─ Text_DisplayName            → txtDisplayName
       ├─ Text_TeamName               → txtTeamName
       ├─ Text_TokenBalance           → txtTokenBalance
       ├─ Text_Rank                   → txtRank
       ├─ Image_PremiumBadge          → imgPremiumBadge
       ├─ Button_Play                 → btnPlay
       ├─ Button_Market               → btnMarket
       ├─ Button_Rankings             → btnRankings
       ├─ Button_Settings             → btnSettings
       ├─ Button_DailyReward          → btnDailyReward
       ├─ Button_Logout               → btnLogout
       └─ GameObject_DailyRewardDot   → dailyRewardDot
```

### Match Scene

**Hierarchy:**
```
MatchSetup (component: MatchSetup)
MatchController (component: MatchController)
MatchSimulator (component: MatchSimulator)
Canvas
  └─ MatchUI (component: MatchUI)
       ├─ Text_HomeTeam               → txtHomeTeam
       ├─ Text_AwayTeam               → txtAwayTeam
       ├─ Text_Score                  → txtScore
       ├─ Text_Minute                 → txtMinute
       ├─ Transform_CommentaryList    → commentaryContainer
       ├─ Prefab_CommentaryItem       → commentaryItemPrefab
       ├─ ScrollRect_Commentary       → commentaryScroll
       ├─ Button_Pause                → btnPause
       ├─ Button_Resume               → btnResume (optional, can be same as Pause)
       ├─ Button_Speed1x              → btnSpeed1
       ├─ Button_Speed2x              → btnSpeed2
       ├─ Button_Speed4x              → btnSpeed4
       ├─ Text_PauseLabel             → txtPauseLabel
       ├─ Panel_Substitution          → substitutionPanel
       │    ├─ Button_OpenSubs        → btnOpenSubs
       │    ├─ Button_CloseSubs       → btnCloseSubs
       │    ├─ Transform_OnField      → onFieldContainer
       │    ├─ Transform_Bench        → benchContainer
       │    ├─ Prefab_PlayerRow       → playerRowPrefab
       │    └─ Text_SubsRemaining     → txtSubsRemaining
       └─ Panel_Result                → resultPanel
            ├─ Text_ResultScore       → txtResultScore
            ├─ Text_ResultDesc        → txtResultDescription
            └─ Button_BackToMenu      → btnBackToMenu
```

**PlayerRow Prefab** must have:
- `Button` component on root
- `Image` component on root (for highlight)
- 3 `TextMeshProUGUI` children: [0]=Name, [1]=Position, [2]=Overall

**CommentaryItem Prefab** must have:
- 1 `TextMeshProUGUI` child for commentary text

### Market Scene

**Hierarchy:**
```
Canvas
  └─ MarketUI (component: MarketUI)
       ├─ Text_TokenBalance           → txtTokenBalance
       ├─ Text_RefreshCost            → txtRefreshCost
       ├─ Button_Refresh              → btnRefresh
       ├─ Button_Back                 → btnBack
       ├─ Dropdown_Tier               → ddTier
       ├─ Dropdown_Position           → ddPosition
       ├─ Transform_List              → listContainer
       ├─ Prefab_PlayerCard           → playerCardPrefab
       ├─ ScrollRect_List             → listScroll
       └─ Panel_Confirm               → confirmPanel
            ├─ Text_ConfirmName       → txtConfirmName
            ├─ Text_ConfirmStats      → txtConfirmStats
            ├─ Text_ConfirmPrice      → txtConfirmPrice
            ├─ Button_ConfirmBuy      → btnConfirmBuy
            └─ Button_CancelBuy       → btnCancelBuy
```

**Tier Dropdown options** (in order):
`All Tiers, Bronze, Silver, Gold, Platinum, Legend`

**Position Dropdown options** (in order):
`All Positions, GK, CB, LB, RB, CDM, CM, CAM, LM, RM, LW, RW, ST, CF`

**PlayerCard Prefab** must have:
- 5 `TextMeshProUGUI` children: [0]=Name, [1]=Position+OVR, [2]=PAC/SHO/PAS, [3]=DRI/DEF/PHY, [4]=Price
- 1 `Button` child for buy button

### Rankings Scene

**Hierarchy:**
```
Canvas
  └─ RankingUI (component: RankingUI)
       ├─ Button_World                → btnWorld
       ├─ Button_Country              → btnCountry
       ├─ Button_City                 → btnCity
       ├─ Text_MyRank                 → txtMyRank
       ├─ Transform_List              → listContainer
       ├─ Prefab_RankRow              → rankRowPrefab
       ├─ ScrollRect_List             → listScroll
       └─ Button_Back                 → btnBack
```

**RankRow Prefab** must have 6 `TextMeshProUGUI` children:
`[0]=Rank, [1]=DisplayName, [2]=TeamName, [3]=Points, [4]=W/D/L, [5]=GF:GA`

### Settings Scene

**Hierarchy:**
```
Canvas
  └─ SettingsUI (component: SettingsUI)
       ├─ Toggle_SFX                  → toggleSFX
       ├─ Toggle_Music                → toggleMusic
       ├─ Slider_SFX                  → sliderSFX
       ├─ Slider_Music                → sliderMusic
       ├─ Dropdown_Language           → ddLanguage
       ├─ Text_Email                  → txtEmail
       ├─ Text_DisplayName            → txtDisplayName
       ├─ Text_PremiumStatus          → txtPremiumStatus
       ├─ Button_PremiumMonthly       → btnBuyPremiumMonthly
       ├─ Button_PremiumYearly        → btnBuyPremiumYearly
       ├─ Button_Buy100               → btnBuy100
       ├─ Button_Buy500               → btnBuy500
       ├─ Button_Buy1200              → btnBuy1200
       ├─ Button_Buy2500              → btnBuy2500
       ├─ Button_Buy6000              → btnBuy6000
       └─ Button_Back                 → btnBack
```

### DailyReward Scene

**Hierarchy:**
```
Canvas
  └─ DailyRewardUI (component: DailyRewardUI)
       ├─ DaySlot_1                   → daySlots[0]
       │    ├─ Text_Day               → dayLabel
       │    ├─ Text_Reward            → rewardLabel
       │    ├─ GameObject_Claimed     → claimedOverlay
       │    └─ GameObject_Glow        → activeGlow
       ├─ DaySlot_2 ... DaySlot_7    → daySlots[1-6]
       ├─ Button_Claim                → btnClaim
       ├─ Text_Status                 → txtStatus
       └─ Button_Back                 → btnBack
```

---

## 11. Localization JSON Files

JSON files must be placed in `Assets/Resources/Localization/`:

```
Assets/Resources/Localization/
  en.json
  tr.json
  es.json
  de.json
  fr.json
  it.json
  pt.json
  ar.json
  th.json
```

### Format (flat key-value):
```json
{
  "login": "Login",
  "register": "Register",
  "logout": "Logout",
  "play": "Play",
  "market": "Market",
  "rankings": "Rankings",
  "settings": "Settings",
  "daily_reward": "Daily Reward",
  "tokens": "Tokens",
  "premium": "Premium",
  "pause": "Pause",
  "resume": "Resume",
  "result_win": "Victory!",
  "result_loss": "Defeat",
  "result_draw": "Draw",
  "premium_active": "Premium Active",
  "premium_inactive": "Not Premium",
  "daily_reward_available": "Claim your daily reward!",
  "daily_reward_next": "Next reward in {0}",
  "days_remaining": "days remaining",
  "hours_remaining": "hours remaining",
  "reset_email_sent": "Reset email sent. Check your inbox.",
  "error_empty_fields": "Please fill in all fields.",
  "error_team_name_empty": "Please enter a team name.",
  "error_team_name_length": "Team name must be 3–20 characters."
}
```

Duplicate this file for each language and translate the values (not the keys).

---

## 12. Android Build Settings

### File → Build Settings

1. Select **Android** platform
2. Click **Switch Platform**
3. Check **Development Build** for testing (uncheck for release)
4. **Player Settings** (see §2)

### Keystore (Release Builds)

1. **Edit → Project Settings → Player → Android → Publishing Settings**
2. Enable **Custom Keystore**
3. Create new keystore: set path, password, alias, alias password
4. Save the keystore and passwords securely
5. Use the same keystore for all updates (cannot change after publishing)

---

## 13. AndroidManifest.xml

Place at `Assets/Plugins/Android/AndroidManifest.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
          package="com.yourcompany.footballmanager">

    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <application
        android:label="Football Manager"
        android:icon="@mipmap/ic_launcher"
        android:theme="@style/UnityThemeSelector"
        android:allowBackup="true">

        <activity
            android:name="com.unity3d.player.UnityPlayerActivity"
            android:label="Football Manager"
            android:screenOrientation="portrait"
            android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"
            android:hardwareAccelerated="false"
            android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>

        <!-- Google Sign-In -->
        <meta-data android:name="com.google.android.gms.version"
                   android:value="@integer/google_play_services_version" />

    </application>
</manifest>
```

---

## 14. Firebase Security Rules

### Realtime Database Rules

In Firebase Console → Realtime Database → Rules:

```json
{
  "rules": {
    "players": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "teams": {
      "$teamId": {
        ".read": "auth != null",
        ".write": "data.child('ownerId').val() === auth.uid || !data.exists()"
      }
    },
    "squads": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "rankings": {
      ".read": "auth != null",
      "$uid": {
        ".write": "$uid === auth.uid"
      }
    },
    "matchHistory": {
      "$matchId": {
        ".read": "auth != null",
        ".write": "auth != null"
      }
    },
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

### Firestore Rules

In Firebase Console → Firestore → Rules:

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /market/{userId} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

---

## 15. Google Play Console

### App Setup

1. Create a new app in [Google Play Console](https://play.google.com/console)
2. Set up the store listing (title, description, screenshots, icon)
3. Target audience: Everyone (no age restriction needed for football manager)
4. Content rating: Complete questionnaire (likely PEGI 3 / Everyone)

### Release Process

1. Build a **Release APK or AAB** (Android App Bundle recommended)
2. Create an **Internal Testing** track first
3. Upload the AAB
4. Add testers
5. Test IAP with test accounts

### SHA-1 for Release

Get your release keystore SHA-1 and add it to Firebase (Authentication → Settings → SHA certificate fingerprints):

```bash
keytool -list -v -keystore your-release-key.jks -alias your-alias
```

---

## 16. Quick Checklist Before Build

- [ ] `google-services.json` is in `Assets/` folder
- [ ] Firebase Auth: Email/Password and Google enabled
- [ ] Google Web Client ID set in `AuthManager` Inspector field
- [ ] Firebase Realtime Database URL matches your project
- [ ] All 9 localization JSON files exist in `Assets/Resources/Localization/`
- [ ] All scenes added to Build Settings in correct order (0–8)
- [ ] Scene names match `SceneName` enum exactly
- [ ] `GameManager` GameObject has all required components attached
- [ ] AudioManager `sfxSource` and `musicSource` AudioSource components assigned
- [ ] TextMeshPro Essential Resources imported
- [ ] IAP products created in Google Play Console with exact product IDs
- [ ] Unity IAP package installed and Services linked
- [ ] Keystore configured for release builds
- [ ] AndroidManifest.xml in `Assets/Plugins/Android/`
- [ ] Firebase Security Rules deployed (not in test mode for release)
- [ ] Minimum API Level set to 24
- [ ] IL2CPP scripting backend selected
- [ ] ARM64 architecture enabled

---

## Troubleshooting

### "Firebase dependency not found"
Run **Assets → External Dependency Manager → Android Resolver → Force Resolve**

### "Google Sign-In crash on Android"
- Verify Web Client ID matches the one in Firebase Auth → Google → Web client ID
- Ensure SHA-1 fingerprint added to Firebase project
- Google Play Services must be installed on test device

### "IAP products not loading"
- Products must be approved in Google Play Console (can take a few hours)
- Use a real device with a real Google account for IAP testing
- Test with `sandbox` tester accounts

### "Localization file not found"
- Ensure JSON files are exactly named `en.json`, `tr.json` etc. (lowercase)
- Files must be in `Assets/Resources/Localization/` (not subdirectory)
- Verify TextAsset type in Project window (should show JSON icon)

### "Scene not found" error
- Scene name in code must exactly match scene file name and `SceneName` enum value
- Scene must be added to Build Settings

### Firebase RTDB "Permission denied"
- User must be authenticated before reading/writing
- Check Security Rules match your data structure paths
