using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Header("Account")]
        [SerializeField] private TMP_Text _displayNameText;
        [SerializeField] private TMP_Text _emailText;
        [SerializeField] private TMP_Text _teamNameText;
        [SerializeField] private TMP_Text _premiumStatusText;
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _changeTeamNameButton;
        [SerializeField] private Button _deleteAccountButton;

        [Header("Notifications")]
        [SerializeField] private Toggle _pushNotifToggle;
        [SerializeField] private Toggle _dailyReminderToggle;

        [Header("Display")]
        [SerializeField] private Toggle _vibrationToggle;

        [Header("Version")]
        [SerializeField] private TMP_Text _versionText;

        private readonly string[] _languageCodes = { "en", "tr", "es", "de", "fr", "it", "pt", "ar", "th" };
        private readonly string[] _languageNames = { "English", "Türkçe", "Español", "Deutsch", "Français", "Italiano", "Português", "العربية", "ภาษาไทย" };

        private void Start()
        {
            SetupLanguageDropdown();
            LoadSettings();
            SetupListeners();

            if (_versionText != null)
                _versionText.text = $"v{Application.version}";
        }

        private void SetupLanguageDropdown()
        {
            if (_languageDropdown == null) return;
            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>(_languageNames));

            string currentLang = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            for (int i = 0; i < _languageCodes.Length; i++)
            {
                if (_languageCodes[i] == currentLang)
                {
                    _languageDropdown.value = i;
                    break;
                }
            }
        }

        private void LoadSettings()
        {
            if (_sfxToggle != null) _sfxToggle.isOn = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
            if (_musicToggle != null) _musicToggle.isOn = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            if (_vibrationToggle != null) _vibrationToggle.isOn = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            if (_pushNotifToggle != null) _pushNotifToggle.isOn = PlayerPrefs.GetInt("PushNotifEnabled", 1) == 1;
            if (_dailyReminderToggle != null) _dailyReminderToggle.isOn = PlayerPrefs.GetInt("DailyReminderEnabled", 1) == 1;

            // User info
            var userData = GameManager.Instance?.CurrentUserData;
            if (userData != null)
            {
                if (_displayNameText != null) _displayNameText.text = userData.DisplayName;
                if (_emailText != null) _emailText.text = userData.Email;
                if (_teamNameText != null) _teamNameText.text = userData.TeamName;
            }
        }

        private void SetupListeners()
        {
            _sfxToggle?.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXEnabled(v));
            _musicToggle?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicEnabled(v));
            _sfxVolumeSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
            _musicVolumeSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

            _languageDropdown?.onValueChanged.AddListener(idx =>
            {
                if (idx < _languageCodes.Length)
                    LocalizationManager.Instance?.SetLanguage(_languageCodes[idx]);
            });

            _vibrationToggle?.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetInt("VibrationEnabled", v ? 1 : 0);
            });

            _logoutButton?.onClick.AddListener(OnLogoutClicked);
            _changeTeamNameButton?.onClick.AddListener(OnChangeTeamName);
            _deleteAccountButton?.onClick.AddListener(OnDeleteAccount);
        }

        private void OnLogoutClicked()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            Authentication.AuthManager.Instance?.SignOut();
            GameSceneManager.Instance?.LoadScene("Login");
        }

        private void OnChangeTeamName()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            // Open team name change panel
            FindObjectOfType<TeamSelectionUI>()?.ShowChangeMode();
        }

        private void OnDeleteAccount()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            // Show confirmation dialog
            Debug.Log("Delete account requested - show confirmation dialog");
        }
    }
}
