using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Player Info")]
        public TextMeshProUGUI txtDisplayName;
        public TextMeshProUGUI txtTeamName;
        public TextMeshProUGUI txtTokenBalance;
        public TextMeshProUGUI txtRank;
        public Image imgPremiumBadge;

        [Header("Navigation Buttons")]
        public Button btnPlay;
        public Button btnMarket;
        public Button btnRankings;
        public Button btnSettings;
        public Button btnDailyReward;
        public Button btnLogout;

        [Header("Daily Reward Notification")]
        public GameObject dailyRewardDot;

        // Cached to avoid going through GameManager.Instance getter during teardown
        private Economy.TokenSystem _tokenSystem;

        private void OnEnable()
        {
            RefreshUI();
            CheckDailyReward();
        }

        private void Start()
        {
            btnPlay?.onClick.AddListener(OnPlay);
            btnMarket?.onClick.AddListener(OnMarket);
            btnRankings?.onClick.AddListener(OnRankings);
            btnSettings?.onClick.AddListener(OnSettings);
            btnDailyReward?.onClick.AddListener(OnDailyReward);
            btnLogout?.onClick.AddListener(OnLogout);

            _tokenSystem = GameManager.Instance?.TokenSystem;
            if (_tokenSystem != null)
                _tokenSystem.OnTokenBalanceChanged += OnBalanceChanged;

            AudioManager.Instance?.PlayMusic(MusicTrack.Menu);
        }

        private void OnDestroy()
        {
            // Use cached reference — never go through Instance getter in OnDestroy,
            // the getter creates a new GameObject if instance is null during teardown.
            if (_tokenSystem != null)
                _tokenSystem.OnTokenBalanceChanged -= OnBalanceChanged;
        }

        private void RefreshUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var pd = gm.CurrentUserData;
            if (pd != null)
            {
                if (txtDisplayName) txtDisplayName.text = pd.DisplayName;
                if (txtTeamName)    txtTeamName.text    = gm.CurrentTeam?.TeamName ?? pd.TeamId;
                if (imgPremiumBadge) imgPremiumBadge.gameObject.SetActive(gm.IsPremium);
            }

            if (txtTokenBalance) txtTokenBalance.text = gm.CurrentTokenBalance.ToString("N0");

            int rank = Ranking.RankingSystem.Instance?.GetMyRank(gm.CurrentUserId, Ranking.RankingScope.World) ?? -1;
            if (txtRank) txtRank.text = rank > 0 ? $"#{rank}" : "-";
        }

        private void OnBalanceChanged(int newBalance)
        {
            if (txtTokenBalance) txtTokenBalance.text = newBalance.ToString("N0");
        }

        private void CheckDailyReward()
        {
            if (dailyRewardDot == null) return;
            var pd  = GameManager.Instance?.CurrentUserData;
            var sys = Economy.DailyRewardSystem.Instance;
            if (sys != null && pd != null) sys.CheckDailyReward(pd);
            dailyRewardDot.SetActive(sys?.CanClaimToday ?? false);
        }

        private void OnPlay()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.Match);
        }

        private void OnMarket()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.Market);
        }

        private void OnRankings()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.Rankings);
        }

        private void OnSettings()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.Settings);
        }

        private void OnDailyReward()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.DailyReward);
        }

        private void OnLogout()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            Authentication.AuthManager.Instance?.Logout();
        }
    }
}
