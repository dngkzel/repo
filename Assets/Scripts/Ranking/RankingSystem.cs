using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using FootballGame.Core;

namespace FootballGame.Ranking
{
    public enum RankingScope { World, Country, City }

    public class RankingSystem : MonoBehaviour
    {
        public static RankingSystem Instance { get; private set; }

        private Dictionary<RankingScope, List<RankEntry>> _cache = new Dictionary<RankingScope, List<RankEntry>>
        {
            { RankingScope.World,   new List<RankEntry>() },
            { RankingScope.Country, new List<RankEntry>() },
            { RankingScope.City,    new List<RankEntry>() },
        };

        public event Action<List<RankEntry>, RankingScope> OnRankingLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadRankings(RankingScope scope, string filterValue = null)
        {
            var db = FirebaseDatabase.DefaultInstance.RootReference;
            Query q;
            switch (scope)
            {
                case RankingScope.Country when !string.IsNullOrEmpty(filterValue):
                    q = db.Child("rankings").OrderByChild("country").EqualTo(filterValue).LimitToLast(100); break;
                case RankingScope.City when !string.IsNullOrEmpty(filterValue):
                    q = db.Child("rankings").OrderByChild("city").EqualTo(filterValue).LimitToLast(100); break;
                default:
                    q = db.Child("rankings").OrderByChild("totalPoints").LimitToLast(100); break;
            }

            q.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || !task.Result.Exists) { OnRankingLoaded?.Invoke(new List<RankEntry>(), scope); return; }

                var entries = new List<RankEntry>();
                foreach (var child in task.Result.Children)
                {
                    entries.Add(new RankEntry
                    {
                        UserId      = child.Key,
                        DisplayName = child.Child("displayName").Value?.ToString() ?? "",
                        TeamName    = child.Child("teamName").Value?.ToString() ?? "",
                        Country     = child.Child("country").Value?.ToString() ?? "",
                        City        = child.Child("city").Value?.ToString() ?? "",
                        Points      = int.TryParse(child.Child("totalPoints").Value?.ToString(), out int p) ? p : 0,
                        Wins        = int.TryParse(child.Child("wins").Value?.ToString(), out int w) ? w : 0,
                        Draws       = int.TryParse(child.Child("draws").Value?.ToString(), out int d) ? d : 0,
                        Losses      = int.TryParse(child.Child("losses").Value?.ToString(), out int l) ? l : 0,
                        GoalsFor    = int.TryParse(child.Child("goalsFor").Value?.ToString(), out int gf) ? gf : 0,
                        GoalsAgainst= int.TryParse(child.Child("goalsAgainst").Value?.ToString(), out int ga) ? ga : 0,
                    });
                }

                entries = entries.OrderByDescending(e => e.Points)
                                 .ThenByDescending(e => e.GoalDifference)
                                 .ThenByDescending(e => e.GoalsFor).ToList();

                for (int i = 0; i < entries.Count; i++) entries[i].Rank = i + 1;
                _cache[scope] = entries;
                OnRankingLoaded?.Invoke(entries, scope);
            });
        }

        public void UpdatePlayerRanking(string userId, string displayName, string teamName,
            string country, string city, bool win, bool draw, int gf, int ga)
        {
            int pts = win ? 3 : draw ? 1 : 0;
            var db = FirebaseDatabase.DefaultInstance.RootReference.Child("rankings").Child(userId);

            db.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                int curPts = 0, wins = 0, draws = 0, losses = 0, totalGF = 0, totalGA = 0;
                if (task.IsCompleted && task.Result.Exists)
                {
                    var d = task.Result;
                    curPts  = int.TryParse(d.Child("totalPoints").Value?.ToString(), out int tp) ? tp : 0;
                    wins    = int.TryParse(d.Child("wins").Value?.ToString(), out int w) ? w : 0;
                    draws   = int.TryParse(d.Child("draws").Value?.ToString(), out int dr) ? dr : 0;
                    losses  = int.TryParse(d.Child("losses").Value?.ToString(), out int l) ? l : 0;
                    totalGF = int.TryParse(d.Child("goalsFor").Value?.ToString(), out int _gf) ? _gf : 0;
                    totalGA = int.TryParse(d.Child("goalsAgainst").Value?.ToString(), out int _ga) ? _ga : 0;
                }

                db.UpdateChildrenAsync(new Dictionary<string, object>
                {
                    ["displayName"] = displayName, ["teamName"] = teamName,
                    ["country"] = country, ["city"] = city,
                    ["totalPoints"] = curPts + pts,
                    ["wins"]   = wins + (win ? 1 : 0),
                    ["draws"]  = draws + (draw ? 1 : 0),
                    ["losses"] = losses + (!win && !draw ? 1 : 0),
                    ["goalsFor"] = totalGF + gf,
                    ["goalsAgainst"] = totalGA + ga,
                    ["lastUpdated"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                });
            });
        }

        public List<RankEntry> GetCached(RankingScope scope) =>
            _cache.TryGetValue(scope, out var list) ? list : new List<RankEntry>();

        public int GetMyRank(string userId, RankingScope scope)
        {
            var entry = GetCached(scope).FirstOrDefault(e => e.UserId == userId);
            return entry?.Rank ?? -1;
        }
    }
}
