using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Economy;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio")]
        public Toggle toggleSFX;
        public Toggle toggleMusic;
        public Slider sliderSFX;
        public Slider sliderMusic;

        [Header("Language")]
        public TMP_Dropdown ddLanguage;

        [Header("Account")]
        public TextMeshProUGUI txtEmail;
        public TextMeshProUGUI txtDisplayName;
        public TextMeshProUGUI txtPremiumStatus;
        public Button btnBuyPremiumMonthly;
        public Button btnBuyPremiumYearly;

        [Header("IAP – Token Packages")]
        public Button btnBuy100;
        public Button btnBuy500;
        public Button btnBuy1200;
        public Button btnBuy2500;
        public Button btnBuy6000;

        [Header("Navigation")]
        public Button btnBack;

        private readonly string[] _langCodes = { "en", "tr", "es", "de", "fr", "it", "pt", "ar", "th" };

        private void Start()
        {
            var am = AudioManager.Instance;
            if (toggleSFX)   { toggleSFX.isOn   = am?.SFXEnabled   ?? true;  toggleSFX.onValueChanged.AddListener(v => am?.SetSFXEnabled(v)); }
            if (toggleMusic) { toggleMusic.isOn  = am?.MusicEnabled ?? true;  toggleMusic.onValueChanged.AddListener(v => am?.SetMusicEnabled(v)); }
            if (sliderSFX)   { sliderSFX.value   = am?.SFXVolume    ?? 1f;   sliderSFX.onValueChanged.AddListener(v => am?.SetSFXVolume(v)); }
            if (sliderMusic) { sliderMusic.value  = am?.MusicVolume  ?? 0.6f; sliderMusic.onValueChanged.AddListener(v => am?.SetMusicVolume(v)); }

            SetupLanguageDropdown();

            var pd = GameManager.Instance?.CurrentUserData;
            if (pd != null)
            {
                if (txtEmail)       txtEmail.text       = pd.Email       ?? "";
                if (txtDisplayName) txtDisplayName.text = pd.DisplayName ?? "";
            }

            bool prem = GameManager.Instance?.IsPremium ?? false;
            if (txtPremiumStatus)
                txtPremiumStatus.text = prem
                    ? PremiumSystem.Instance?.GetExpiryText() ?? LocalizationManager.Instance?.Get("premium_active") ?? "Premium Active"
                    : LocalizationManager.Instance?.Get("premium_inactive") ?? "Not Premium";

            btnBuyPremiumMonthly?.onClick.AddListener(() => BuyPremium(false));
            btnBuyPremiumYearly?.onClick.AddListener(()  => BuyPremium(true));
            btnBuy100?.onClick.AddListener(()   => BuyTokens(IAPManager.TOKENS_100));
            btnBuy500?.onClick.AddListener(()   => BuyTokens(IAPManager.TOKENS_500));
            btnBuy1200?.onClick.AddListener(()  => BuyTokens(IAPManager.TOKENS_1200));
            btnBuy2500?.onClick.AddListener(()  => BuyTokens(IAPManager.TOKENS_2500));
            btnBuy6000?.onClick.AddListener(()  => BuyTokens(IAPManager.TOKENS_6000));
            btnBack?.onClick.AddListener(OnBack);
        }

        private void SetupLanguageDropdown()
        {
            if (ddLanguage == null) return;
            ddLanguage.ClearOptions();
            ddLanguage.AddOptions(new System.Collections.Generic.List<string>
                { "English","Türkçe","Español","Deutsch","Français","Italiano","Português","العربية","ภาษาไทย" });

            string cur = LocalizationManager.Instance?.CurrentLanguageCode ?? "en";
            int idx = System.Array.IndexOf(_langCodes, cur);
            ddLanguage.value = idx >= 0 ? idx : 0;
            ddLanguage.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void OnLanguageChanged(int idx)
        {
            if (idx < 0 || idx >= _langCodes.Length) return;
            LocalizationManager.Instance?.SetLanguage(_langCodes[idx]);
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private void BuyTokens(string productId)
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            IAPManager.Instance?.BuyTokens(productId);
        }

        private void BuyPremium(bool yearly)
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            IAPManager.Instance?.BuyPremium(yearly);
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }
}
