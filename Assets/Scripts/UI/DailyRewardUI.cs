using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Economy;
using FootballGame.Audio;
using FootballGame.Core;

namespace FootballGame.UI
{
    public class DailyRewardUI : MonoBehaviour
    {
        [Header("Reward Slots")]
        [SerializeField] private GameObject[] _daySlots; // 7 slots
        [SerializeField] private Image[] _dayIcons;
        [SerializeField] private TMP_Text[] _dayLabels;
        [SerializeField] private TMP_Text[] _dayTokenAmounts;
        [SerializeField] private Image[] _dayBorders;
        [SerializeField] private GameObject[] _claimedOverlays;

        [Header("Claim Button")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimButtonText;
        [SerializeField] private TMP_Text _nextRewardText;

        [Header("Reward Preview")]
        [SerializeField] private TMP_Text _rewardTokenAmount;
        [SerializeField] private TMP_Text _bonusPackLabel;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private GameObject _bonusPackPanel;

        [Header("Colors")]
        [SerializeField] private Color _activeColor = Color.yellow;
        [SerializeField] private Color _claimedColor = Color.gray;
        [SerializeField] private Color _upcomingColor = Color.white;

        [Header("Animation")]
        [SerializeField] private Animator _popupAnimator;
        [SerializeField] private ParticleSystem _confettiEffect;

        private DailyRewardSystem _rewardSystem;

        private void Start()
        {
            _rewardSystem = DailyRewardSystem.Instance;
            if (_rewardSystem == null) return;

            _rewardSystem.OnRewardClaimed += OnRewardClaimed;
            _rewardSystem.OnRewardDataLoaded += RefreshUI;

            RefreshUI();
            _claimButton?.onClick.AddListener(OnClaimClicked);
        }

        private void OnDestroy()
        {
            if (_rewardSystem != null)
            {
                _rewardSystem.OnRewardClaimed -= OnRewardClaimed;
                _rewardSystem.OnRewardDataLoaded -= RefreshUI;
            }
        }

        private void RefreshUI()
        {
            if (_rewardSystem == null) return;

            int currentDay = _rewardSystem.CurrentStreakDay;
            bool canClaim = _rewardSystem.CanClaimToday;
            int nextDayIdx = currentDay % 7;

            for (int i = 0; i < DailyRewardSystem.RewardSchedule.Count && i < _daySlots.Length; i++)
            {
                var reward = DailyRewardSystem.RewardSchedule[i];
                bool isClaimed = i < currentDay;
                bool isActive = i == nextDayIdx && canClaim;

                if (_dayTokenAmounts != null && i < _dayTokenAmounts.Length)
                    _dayTokenAmounts[i].text = $"+{reward.Tokens}";

                if (_dayLabels != null && i < _dayLabels.Length)
                    _dayLabels[i].text = LocalizationManager.Instance?.Get($"day_{i + 1}") ?? $"Day {i + 1}";

                if (_dayBorders != null && i < _dayBorders.Length)
                    _dayBorders[i].color = isClaimed ? _claimedColor : isActive ? _activeColor : _upcomingColor;

                if (_claimedOverlays != null && i < _claimedOverlays.Length)
                    _claimedOverlays[i].SetActive(isClaimed);
            }

            var nextReward = _rewardSystem.GetNextReward();
            if (_rewardTokenAmount != null)
                _rewardTokenAmount.text = $"+{nextReward.Tokens}";

            bool hasPack = !string.IsNullOrEmpty(nextReward.BonusPackTier);
            if (_bonusPackPanel != null) _bonusPackPanel.SetActive(hasPack);
            if (_bonusPackLabel != null && hasPack)
                _bonusPackLabel.text = LocalizationManager.Instance?.Get($"pack_{nextReward.BonusPackTier}") ?? nextReward.BonusPackTier.ToUpper() + " PACK";

            if (_claimButton != null) _claimButton.interactable = canClaim;
            if (_claimButtonText != null)
                _claimButtonText.text = canClaim
                    ? LocalizationManager.Instance?.Get("claim_reward") ?? "CLAIM"
                    : LocalizationManager.Instance?.Get("already_claimed") ?? "Come Back Tomorrow";

            if (!canClaim && _nextRewardText != null)
            {
                System.DateTime tomorrow = System.DateTime.UtcNow.Date.AddDays(1);
                System.TimeSpan remaining = tomorrow - System.DateTime.UtcNow;
                _nextRewardText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                StartCoroutine(UpdateCountdown());
            }
        }

        private void OnClaimClicked()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            string userId = GameManager.Instance?.CurrentUserId;
            if (!string.IsNullOrEmpty(userId))
                _rewardSystem.ClaimDailyReward(userId);
        }

        private void OnRewardClaimed(DailyReward reward)
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.CoinCollect);
            _confettiEffect?.Play();
            StartCoroutine(AnimateClaim(reward));
            RefreshUI();
        }

        private IEnumerator AnimateClaim(DailyReward reward)
        {
            if (_popupAnimator != null)
                _popupAnimator.SetTrigger("Claim");
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator UpdateCountdown()
        {
            while (!_rewardSystem.CanClaimToday)
            {
                yield return new WaitForSeconds(1f);
                if (_nextRewardText != null)
                {
                    System.DateTime tomorrow = System.DateTime.UtcNow.Date.AddDays(1);
                    System.TimeSpan remaining = tomorrow - System.DateTime.UtcNow;
                    _nextRewardText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                }
            }
        }
    }
}
