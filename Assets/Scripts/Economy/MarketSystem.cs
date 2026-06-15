using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using FootballGame.Player;

namespace FootballGame.Economy
{
    [Serializable]
    public class MarketPlayer
    {
        public FootballPlayerData player;
        public int price;
        public DateTime availableUntil;

        public MarketPlayer() { }
        public MarketPlayer(FootballPlayerData p, int price, DateTime expiry)
        { this.player = p; this.price = price; availableUntil = expiry; }

        public bool IsExpired => DateTime.UtcNow >= availableUntil;

        public Dictionary<string, object> ToDictionary() => new Dictionary<string, object>
        {
            ["player"] = player?.ToFirebaseDictionary(),
            ["price"] = price,
            ["availableUntil"] = availableUntil.ToString("o"),
        };

        public static MarketPlayer FromDictionary(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var mp = new MarketPlayer();
            if (d.TryGetValue("player", out var po) && po is Dictionary<string, object> pd)
                mp.player = FootballPlayerData.FromFirebaseDictionary(pd);
            if (d.TryGetValue("price", out var pr)) mp.price = Convert.ToInt32(pr);
            if (d.TryGetValue("availableUntil", out var au) && DateTime.TryParse(au?.ToString(), out DateTime dt))
                mp.availableUntil = dt;
            return mp;
        }
    }

    public class MarketSystem : MonoBehaviour
    {
        public static MarketSystem Instance { get; private set; }

        public const int MarketRefreshCost = 100;

        private List<MarketPlayer> _market = new List<MarketPlayer>();
        private FirebaseFirestore _firestore;
        private bool _firestoreReady;
        private string _userId;
        private DateTime _lastFreeRefresh = DateTime.MinValue;

        public event Action<List<MarketPlayer>> OnMarketRefreshed;
        public event Action<MarketPlayer> OnPlayerPurchased;
        public event Action<string> OnPurchaseFailed;

