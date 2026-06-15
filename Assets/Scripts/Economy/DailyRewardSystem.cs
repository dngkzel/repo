using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using FootballGame.Core;
using FootballGame.Player;

namespace FootballGame.Economy
{
    [Serializable]
    public class DailyReward
    {
        public int Day;
        public int Tokens;
        public string BonusPackTier; // "Bronze","Silver","Gold", or ""
        public string IconName;
    }

    public class DailyRewardSystem : MonoBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        public static readonly List<DailyReward> Schedule = new List<DailyReward>
        {
            new DailyReward { Day=1, Tokens=50,  BonusPackTier="",       IconName="coin_50"    },
            new DailyReward { Day=2, Tokens=100, BonusPackTier="",       IconName="coin_100"   },
            new DailyReward { Day=3, Tokens=150, BonusPackTier="Bronze", IconName="pack_bronze" },
            new DailyReward { Day=4, Tokens=200, BonusPackTier="",       IconName="coin_200"   },
            new DailyReward { Day=5, Tokens=300, BonusPackTier="Silver", IconName="pack_silver" },
            new DailyReward { Day=6, Tokens=400, BonusPackTier="",       IconName="coin_400"   },
            new DailyReward { Day=7, Tokens=500, BonusPackTier="Gold",   IconName="pack_gold"  },
        };

        public int CurrentStreakDay { get; private set; }
        public bool CanClaimToday { get; private set; }
        public DateTime LastClaimDate { get; private set; }

        public event Action<DailyReward> OnRewardClaimed;
        public event Action OnRewardDataLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void CheckDailyReward(PlayerData player)
        {
            if (player == null) return;
            CurrentStreakDay = player.DailyRewardDay;
            string lastDate = player.LastLoginDate ?? "";
            if (string.IsNullOrEmpty(lastDate))
            {
                CanClaimToday = true;
            }
            else if (DateTime.TryParse(lastDate, out DateTime last))
            {
                LastClaimDate = last;
                int diff = (DateTime.UtcNow.Date - last.Date).Days;
                CanClaimToday = diff >= 1;
                if (diff > 1) CurrentStreakDay = 0; // streak broken
            }
            else CanClaimToday = true;

            OnRewardDataLoaded?.Invoke();
        }

        public void ClaimDailyReward(string userId)
        {
            if (!CanClaimToday) return;
            int nextDay = (CurrentStreakDay % 7) + 1;
            DailyReward reward = Schedule[nextDay - 1];

            int tokens = reward.Tokens;
            if (PremiumSystem.Instance?.IsPremium == true)
                tokens = Mathf.RoundToInt(tokens * PremiumSystem.PREMIUM_TOKEN_MULTIPLIER);

            TokenSystem.Instance?.AddTokens(tokens, $"Daily Reward Day {nextDay}");

            if (!string.IsNullOrEmpty(reward.BonusPackTier) &&
                Enum.TryParse<PlayerTier>(reward.BonusPackTier, out PlayerTier packTier))
            {
                var freePlayer = FootballPlayerData.GenerateRandomPlayer(packTier);
                if (freePlayer != null && !string.IsNullOrEmpty(userId))
                {
                    FirebaseDatabase.DefaultInstance.RootReference
                        .Child("squads").Child(userId).Child(freePlayer.PlayerId)
                        .SetValueAsync(freePlayer.ToFirebaseDictionary());
                }
            }

            CurrentStreakDay = nextDay;
            CanClaimToday = false;
            LastClaimDate = DateTime.UtcNow;
            string todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var updates = new Dictionary<string, object>
            {
                [$"users/{userId}/dailyReward/streakDay"] = CurrentStreakDay,
                [$"users/{userId}/dailyReward/lastClaimDate"] = todayStr,
                [$"players/{userId}/dailyRewardDay"] = CurrentStreakDay,
                [$"players/{userId}/lastLoginDate"] = todayStr,
            };
            FirebaseDatabase.DefaultInstance.RootReference.UpdateChildrenAsync(updates);

            OnRewardClaimed?.Invoke(reward);
        }

        public DailyReward GetNextReward() => Schedule[(CurrentStreakDay % 7)];
    }
}
