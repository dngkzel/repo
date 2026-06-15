using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;

namespace FootballGame.UI
{
    /// <summary>
    /// Drives the Main Menu screen. Subscribes to GameManager events,
    /// keeps the token balance animated, and routes button presses to
    /// the appropriate GameState transitions.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────────────────────────────

        [Header("Navigation Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button rankingsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button dailyRewardButton;

        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI tokenBalanceText;
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI premiumBadge;

        [Header("Notifications & Badges")]
        [SerializeField] private GameObject dailyRewardNotification;
        [SerializeField] private GameObject premiumBanner;

        [Header("Profile")]
        [SerializeField] private Image profileAvatar;

        [Header("Animation Settings")]
        [SerializeField] private float tokenAnimDuration = 0.8f;

        // ─────────────────────────────────────────────────────────────────────
        // Private State
        // ─────────────────────────────────────────────────────────────────────

        private int _displayedTokenBalance;
        private Coroutine _tokenAnimCoroutine;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            RegisterButtonListeners();
        }

        private void Start()
        {
            SubscribeToEvents();
            RefreshUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            RemoveButtonListeners();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Wiring
        // ─────────────────────────────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            if (GameManager.Instance == null) return;

            // GameManager fires OnStateChanged (UnityEvent<GameState,GameState>)
            // and exposes UpdateTokenBalance / SetPremiumStatus as direct calls.
            // We poll on Start / manual calls rather than private C# events.
        }

        private void UnsubscribeFromEvents()
        {
            // No dynamic subscriptions to clean up with the current GameManager API.
        }

        private void RegisterButtonListeners()
        {
            if (playButton != null)        playButton.onClick.AddListener(OnPlayClicked);
            if (marketButton != null)      marketButton.onClick.AddListener(OnMarketClicked);
            if (rankingsButton != null)    rankingsButton.onClick.AddListener(OnRankingsClicked);
            if (settingsButton != null)    settingsButton.onClick.AddListener(OnSettingsClicked);
            if (dailyRewardButton != null) dailyRewardButton.onClick.AddListener(OnDailyRewardClicked);
        }

        private void RemoveButtonListeners()
        {
            if (playButton != null)        playButton.onClick.RemoveListener(OnPlayClicked);
            if (marketButton != null)      marketButton.onClick.RemoveListener(OnMarketClicked);
            if (rankingsButton != null)    rankingsButton.onClick.RemoveListener(OnRankingsClicked);
            if (settingsButton != null)    settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (dailyRewardButton != null) dailyRewardButton.onClick.RemoveListener(OnDailyRewardClicked);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public UI Update API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Smoothly animates the token counter from the current display
        /// value to <paramref name="tokens"/>.</summary>
        public void UpdateTokenDisplay(int tokens)
        {
            if (_tokenAnimCoroutine != null)
                StopCoroutine(_tokenAnimCoroutine);

            _tokenAnimCoroutine = StartCoroutine(AnimateTokenChange(_displayedTokenBalance, tokens));
        }

        /// <summary>Shows the red-dot notification on the daily reward button if
        /// a reward is currently available.</summary>
        public void UpdateDailyRewardNotification()
        {
            if (dailyRewardNotification == null) return;

            bool available = false;
            if (GameManager.Instance?.DailyRewardSystem != null)
                available = GameManager.Instance.DailyRewardSystem.IsRewardAvailable();

            dailyRewardNotification.SetActive(available);
        }

        /// <summary>Refreshes every UI element from the current PlayerSession.</summary>
        public void RefreshUI()
        {
            if (GameManager.Instance == null) return;

            PlayerSession session = GameManager.Instance.PlayerSession;

            if (teamNameText != null)
                teamNameText.text = string.IsNullOrEmpty(session.teamName) ? "My Team" : session.teamName;

            // Set balance instantly on first load (no animation flash).
            _displayedTokenBalance = session.tokenBalance;
            if (tokenBalanceText != null)
                tokenBalanceText.text = FormatTokens(_displayedTokenBalance);

            CheckPremiumStatus();
            UpdateDailyRewardNotification();
        }

        /// <summary>Shows or hides the premium badge and banner based on session.</summary>
        public void CheckPremiumStatus()
        {
            if (GameManager.Instance == null) return;

            bool isPremium = GameManager.Instance.PlayerSession.isPremium;

            if (premiumBadge != null)  premiumBadge.gameObject.SetActive(isPremium);
            if (premiumBanner != null) premiumBanner.SetActive(isPremium);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Button Handlers
        // ─────────────────────────────────────────────────────────────────────

        public void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] Play clicked – transitioning to Match.");
            GameManager.Instance?.ChangeState(GameState.Match);
        }

        public void OnMarketClicked()
        {
            Debug.Log("[MainMenuUI] Market clicked.");
            GameManager.Instance?.ChangeState(GameState.Market);
        }

        public void OnRankingsClicked()
        {
            Debug.Log("[MainMenuUI] Rankings clicked.");
            GameManager.Instance?.ChangeState(GameState.Rankings);
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings clicked.");
            GameManager.Instance?.ChangeState(GameState.Settings);
        }

        public void OnDailyRewardClicked()
        {
            Debug.Log("[MainMenuUI] Daily Reward clicked.");
            GameManager.Instance?.ChangeState(GameState.DailyReward);
        }

        // ─────────────────────────────────────────────────────────────────────
        // External Event Handlers (called by other systems)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Called by TokenSystem / GameManager when the balance changes.</summary>
        public void OnTokenBalanceChanged(int newBalance)
        {
            UpdateTokenDisplay(newBalance);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coroutines
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Animates the token counter from <paramref name="from"/> to
        /// <paramref name="to"/> over <see cref="tokenAnimDuration"/> seconds using
        /// an ease-out quad curve.</summary>
        public IEnumerator AnimateTokenChange(int from, int to)
        {
            float elapsed = 0f;

            while (elapsed < tokenAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tokenAnimDuration);
                // Ease-out quadratic: decelerate toward the target.
                t = 1f - (1f - t) * (1f - t);

                int display = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                if (tokenBalanceText != null)
                    tokenBalanceText.text = FormatTokens(display);

                yield return null;
            }

            _displayedTokenBalance = to;
            if (tokenBalanceText != null)
                tokenBalanceText.text = FormatTokens(to);

            _tokenAnimCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static string FormatTokens(int amount)
        {
            if (amount >= 1_000_000)
                return $"{amount / 1_000_000f:0.##}M";
            if (amount >= 1_000)
                return $"{amount / 1_000f:0.##}K";
            return amount.ToString("N0");
        }
    }
}
