using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace FootballGame.Economy
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        public static IAPManager Instance { get; private set; }

        public const string TOKENS_100  = "com.footballgame.tokens_100";
        public const string TOKENS_500  = "com.footballgame.tokens_500";
        public const string TOKENS_1200 = "com.footballgame.tokens_1200";
        public const string TOKENS_2500 = "com.footballgame.tokens_2500";
        public const string TOKENS_6000 = "com.footballgame.tokens_6000";
        public const string PREMIUM_MONTHLY = "com.footballgame.premium_monthly";
        public const string PREMIUM_YEARLY  = "com.footballgame.premium_yearly";

        public static readonly Dictionary<string, int> TokenAmounts = new Dictionary<string, int>
        {
            { TOKENS_100,  100  },
            { TOKENS_500,  550  },
            { TOKENS_1200, 1350 },
            { TOKENS_2500, 2900 },
            { TOKENS_6000, 7200 },
        };

        public event Action<string> OnPurchaseSuccess;
        public event Action<string> OnPurchaseFailed;

        private IStoreController _store;
        private IExtensionProvider _extensions;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitIAP();
        }

        private void InitIAP()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(TOKENS_100,       ProductType.Consumable);
            builder.AddProduct(TOKENS_500,       ProductType.Consumable);
            builder.AddProduct(TOKENS_1200,      ProductType.Consumable);
            builder.AddProduct(TOKENS_2500,      ProductType.Consumable);
            builder.AddProduct(TOKENS_6000,      ProductType.Consumable);
            builder.AddProduct(PREMIUM_MONTHLY,  ProductType.Subscription);
            builder.AddProduct(PREMIUM_YEARLY,   ProductType.Subscription);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyTokens(string productId)
        {
            if (_store != null) _store.InitiatePurchase(productId);
        }

        public void BuyPremium(bool yearly = false) =>
            BuyTokens(yearly ? PREMIUM_YEARLY : PREMIUM_MONTHLY);

        public string GetProductPrice(string productId)
        {
            if (_store == null) return "...";
            var p = _store.products.WithID(productId);
            return p?.metadata.localizedPriceString ?? "N/A";
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _extensions = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error) =>
            Debug.LogError($"IAP init failed: {error}");

        public void OnInitializeFailed(InitializationFailureReason error, string message) =>
            Debug.LogError($"IAP init failed: {error} — {message}");

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string id = args.purchasedProduct.definition.id;
            if (TokenAmounts.TryGetValue(id, out int tokens))
            {
                TokenSystem.Instance?.AddTokens(tokens, $"IAP: {id}");
                OnPurchaseSuccess?.Invoke(id);
            }
            else if (id == PREMIUM_MONTHLY || id == PREMIUM_YEARLY)
            {
                PremiumSystem.Instance?.ActivatePremium(id == PREMIUM_YEARLY ? 365 : 30);
                OnPurchaseSuccess?.Invoke(id);
            }
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription desc)
        {
            Debug.LogError($"Purchase failed: {product.definition.id} — {desc.reason}");
            OnPurchaseFailed?.Invoke(product.definition.id);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogError($"Purchase failed: {product.definition.id} — {reason}");
            OnPurchaseFailed?.Invoke(product.definition.id);
        }
    }
}
