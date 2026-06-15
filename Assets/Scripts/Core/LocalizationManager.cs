using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace FootballGame.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        public static event Action OnLanguageChanged;

        public enum Language { English, Turkish, Spanish, German, French, Italian, Portuguese, Arabic, Thai }

        private Language _currentLanguage = Language.English;
        public Language CurrentLanguage => _currentLanguage;

        private Dictionary<string, string> _strings = new Dictionary<string, string>();

        private static readonly Dictionary<Language, string> LanguageCodes = new Dictionary<Language, string>
        {
            { Language.English, "en" }, { Language.Turkish, "tr" }, { Language.Spanish, "es" },
            { Language.German, "de" }, { Language.French, "fr" }, { Language.Italian, "it" },
            { Language.Portuguese, "pt" }, { Language.Arabic, "ar" }, { Language.Thai, "th" }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            string code = PlayerPrefs.GetString("language", "en");
            LoadLanguage(GetLanguageFromCode(code));
        }

        public void LoadLanguage(Language language)
        {
            _currentLanguage = language;
            LoadByCode(LanguageCodes[language]);
        }

        public void SetLanguage(string code)
        {
            LoadLanguage(GetLanguageFromCode(code));
        }

        public string CurrentLanguageCode => LanguageCodes.TryGetValue(_currentLanguage, out var c) ? c : "en";

        private void LoadByCode(string code)
        {
            TextAsset ta = Resources.Load<TextAsset>($"Localization/{code}");
            if (ta == null) { Debug.LogError($"[Localization] Missing: {code}"); return; }

            _strings.Clear();
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(ta.text);
                if (dict != null)
                    foreach (var kv in dict)
                        _strings[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Parse error: {ex.Message}");
            }

            PlayerPrefs.SetString("language", code);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        public string Get(string key, params object[] args)
        {
            if (_strings.TryGetValue(key, out string value))
                return args?.Length > 0 ? string.Format(value, args) : value;
            return $"[{key}]";
        }

        public bool IsRTL() => _currentLanguage == Language.Arabic;

        public Language GetLanguageFromCode(string code)
        {
            foreach (var kvp in LanguageCodes)
                if (kvp.Value == code) return kvp.Key;
            return Language.English;
        }

        public List<string> GetAvailableLanguageNames() =>
            new List<string> { "English", "Türkçe", "Español", "Deutsch", "Français", "Italiano", "Português", "العربية", "ภาษาไทย" };
    }
}
