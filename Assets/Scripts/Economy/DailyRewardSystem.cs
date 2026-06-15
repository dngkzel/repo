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
        public string BonusPackTier; // "bronze", "silver", "gold", ""
        public string IconName;
    }

    public class DailyRewardSystem : MonoBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        public static readonly List<DailyReward> RewardSchedule = new List<DailyReward>
        {
            new DailyReward { Day = 1, Tokens = 50,  BonusPackTier = "",       IconName = "coin_50" },
            new DailyReward { Day = 2, Tokens = 100, BonusPackTier = "",       IconName = "coin_100" },
            new DailyReward { Day = 3, Tokens = 150, BonusPackTier = "bronze", IconName = "pack_bronze" },
            new DailyReward { Day = 4, Tokens = 200, BonusPackTier = "",       IconName = "coin_200" },
            new DailyReward { Day = 5, Tokens = 300, BonusPackTier = "silver", IconName = "pack_silver" },
            new DailyReward { Day = 6, Tokens = 400, BonusPackTier = "",       IconName = "coin_400" },
            new DailyReward { Day = 7, Tokens = 500, BonusPackTier = "gold",   IconName = "pack_gold" },
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

        public void LoadRewardData(string userId)
        {
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            db.Child("users").Child(userId).Child("dailyReward").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    var data = task.Result;
                    CurrentStreakDay = data.Child("streakDay").Exists ? int.Parse(data.Child("streakDay").Value.ToString()) : 0;
                    string lastDateStr = data.Child("lastClaimDate").Exists ? data.Child("lastClaimDate").Value.ToString() : "";

                    if (!string.IsNullOrEmpty(lastDateStr))
                    {
                        LastClaimDate = DateTime.Parse(lastDateStr);
                        TimeSpan diff = DateTime.UtcNow.Date - LastClaimDate.Date;

                        if (diff.Days == 0)
                            CanClaimToday = false;
                        else if (diff.Days == 1)
                            CanClaimToday = true;
                        else
                        {
                            // Streak broken
                            CanClaimToday = true;
                            CurrentStreakDay = 0;
                        }
                    }
                    else
                    {
                        CanClaimToday = true;
                    }
                }
                else
                {
                    CanClaimToday = true;
                    CurrentStreakDay = 0;
                }

                OnRewardDataLoaded?.Invoke();
            });
        }

        public void ClaimDailyReward(string userId)
        {
            if (!CanClaimToday) return;

            int nextDay = (CurrentStreakDay % 7) + 1;
            DailyReward reward = RewardSchedule[nextDay - 1];

            // Apply tokens
            int tokensToAdd = reward.Tokens;
            if (PremiumSystem.Instance != null && PremiumSystem.Instance.IsPremiumActive)
                tokensToAdd = Mathf.RoundToInt(tokensToAdd * 1.5f);

            TokenSystem.Instance?.AddTokens(tokensToAdd, $"Daily Reward Day {nextDay}");

            // Apply bonus pack — generate a free market player of that tier
            if (!string.IsNullOrEmpty(reward.BonusPackTier))
            {
                if (System.Enum.TryParse<PlayerTier>(reward.BonusPackTier, ignoreCase: true, out PlayerTier packTier))
                {
                    var freePlayer = FootballPlayerData.GenerateRandomPlayer(packTier);
                    if (freePlayer != null)
                    {
                        var db = Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference;
                        string uid = GameManager.Instance?.CurrentUserId;
                        if (!string.IsNullOrEmpty(uid))
                            db.Child("squads").Child(uid).Child(freePlayer.PlayerId)
                              .SetValueAsync(freePlayer.ToFirebaseDictionary());
                    }
                }
            }

            CurrentStreakDay = nextDay;
            CanClaimToday = false;
            LastClaimDate = DateTime.UtcNow;

            // Save to Firebase
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            var updates = new Dictionary<string, object>
            {
                [$"users/{userId}/dailyReward/streakDay"] = CurrentStreakDay,
                [$"users/{userId}/dailyReward/lastClaimDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            };
            db.UpdateChildrenAsync(updates);

            OnRewardClaimed?.Invoke(reward);
        }

        public DailyReward GetNextReward()
        {
            int nextDay = (CurrentStreakDay % 7) + 1;
            return RewardSchedule[nextDay - 1];
        }
    }
}
