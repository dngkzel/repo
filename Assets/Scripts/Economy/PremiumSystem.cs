using System;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using FootballGame.Core;

namespace FootballGame.Economy
{
    public class PremiumSystem : MonoBehaviour
    {
        public static PremiumSystem Instance { get; private set; }

        public bool IsPremiumActive { get; private set; }
        public bool IsPremium => IsPremiumActive;
        public DateTime PremiumExpiry { get; private set; }

        public event Action<bool> OnPremiumStatusChanged;

        public const float PREMIUM_TOKEN_MULTIPLIER = 1.5f;
        public const int PREMIUM_DAILY_BONUS_TOKENS = 100;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadPremiumStatus(string userId)
        {
            FirebaseDatabase.DefaultInstance.RootReference
                .Child("users").Child(userId).Child("premium")
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        long ts = (long)task.Result.Value;
                        PremiumExpiry = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
                        IsPremiumActive = DateTime.UtcNow < PremiumExpiry;
                        OnPremiumStatusChanged?.Invoke(IsPremiumActive);
                    }
                });
        }

        public void ActivatePremium(int days)
        {
            string userId = GameManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return;

            PremiumExpiry = IsPremiumActive && DateTime.UtcNow < PremiumExpiry
                ? PremiumExpiry.AddDays(days)
                : DateTime.UtcNow.AddDays(days);
            IsPremiumActive = true;

            FirebaseDatabase.DefaultInstance.RootReference
                .Child("users").Child(userId).Child("premium")
                .SetValueAsync(new DateTimeOffset(PremiumExpiry).ToUnixTimeSeconds());

            TokenSystem.Instance?.AddTokens(200, "Premium Welcome Bonus");
            OnPremiumStatusChanged?.Invoke(true);
        }

        public void CheckAndUpdateStatus()
        {
            if (IsPremiumActive && DateTime.UtcNow >= PremiumExpiry)
            {
                IsPremiumActive = false;
                OnPremiumStatusChanged?.Invoke(false);
            }
        }

        // Returns a multiplier (0.5 = 50% discount on market refresh for premium users)
        public float GetMarketRefreshDiscount() => IsPremiumActive ? 0.5f : 1f;

        public string GetExpiryText()
        {
            if (!IsPremiumActive) return "";
            TimeSpan r = PremiumExpiry - DateTime.UtcNow;
            if (r.TotalDays >= 1) return $"{(int)r.TotalDays} {LocalizationManager.Instance?.Get("days_remaining") ?? "days"}";
            return $"{(int)r.TotalHours} {LocalizationManager.Instance?.Get("hours_remaining") ?? "hours"}";
        }
    }
}
