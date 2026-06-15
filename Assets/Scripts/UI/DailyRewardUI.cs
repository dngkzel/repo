using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Economy;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class DailyRewardUI : MonoBehaviour
    {
        [Header("Day Slots (7 elements)")]
        public DaySlot[] daySlots;

        [Header("Claim")]
        public Button btnClaim;
        public TextMeshProUGUI txtStatus;

        [Header("Navigation")]
        public Button btnBack;

        private DailyRewardSystem _sys;

        private void Start()
        {
            _sys = DailyRewardSystem.Instance;
            var pd = GameManager.Instance?.CurrentUserData;

            if (_sys != null && pd != null)
                _sys.CheckDailyReward(pd);

            _sys?.OnRewardClaimed += OnRewardClaimed;

            RenderDays(pd?.DailyRewardDay ?? 0);
            UpdateStatus();

            btnClaim?.onClick.AddListener(OnClaim);
            btnBack?.onClick.AddListener(OnBack);

            bool canClaim = _sys?.CanClaimToday ?? false;
            btnClaim?.gameObject.SetActive(canClaim);
        }

        private void OnDestroy()
        {
            if (_sys != null) _sys.OnRewardClaimed -= OnRewardClaimed;
        }

        private void OnRewardClaimed(DailyReward reward)
        {
            AudioManager.Instance?.PlaySFX(SFX.CoinCollect);
            if (!string.IsNullOrEmpty(reward.BonusPackTier))
                AudioManager.Instance?.PlaySFX(SFX.PackOpen);
            var pd = GameManager.Instance?.CurrentUserData;
            RenderDays(pd?.DailyRewardDay ?? _sys?.CurrentStreakDay ?? 0);
            UpdateStatus();
            btnClaim?.gameObject.SetActive(false);
        }

        private void RenderDays(int currentDay)
        {
            if (daySlots == null) return;
            for (int i = 0; i < daySlots.Length && i < DailyRewardSystem.Schedule.Count; i++)
            {
                var sched = DailyRewardSystem.Schedule[i];
                var slot  = daySlots[i];
                if (slot == null) continue;

                bool claimed = i < currentDay;
                bool active  = i == currentDay && (_sys?.CanClaimToday ?? false);
                bool hasPack = !string.IsNullOrEmpty(sched.BonusPackTier);

                slot.dayLabel?.SetText($"Day {i + 1}");
                slot.rewardLabel?.SetText(hasPack ? $"{sched.Tokens}T + Pack" : $"{sched.Tokens}T");
                slot.claimedOverlay?.SetActive(claimed);
                slot.activeGlow?.SetActive(active);
            }
        }

        private void UpdateStatus()
        {
            if (txtStatus == null) return;
            var loc = LocalizationManager.Instance;
            if (_sys?.CanClaimToday == true)
                txtStatus.text = loc?.Get("daily_reward_available") ?? "Claim your daily reward!";
            else
            {
                var nextMidnight = System.DateTime.UtcNow.Date.AddDays(1);
                var remaining = nextMidnight - System.DateTime.UtcNow;
                txtStatus.text = string.Format(
                    loc?.Get("daily_reward_next") ?? "Next reward in {0}",
                    remaining.ToString(@"hh\:mm\:ss"));
            }
        }

        private void OnClaim()
        {
            string uid = GameManager.Instance?.CurrentUserId ?? "";
            if (string.IsNullOrEmpty(uid) || _sys == null || !_sys.CanClaimToday) return;
            btnClaim.interactable = false;
            _sys.ClaimDailyReward(uid);
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }

    [System.Serializable]
    public class DaySlot
    {
        public TextMeshProUGUI dayLabel;
        public TextMeshProUGUI rewardLabel;
        public GameObject claimedOverlay;
        public GameObject activeGlow;
    }
}
