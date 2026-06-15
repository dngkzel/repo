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

    [Serializable]
    public class RankingEntry
    {
        public string UserId;
        public string DisplayName;
        public string TeamName;
        public string Country;
        public string City;
        public int TotalPoints;
        public int Wins;
        public int Draws;
        public int Losses;
        public int GoalsFor;
        public int GoalsAgainst;
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Rank;
    }

    public class RankingSystem : MonoBehaviour
    {
        public static RankingSystem Instance { get; private set; }

        private List<RankingEntry> _worldRanking = new List<RankingEntry>();
        private List<RankingEntry> _countryRanking = new List<RankingEntry>();
        private List<RankingEntry> _cityRanking = new List<RankingEntry>();

        public event Action<List<RankingEntry>, RankingScope> OnRankingLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadRankings(RankingScope scope, string filterValue = null)
        {
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            Query query = db.Child("rankings").OrderByChild("totalPoints").LimitToLast(100);

            if (scope == RankingScope.Country && !string.IsNullOrEmpty(filterValue))
                query = db.Child("rankings").OrderByChild("country").EqualTo(filterValue).LimitToLast(100);
            else if (scope == RankingScope.City && !string.IsNullOrEmpty(filterValue))
                query = db.Child("rankings").OrderByChild("city").EqualTo(filterValue).LimitToLast(100);

            query.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || !task.Result.Exists) return;

                var entries = new List<RankingEntry>();
                foreach (var child in task.Result.Children)
                {
                    var e = new RankingEntry
                    {
                        UserId = child.Key,
                        DisplayName = child.Child("displayName").Value?.ToString() ?? "Unknown",
                        TeamName = child.Child("teamName").Value?.ToString() ?? "Unknown FC",
                        Country = child.Child("country").Value?.ToString() ?? "",
                        City = child.Child("city").Value?.ToString() ?? "",
                        TotalPoints = int.TryParse(child.Child("totalPoints").Value?.ToString(), out int pts) ? pts : 0,
                        Wins = int.TryParse(child.Child("wins").Value?.ToString(), out int w) ? w : 0,
                        Draws = int.TryParse(child.Child("draws").Value?.ToString(), out int d) ? d : 0,
                        Losses = int.TryParse(child.Child("losses").Value?.ToString(), out int l) ? l : 0,
                        GoalsFor = int.TryParse(child.Child("goalsFor").Value?.ToString(), out int gf) ? gf : 0,
                        GoalsAgainst = int.TryParse(child.Child("goalsAgainst").Value?.ToString(), out int ga) ? ga : 0,
                    };
                    entries.Add(e);
                }

                entries = entries.OrderByDescending(e => e.TotalPoints)
                                 .ThenByDescending(e => e.GoalDifference)
                                 .ThenByDescending(e => e.GoalsFor)
                                 .ToList();

                for (int i = 0; i < entries.Count; i++)
                    entries[i].Rank = i + 1;

                switch (scope)
                {
                    case RankingScope.World: _worldRanking = entries; break;
                    case RankingScope.Country: _countryRanking = entries; break;
                    case RankingScope.City: _cityRanking = entries; break;
                }

                OnRankingLoaded?.Invoke(entries, scope);
            });
        }

        public void UpdatePlayerRanking(string userId, string displayName, string teamName,
            string country, string city, bool win, bool draw, int goalsFor, int goalsAgainst)
        {
            int points = win ? 3 : draw ? 1 : 0;
            DatabaseReference db = FirebaseDatabase.DefaultInstance.RootReference;
            DatabaseReference rankRef = db.Child("rankings").Child(userId);

            rankRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                int currentPoints = 0, wins = 0, draws = 0, losses = 0, totalGF = 0, totalGA = 0;

                if (task.IsCompleted && task.Result.Exists)
                {
                    var data = task.Result;
                    currentPoints = int.TryParse(data.Child("totalPoints").Value?.ToString(), out int p) ? p : 0;
                    wins = int.TryParse(data.Child("wins").Value?.ToString(), out int w) ? w : 0;
                    draws = int.TryParse(data.Child("draws").Value?.ToString(), out int d) ? d : 0;
                    losses = int.TryParse(data.Child("losses").Value?.ToString(), out int l) ? l : 0;
                    totalGF = int.TryParse(data.Child("goalsFor").Value?.ToString(), out int gf) ? gf : 0;
                    totalGA = int.TryParse(data.Child("goalsAgainst").Value?.ToString(), out int ga) ? ga : 0;
                }

                var updates = new Dictionary<string, object>
                {
                    ["displayName"] = displayName,
                    ["teamName"] = teamName,
                    ["country"] = country,
                    ["city"] = city,
                    ["totalPoints"] = currentPoints + points,
                    ["wins"] = wins + (win ? 1 : 0),
                    ["draws"] = draws + (draw ? 1 : 0),
                    ["losses"] = losses + (!win && !draw ? 1 : 0),
                    ["goalsFor"] = totalGF + goalsFor,
                    ["goalsAgainst"] = totalGA + goalsAgainst,
                    ["lastUpdated"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };

                rankRef.UpdateChildrenAsync(updates);
            });
        }

        public List<RankingEntry> GetCachedRanking(RankingScope scope)
        {
            return scope switch
            {
                RankingScope.World => _worldRanking,
                RankingScope.Country => _countryRanking,
                RankingScope.City => _cityRanking,
                _ => _worldRanking,
            };
        }

        public int GetMyRank(string userId, RankingScope scope)
        {
            var list = GetCachedRanking(scope);
            var entry = list.FirstOrDefault(e => e.UserId == userId);
            return entry?.Rank ?? -1;
        }
    }
}