        private static readonly System.Random _rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            _userId = Authentication.AuthManager.Instance?.CurrentUser?.UserId ?? "";
            LoadFreeRefreshDate();

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _firestore = FirebaseFirestore.DefaultInstance;
                    _firestoreReady = true;
                    _ = LoadMarketFromFirebase();
                }
                else
                {
                    GenerateMarketPlayers();
                }
            });
        }

        public List<MarketPlayer> GetMarketPlayers() => new List<MarketPlayer>(_market);

        public List<MarketPlayer> FilterByTier(PlayerTier tier) =>
            _market.Where(mp => mp.player?.GetTier() == tier).ToList();

        public List<MarketPlayer> FilterByPosition(PlayerPosition pos) =>
            _market.Where(mp => mp.player?.position == pos).ToList();

        public bool IsRefreshAvailable => _lastFreeRefresh.Date < DateTime.UtcNow.Date;

        public async Task GetDailyFreeRefresh()
        {
            if (!IsRefreshAvailable) return;
            await RefreshMarket(false);
            _lastFreeRefresh = DateTime.UtcNow;
            SaveFreeRefreshDate();
        }

        public async Task RefreshMarket(bool spendTokens)
        {
            if (spendTokens)
            {
                int cost = PremiumSystem.Instance?.IsPremium == true
                    ? Mathf.RoundToInt(MarketRefreshCost * PremiumSystem.Instance.GetMarketRefreshDiscount())
                    : MarketRefreshCost;

                if (!(TokenSystem.Instance?.SpendTokens(cost, "Market Refresh") ?? false))
                {
                    OnPurchaseFailed?.Invoke($"Need {cost} tokens to refresh.");
                    return;
                }
            }
            GenerateMarketPlayers();
            await SaveMarketToFirebase();
            OnMarketRefreshed?.Invoke(new List<MarketPlayer>(_market));
        }

        public async Task BuyMarketPlayer(MarketPlayer mp)
        {
            if (mp == null || mp.player == null) return;
            if (mp.IsExpired) { OnPurchaseFailed?.Invoke("Listing expired."); return; }

            if (!(TokenSystem.Instance?.SpendTokens(mp.price, $"Bought {mp.player.Name}") ?? false))
            {
                OnPurchaseFailed?.Invoke($"Need {mp.price} tokens.");
                return;
            }

            mp.player.OwnerId = _userId;
            // Use ID-based lookup to avoid reference-equality failures after Firebase reload
            var existing = _market.Find(m => m.player?.PlayerId == mp.player?.PlayerId);
            if (existing == null) { OnPurchaseFailed?.Invoke("No longer available."); return; }
            _market.Remove(existing);

            if (!string.IsNullOrEmpty(_userId))
            {
                var db = Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference;
                await db.Child("squads").Child(_userId).Child(mp.player.PlayerId)
                    .SetValueAsync(mp.player.ToFirebaseDictionary());
            }

            await SaveMarketToFirebase();
            OnPlayerPurchased?.Invoke(mp);
        }

        public void GenerateMarketPlayers()
        {
            _market.Clear();
            _market.AddRange(MakePlayers(PlayerTier.Bronze, 6));
            _market.AddRange(MakePlayers(PlayerTier.Silver, 6));
            _market.AddRange(MakePlayers(PlayerTier.Gold, 4));
            _market.AddRange(MakePlayers(PlayerTier.Platinum, 3));
            _market.AddRange(MakePlayers(PlayerTier.Legend, 1));

            for (int i = _market.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_market[i], _market[j]) = (_market[j], _market[i]);
            }
        }

        private List<MarketPlayer> MakePlayers(PlayerTier tier, int count)
        {
            var result = new List<MarketPlayer>(count);
            DateTime base_expiry = DateTime.UtcNow.AddHours(24);
            for (int i = 0; i < count; i++)
            {
                var p = FootballPlayerData.GenerateRandomPlayer(tier);
                (int min, int max) = p.GetTokenRange();
                int price = min + (int)((max - min) * (0.3 + _rng.NextDouble() * 0.5));
                price = Mathf.RoundToInt(price / 10f) * 10;
                result.Add(new MarketPlayer(p, price, base_expiry.AddMinutes(_rng.Next(-120, 121))));
            }
            return result;
        }

        private async Task LoadMarketFromFirebase()
        {
            if (!_firestoreReady || string.IsNullOrEmpty(_userId)) { GenerateMarketPlayers(); return; }
            try
            {
                var snap = await _firestore.Collection("market").Document(_userId).GetSnapshotAsync();
                if (snap.Exists && snap.ContainsField("players"))
                {
                    _market.Clear();
                    var list = snap.GetValue<List<object>>("players");
                    if (list != null)
                        foreach (var item in list)
                        {
                            var mp = MarketPlayer.FromDictionary(item as Dictionary<string, object>);
                            if (mp != null && !mp.IsExpired) _market.Add(mp);
                        }
                    if (_market.Count == 0) { GenerateMarketPlayers(); await SaveMarketToFirebase(); }
                }
                else { GenerateMarketPlayers(); await SaveMarketToFirebase(); }
                OnMarketRefreshed?.Invoke(new List<MarketPlayer>(_market));
            }
            catch { GenerateMarketPlayers(); }
        }

        private async Task SaveMarketToFirebase()
        {
            if (!_firestoreReady || string.IsNullOrEmpty(_userId)) return;
            try
            {
                var playersList = _market.Select(mp => (object)mp.ToDictionary()).ToList();
                await _firestore.Collection("market").Document(_userId).SetAsync(
                    new Dictionary<string, object> { ["players"] = playersList, ["updatedAt"] = DateTime.UtcNow.ToString("o") });
            }
            catch (Exception ex) { Debug.LogError($"[MarketSystem] Save error: {ex.Message}"); }
        }

        private void LoadFreeRefreshDate()
        {
            string key = $"MktFreeRefresh_{_userId ?? "anon"}";
            if (PlayerPrefs.HasKey(key) && DateTime.TryParse(PlayerPrefs.GetString(key), out DateTime d))
                _lastFreeRefresh = d;
        }

        private void SaveFreeRefreshDate()
        {
            PlayerPrefs.SetString($"MktFreeRefresh_{_userId ?? "anon"}", _lastFreeRefresh.ToString("o"));
            PlayerPrefs.Save();
        }
    }
}
