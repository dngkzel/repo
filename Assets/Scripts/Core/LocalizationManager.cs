using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public static event Action OnLanguageChanged;

        public enum Language
        {
            English,
            Turkish,
            Spanish,
            German,
            French,
            Italian,
            Portuguese,
            Arabic,
            Thai
        }

        private Language _currentLanguage = Language.English;
        public Language CurrentLanguage => _currentLanguage;

        private Dictionary<string, string> _strings = new Dictionary<string, string>();

        private static readonly Dictionary<Language, string> LanguageCodes = new Dictionary<Language, string>
        {
            { Language.English, "en" },
            { Language.Turkish, "tr" },
            { Language.Spanish, "es" },
            { Language.German, "de" },
            { Language.French, "fr" },
            { Language.Italian, "it" },
            { Language.Portuguese, "pt" },
            { Language.Arabic, "ar" },
            { Language.Thai, "th" }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Load saved language preference
            string savedCode = PlayerPrefs.GetString("language", "en");
            Language lang = GetLanguageFromCode(savedCode);
            LoadLanguage(lang);
        }

        public void LoadLanguage(Language language)
        {
            _currentLanguage = language;
            string code = LanguageCodes[language];
            LoadByCode(code);
        }

        // String-code overload for SettingsUI compatibility
        public void SetLanguage(string code)
        {
            Language lang = GetLanguageFromCode(code);
            LoadLanguage(lang);
        }

        // String representation of current language
        public string CurrentLanguageCode => LanguageCodes.TryGetValue(_currentLanguage, out var c) ? c : "en";

        private void LoadByCode(string code)
        {
            TextAsset textAsset = Resources.Load<TextAsset>($"Localization/{code}");
            if (textAsset == null)
            {
                Debug.LogError($"[LocalizationManager] Could not load: Localization/{code}");
                return;
            }

            _strings.Clear();

            // Parse flat JSON {"key":"value"} using Newtonsoft.Json if available, else fallback
            try
            {
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                if (dict != null)
                    foreach (var kv in dict)
                        _strings[kv.Key] = kv.Value;
            }
            catch
            {
                // Fallback: try legacy array format
                var data = JsonUtility.FromJson<LocalizationData>(textAsset.text);
                if (data?.entries != null)
                    foreach (var entry in data.entries)
                        _strings[entry.key] = entry.value;
            }

            PlayerPrefs.SetString("language", code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
            Debug.Log($"[LocalizationManager] Language loaded: {code} ({_strings.Count} keys)");
        }

        public string Get(string key, params object[] args)
        {
            if (_strings.TryGetValue(key, out string value))
            {
                if (args != null && args.Length > 0)
                    return string.Format(value, args);
                return value;
            }
            Debug.LogWarning($"[LocalizationManager] Missing key: {key}");
            return $"[{key}]";
        }

        public string GetLanguageCode() => LanguageCodes[_currentLanguage];

        public Language GetLanguageFromCode(string code)
        {
            foreach (var kvp in LanguageCodes)
                if (kvp.Value == code) return kvp.Key;
            return Language.English;
        }

        public bool IsRTL() => _currentLanguage == Language.Arabic;

        public List<string> GetAvailableLanguageNames()
        {
            return new List<string> { "English", "Türkçe", "Español", "Deutsch", "Français", "Italiano", "Português", "العربية", "ภาษาไทย" };
        }
    }

    [Serializable]
    public class LocalizationData
    {
        public LocalizationEntry[] entries;
    }

    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }
}
