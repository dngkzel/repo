using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using FootballGame.Player;

namespace FootballGame.Economy
{
    // ─────────────────────────────────────────────────────────────────────────────
    // MarketPlayer
    // ─────────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class MarketPlayer
    {
        public PlayerData player;
        public int        price;
        public DateTime   availableUntil;

        public MarketPlayer() { }

        public MarketPlayer(PlayerData player, int price, DateTime availableUntil)
        {
            this.player         = player;
            this.price          = price;
            this.availableUntil = availableUntil;
        }

        public bool IsExpired => DateTime.UtcNow >= availableUntil;

        // ── Firestore serialization ───────────────────────────────────────────────

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "player",         player.ToFirebaseDictionary() },
                { "price",          price },
                { "availableUntil", availableUntil.ToString("o") }
            };
        }

        public static MarketPlayer FromDictionary(Dictionary<string, object> d)
        {
            if (d == null) return null;

            var mp = new MarketPlayer();

            if (d.TryGetValue("player", out object playerObj) &&
                playerObj is Dictionary<string, object> playerDict)
            {
                mp.player = PlayerData.FromFirebaseDictionary(playerDict);
            }

            if (d.TryGetValue("price", out object priceObj) && priceObj != null)
                mp.price = Convert.ToInt32(priceObj);

            if (d.TryGetValue("availableUntil", out object auObj) && auObj != null &&
                DateTime.TryParse(auObj.ToString(), out DateTime au))
                mp.availableUntil = au;

            return mp;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MarketSystem
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Manages the player transfer market: generation, refresh, purchase, and filtering.
    /// Integrates with <see cref="TokenSystem"/> for spend validation and Firebase Firestore
    /// for persistence.
    /// </summary>
    public class MarketSystem : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        public const int MarketRefreshCost  = 100;
        public const int MarketSize         = 20;
        private const int DailyFreeRefreshHour = 0;   // midnight UTC reset

        // ── Firestore paths ───────────────────────────────────────────────────────
        private const string COL_MARKET = "market";

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string PREFS_LAST_FREE_REFRESH = "MarketSystem_LastFreeRefresh_";

        // ── Singleton ─────────────────────────────────────────────────────────────
        public static MarketSystem Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────────
        private List<MarketPlayer> _currentMarket    = new List<MarketPlayer>();
        private FirebaseFirestore  _firestore;
        private bool               _isFirebaseReady;
        private string             _userId;
        private DateTime           _nextRefreshTime;
        private DateTime           _lastFreeRefreshDate;
        private static readonly System.Random _rng = new System.Random();

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<List<MarketPlayer>> OnMarketRefreshed;
        public event Action<MarketPlayer>       OnPlayerPurchased;
        public event Action<string>             OnPurchaseFailed;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Time at which the next paid refresh becomes available (irrelevant for free refresh).</summary>
        public DateTime NextRefreshTime => _nextRefreshTime;

        /// <summary>Whether a free daily refresh is available.</summary>
        public bool IsRefreshAvailable => !IsFreeRefreshUsedToday();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Initialization ────────────────────────────────────────────────────────

        /// <summary>Called by GameManager after TokenSystem and PremiumSystem are ready.</summary>
        public void Initialize()
        {
            var authManager = Authentication.AuthManager.Instance;
            _userId = authManager != null && authManager.IsAuthenticated
                ? authManager.CurrentUser.UserId
                : string.Empty;

            LoadFreeRefreshDate();

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _firestore       = FirebaseFirestore.DefaultInstance;
                    _isFirebaseReady = true;
                    Debug.Log("[MarketSystem] Firebase Firestore ready.");

                    _ = LoadMarketFromFirebase();
                }
                else
                {
                    Debug.LogWarning($"[MarketSystem] Firebase not available: {task.Result}. Generating local market.");
                    GenerateMarketPlayers();
                }
            });
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the current market listing (may contain expired entries).</summary>
        public List<MarketPlayer> GetMarketPlayers() => new List<MarketPlayer>(_currentMarket);

        /// <summary>Returns only market players at the given position.</summary>
        public List<MarketPlayer> FilterByPosition(PlayerPosition position)
        {
            return _currentMarket
                .Where(mp => mp.player != null && mp.player.position == position)
                .ToList();
        }

        /// <summary>Returns only market players at the given tier.</summary>
        public List<MarketPlayer> FilterByTier(PlayerTier tier)
        {
            return _currentMarket
                .Where(mp => mp.player != null && mp.player.GetTier() == tier)
                .ToList();
        }

        /// <summary>
        /// Awards the free daily refresh (once per UTC day).
        /// Generates a new market without spending tokens.
        /// </summary>
        public async Task GetDailyFreeRefresh()
        {
            if (IsFreeRefreshUsedToday())
            {
                Debug.LogWarning("[MarketSystem] Free daily refresh already used today.");
                return;
            }

            await RefreshMarket(spendTokens: false);
            _lastFreeRefreshDate = DateTime.UtcNow.Date;
            SaveFreeRefreshDate();
        }

        /// <summary>
        /// Refreshes the market.  If <paramref name="spendTokens"/> is <c>true</c> and this is
        /// not a free refresh, deducts <see cref="MarketRefreshCost"/> tokens (adjusted for premium).
        /// </summary>
        public async Task RefreshMarket(bool spendTokens)
        {
            if (spendTokens)
            {
                var tokenSystem = TokenSystem.Instance;
                if (tokenSystem == null)
                {
                    Debug.LogError("[MarketSystem] TokenSystem not found.");
                    return;
                }

                int cost = GetRefreshCost();
                bool success = await tokenSystem.SpendTokens(cost, "Market Refresh");
                if (!success)
                {
                    Debug.LogWarning("[MarketSystem] Insufficient tokens for market refresh.");
                    OnPurchaseFailed?.Invoke($"Not enough tokens. You need {cost} tokens to refresh the market.");
                    return;
                }
            }

            GenerateMarketPlayers();
            await SaveMarketToFirebase();

            _nextRefreshTime = DateTime.UtcNow.AddHours(1);
            OnMarketRefreshed?.Invoke(new List<MarketPlayer>(_currentMarket));
            Debug.Log($"[MarketSystem] Market refreshed with {_currentMarket.Count} players.");
        }

        /// <summary>
        /// Purchases <paramref name="marketPlayer"/> from the market, spending the listed price.
        /// On success, the player is added to the user's team via DataManager.
        /// </summary>
        public async Task BuyMarketPlayer(MarketPlayer marketPlayer)
        {
            if (marketPlayer == null || marketPlayer.player == null)
            {
                Debug.LogError("[MarketSystem] BuyMarketPlayer: null market player.");
                return;
            }

            if (marketPlayer.IsExpired)
            {
                Debug.LogWarning("[MarketSystem] BuyMarketPlayer: listing has expired.");
                OnPurchaseFailed?.Invoke("This player listing has expired. Please refresh the market.");
                return;
            }

            if (!_currentMarket.Contains(marketPlayer))
            {
                Debug.LogWarning("[MarketSystem] BuyMarketPlayer: player not in current market.");
                OnPurchaseFailed?.Invoke("Player is no longer available.");
                return;
            }

            var tokenSystem = TokenSystem.Instance;
            if (tokenSystem == null)
            {
                Debug.LogError("[MarketSystem] TokenSystem not found.");
                return;
            }

            bool success = await tokenSystem.SpendTokens(
                marketPlayer.price,
                $"Bought {marketPlayer.player.name}"
            );

            if (!success)
            {
                string msg = $"Not enough tokens. You need {marketPlayer.price} tokens.";
                Debug.LogWarning($"[MarketSystem] {msg}");
                OnPurchaseFailed?.Invoke(msg);
                return;
            }

            // Remove from market
            _currentMarket.Remove(marketPlayer);
            await SaveMarketToFirebase();

            Debug.Log($"[MarketSystem] Purchased '{marketPlayer.player.name}' for {marketPlayer.price} tokens.");
            OnPlayerPurchased?.Invoke(marketPlayer);
        }

        // ── Market Generation ─────────────────────────────────────────────────────

        /// <summary>
        /// Generates a fresh set of 20 market players distributed across all tiers.
        /// Tier distribution: Bronze 6, Silver 6, Gold 4, Platinum 3, Legend 1.
        /// </summary>
        public void GenerateMarketPlayers()
        {
            _currentMarket.Clear();

            _currentMarket.AddRange(GenerateTierPlayers(PlayerTier.Bronze,   6));
            _currentMarket.AddRange(GenerateTierPlayers(PlayerTier.Silver,   6));
            _currentMarket.AddRange(GenerateTierPlayers(PlayerTier.Gold,     4));
            _currentMarket.AddRange(GenerateTierPlayers(PlayerTier.Platinum, 3));
            _currentMarket.AddRange(GenerateTierPlayers(PlayerTier.Legend,   1));

            // Shuffle
            for (int i = _currentMarket.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_currentMarket[i], _currentMarket[j]) = (_currentMarket[j], _currentMarket[i]);
            }
        }

        /// <summary>Generates <paramref name="count"/> market players of the specified tier.</summary>
        public List<MarketPlayer> GenerateTierPlayers(PlayerTier tier, int count)
        {
            var result = new List<MarketPlayer>(count);

            // Listings last 24 hours with slight jitter (±2h) to stagger expiry
            DateTime baseExpiry = DateTime.UtcNow.AddHours(24);

            for (int i = 0; i < count; i++)
            {
                PlayerData player = PlayerData.GenerateRandomPlayer(tier);

                // Price: random within the tier's token range, biased toward the midpoint
                (int min, int max) = player.GetTokenRange();
                int spread = max - min;
                int price  = min + (int)(spread * (0.3f + (float)_rng.NextDouble() * 0.5f));
                price = Mathf.RoundToInt(price / 10f) * 10;   // round to nearest 10

                DateTime expiry = baseExpiry.AddMinutes(_rng.Next(-120, 121));

                result.Add(new MarketPlayer(player, price, expiry));
            }

            return result;
        }

        // ── Firebase Persistence ─────────────────────────────────────────────────

        /// <summary>Loads the market state from Firestore; generates a new market on miss.</summary>
        public async Task LoadMarketFromFirebase()
        {
            if (!_isFirebaseReady || string.IsNullOrEmpty(_userId))
            {
                GenerateMarketPlayers();
                return;
            }

            try
            {
                DocumentReference doc      = _firestore.Collection(COL_MARKET).Document(_userId);
                DocumentSnapshot  snapshot = await doc.GetSnapshotAsync();

                if (snapshot.Exists && snapshot.ContainsField("players"))
                {
                    var rawList = snapshot.GetValue<List<object>>("players");
                    _currentMarket.Clear();

                    if (rawList != null)
                    {
                        foreach (object item in rawList)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict == null) continue;

                            MarketPlayer mp = MarketPlayer.FromDictionary(dict);
                            if (mp != null && !mp.IsExpired)
                                _currentMarket.Add(mp);
                        }
                    }

                    Debug.Log($"[MarketSystem] Loaded {_currentMarket.Count} non-expired market players from Firebase.");

                    // If all listings expired, regenerate
                    if (_currentMarket.Count == 0)
                    {
                        Debug.Log("[MarketSystem] All listings expired. Generating fresh market.");
                        GenerateMarketPlayers();
                        await SaveMarketToFirebase();
                    }
                }
                else
                {
                    Debug.Log("[MarketSystem] No market data found. Generating fresh market.");
                    GenerateMarketPlayers();
                    await SaveMarketToFirebase();
                }

                if (snapshot.ContainsField("nextRefreshTime") &&
                    DateTime.TryParse(snapshot.GetValue<string>("nextRefreshTime"), out DateTime nrt))
                {
                    _nextRefreshTime = nrt;
                }

                OnMarketRefreshed?.Invoke(new List<MarketPlayer>(_currentMarket));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MarketSystem] LoadMarketFromFirebase error: {ex.Message}. Generating local market.");
                GenerateMarketPlayers();
            }
        }

        /// <summary>Saves the current market state to Firestore.</summary>
        public async Task SaveMarketToFirebase()
        {
            if (!_isFirebaseReady || string.IsNullOrEmpty(_userId)) return;

            try
            {
                var playersList = new List<object>();
                foreach (MarketPlayer mp in _currentMarket)
                    playersList.Add(mp.ToDictionary());

                var data = new Dictionary<string, object>
                {
                    { "players",         playersList },
                    { "nextRefreshTime", _nextRefreshTime.ToString("o") },
                    { "updatedAt",       DateTime.UtcNow.ToString("o") }
                };

                DocumentReference doc = _firestore.Collection(COL_MARKET).Document(_userId);
                await doc.SetAsync(data);
                Debug.Log("[MarketSystem] Market saved to Firebase.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MarketSystem] SaveMarketToFirebase error: {ex.Message}");
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private int GetRefreshCost()
        {
            var premium = PremiumSystem.Instance;
            if (premium != null && premium.IsPremium)
                return Mathf.RoundToInt(MarketRefreshCost * premium.GetMarketRefreshDiscount());
            return MarketRefreshCost;
        }

        private bool IsFreeRefreshUsedToday()
        {
            return _lastFreeRefreshDate.Date == DateTime.UtcNow.Date;
        }

        private void LoadFreeRefreshDate()
        {
            string key = PREFS_LAST_FREE_REFRESH + (_userId ?? "anon");
            if (PlayerPrefs.HasKey(key) &&
                DateTime.TryParse(PlayerPrefs.GetString(key), out DateTime d))
            {
                _lastFreeRefreshDate = d;
            }
            else
            {
                _lastFreeRefreshDate = DateTime.MinValue;
            }
        }

        private void SaveFreeRefreshDate()
        {
            string key = PREFS_LAST_FREE_REFRESH + (_userId ?? "anon");
            PlayerPrefs.SetString(key, _lastFreeRefreshDate.ToString("o"));
            PlayerPrefs.Save();
        }
    }
}
