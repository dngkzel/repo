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
        public DateTime PremiumExpiry { get; private set; }

        public event Action<bool> OnPremiumStatusChanged;

        // Premium benefits
        public const float PREMIUM_TOKEN_MULTIPLIER = 1.5f;
        public const int PREMIUM_DAILY_BONUS_TOKENS = 100;
        public const bool PREMIUM_NO_ADS = true;
        public const int PREMIUM_EXTRA_MARKET_SLOTS = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadPremiumStatus(string userId)
        {
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            db.Child("users").Child(userId).Child("premium").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    long expiryTimestamp = (long)task.Result.Value;
                    PremiumExpiry = DateTimeOffset.FromUnixTimeSeconds(expiryTimestamp).DateTime;
                    IsPremiumActive = DateTime.UtcNow < PremiumExpiry;
                    OnPremiumStatusChanged?.Invoke(IsPremiumActive);
                }
            });
        }

        public void ActivatePremium(int days)
        {
            string userId = GameManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return;

            DateTime newExpiry = IsPremiumActive && DateTime.UtcNow < PremiumExpiry
                ? PremiumExpiry.AddDays(days)
                : DateTime.UtcNow.AddDays(days);

            PremiumExpiry = newExpiry;
            IsPremiumActive = true;

            long expiryTimestamp = new DateTimeOffset(newExpiry).ToUnixTimeSeconds();
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            db.Child("users").Child(userId).Child("premium").SetValueAsync(expiryTimestamp);

            // Grant welcome bonus
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

        // Alias for MarketSystem compatibility
        public bool IsPremium => IsPremiumActive;

        public float GetMarketRefreshDiscount() => IsPremiumActive ? 0.5f : 1f;

        public string GetExpiryText()
        {
            if (!IsPremiumActive) return "";
            TimeSpan remaining = PremiumExpiry - DateTime.UtcNow;
            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays} {LocalizationManager.Instance?.Get("days_remaining") ?? "days"}";
            return $"{(int)remaining.TotalHours} {LocalizationManager.Instance?.Get("hours_remaining") ?? "hours"}";
        }
    }
}
