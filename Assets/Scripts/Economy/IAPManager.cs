using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using FootballGame.Core;
using FootballGame.Player;

namespace FootballGame.Economy
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        public static IAPManager Instance { get; private set; }

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        // Token packages
        public const string TOKENS_100 = "com.footballgame.tokens_100";
        public const string TOKENS_500 = "com.footballgame.tokens_500";
        public const string TOKENS_1200 = "com.footballgame.tokens_1200";
        public const string TOKENS_2500 = "com.footballgame.tokens_2500";
        public const string TOKENS_6000 = "com.footballgame.tokens_6000";

        // Premium subscriptions
        public const string PREMIUM_MONTHLY = "com.footballgame.premium_monthly";
        public const string PREMIUM_YEARLY = "com.footballgame.premium_yearly";

        public static readonly Dictionary<string, int> TokenAmounts = new Dictionary<string, int>
        {
            { TOKENS_100, 100 },
            { TOKENS_500, 550 },    // bonus 50
            { TOKENS_1200, 1350 },  // bonus 150
            { TOKENS_2500, 2900 },  // bonus 400
            { TOKENS_6000, 7200 },  // bonus 1200
        };

        public event Action<string> OnPurchaseSuccess;
        public event Action<string> OnPurchaseFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }

        private void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(TOKENS_100, ProductType.Consumable);
            builder.AddProduct(TOKENS_500, ProductType.Consumable);
            builder.AddProduct(TOKENS_1200, ProductType.Consumable);
            builder.AddProduct(TOKENS_2500, ProductType.Consumable);
            builder.AddProduct(TOKENS_6000, ProductType.Consumable);
            builder.AddProduct(PREMIUM_MONTHLY, ProductType.Subscription);
            builder.AddProduct(PREMIUM_YEARLY, ProductType.Subscription);

            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyTokens(string productId)
        {
            if (_storeController != null)
                _storeController.InitiatePurchase(productId);
            else
                Debug.LogError("IAP not initialized");
        }

        public void BuyPremium(bool yearly = false)
        {
            string id = yearly ? PREMIUM_YEARLY : PREMIUM_MONTHLY;
            if (_storeController != null)
                _storeController.InitiatePurchase(id);
        }

        public string GetProductPrice(string productId)
        {
            if (_storeController == null) return "...";
            var product = _storeController.products.WithID(productId);
            return product != null ? product.metadata.localizedPriceString : "N/A";
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            Debug.Log("IAP Initialized successfully");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"IAP Init Failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"IAP Init Failed: {error} - {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;

            if (TokenAmounts.TryGetValue(productId, out int tokens))
            {
                TokenSystem.Instance?.AddTokens(tokens, $"IAP Purchase: {productId}");
                OnPurchaseSuccess?.Invoke(productId);
                Debug.Log($"Purchased {tokens} tokens via {productId}");
            }
            else if (productId == PREMIUM_MONTHLY || productId == PREMIUM_YEARLY)
            {
                int days = productId == PREMIUM_YEARLY ? 365 : 30;
                PremiumSystem.Instance?.ActivatePremium(days);
                OnPurchaseSuccess?.Invoke(productId);
                Debug.Log($"Premium activated for {days} days");
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"Purchase failed: {product.definition.id} - {failureDescription.reason}");
            OnPurchaseFailed?.Invoke(product.definition.id);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"Purchase failed: {product.definition.id} - {failureReason}");
            OnPurchaseFailed?.Invoke(product.definition.id);
        }
    }
}
