using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;

namespace FootballGame.Core
{
    public class DataManager : MonoBehaviour
    {
        private DatabaseReference _db;
        private const string PLAYERS_PATH = "players";
        private const string TEAMS_PATH = "teams";
        private const string RANKINGS_PATH = "rankings";
        private const string MARKET_PATH = "market";
        private const string TRANSFERS_PATH = "transfers";

        private void Awake()
        {
            _db = FirebaseDatabase.DefaultInstance.RootReference;
        }

        // ---- Player Data ----
        public IEnumerator LoadPlayerData(string userId, Action<Player.PlayerData> callback)
        {
            bool done = false;
            Player.PlayerData result = null;
            _db.Child(PLAYERS_PATH).Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string json = task.Result.GetRawJsonValue();
                    result = JsonConvert.DeserializeObject<Player.PlayerData>(json);
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator SavePlayerData(Player.PlayerData player, Action<bool> callback = null)
        {
            bool done = false;
            bool success = false;
            string json = JsonConvert.SerializeObject(player);
            _db.Child(PLAYERS_PATH).Child(player.UserId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                success = task.IsCompleted && !task.IsFaulted;
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(success);
        }

        public void SavePlayerTokenBalance(string userId, int balance)
        {
            _db.Child(PLAYERS_PATH).Child(userId).Child("tokenBalance").SetValueAsync(balance);
        }

        public void SavePlayerPremiumStatus(string userId, bool isPremium, long expiryTimestamp)
        {
            var updates = new Dictionary<string, object>
            {
                ["isPremium"] = isPremium,
                ["premiumExpiry"] = expiryTimestamp
            };
            _db.Child(PLAYERS_PATH).Child(userId).UpdateChildrenAsync(updates);
        }

        public void SaveLastLoginDate(string userId, string dateString)
        {
            _db.Child(PLAYERS_PATH).Child(userId).Child("lastLoginDate").SetValueAsync(dateString);
        }

        public void SaveDailyRewardDay(string userId, int day)
        {
            _db.Child(PLAYERS_PATH).Child(userId).Child("dailyRewardDay").SetValueAsync(day);
        }

        // ---- Team Data ----
        public IEnumerator LoadTeamData(string teamId, Action<Player.TeamData> callback)
        {
            bool done = false;
            Player.TeamData result = null;
            if (string.IsNullOrEmpty(teamId))
            {
                callback?.Invoke(null);
                yield break;
            }
            _db.Child(TEAMS_PATH).Child(teamId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string json = task.Result.GetRawJsonValue();
                    result = JsonConvert.DeserializeObject<Player.TeamData>(json);
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator SaveTeamData(Player.TeamData team, Action<bool> callback = null)
        {
            bool done = false;
            bool success = false;
            string json = JsonConvert.SerializeObject(team);
            _db.Child(TEAMS_PATH).Child(team.TeamId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                success = task.IsCompleted && !task.IsFaulted;
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(success);
        }

        public IEnumerator UpdateTeamRoster(string teamId, List<string> playerIds, Action<bool> callback = null)
        {
            bool done = false;
            bool success = false;
            string json = JsonConvert.SerializeObject(playerIds);
            _db.Child(TEAMS_PATH).Child(teamId).Child("playerIds").SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                success = task.IsCompleted && !task.IsFaulted;
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(success);
        }

        // ---- Rankings ----
        public IEnumerator LoadWorldRankings(int limit, Action<List<Ranking.RankEntry>> callback)
        {
            bool done = false;
            List<Ranking.RankEntry> result = new List<Ranking.RankEntry>();
            _db.Child(RANKINGS_PATH).Child("world").OrderByChild("points").LimitToLast(limit)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        foreach (var child in task.Result.Children)
                        {
                            var entry = JsonConvert.DeserializeObject<Ranking.RankEntry>(child.GetRawJsonValue());
                            result.Add(entry);
                        }
                        result.Reverse();
                    }
                    done = true;
                });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator LoadCountryRankings(string country, int limit, Action<List<Ranking.RankEntry>> callback)
        {
            bool done = false;
            List<Ranking.RankEntry> result = new List<Ranking.RankEntry>();
            _db.Child(RANKINGS_PATH).Child("country").Child(country).OrderByChild("points").LimitToLast(limit)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        foreach (var child in task.Result.Children)
                        {
                            var entry = JsonConvert.DeserializeObject<Ranking.RankEntry>(child.GetRawJsonValue());
                            result.Add(entry);
                        }
                        result.Reverse();
                    }
                    done = true;
                });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator LoadCityRankings(string city, int limit, Action<List<Ranking.RankEntry>> callback)
        {
            bool done = false;
            List<Ranking.RankEntry> result = new List<Ranking.RankEntry>();
            _db.Child(RANKINGS_PATH).Child("city").Child(city).OrderByChild("points").LimitToLast(limit)
                .GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        foreach (var child in task.Result.Children)
                        {
                            var entry = JsonConvert.DeserializeObject<Ranking.RankEntry>(child.GetRawJsonValue());
                            result.Add(entry);
                        }
                        result.Reverse();
                    }
                    done = true;
                });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public void UpdateRankingEntry(string userId, Ranking.RankEntry entry)
        {
            string json = JsonConvert.SerializeObject(entry);
            _db.Child(RANKINGS_PATH).Child("world").Child(userId).SetRawJsonValueAsync(json);
            if (!string.IsNullOrEmpty(entry.Country))
                _db.Child(RANKINGS_PATH).Child("country").Child(entry.Country).Child(userId).SetRawJsonValueAsync(json);
            if (!string.IsNullOrEmpty(entry.City))
                _db.Child(RANKINGS_PATH).Child("city").Child(entry.City).Child(userId).SetRawJsonValueAsync(json);
        }

        // ---- Market ----
        public IEnumerator LoadMarketListings(Action<List<Economy.MarketListing>> callback)
        {
            bool done = false;
            List<Economy.MarketListing> result = new List<Economy.MarketListing>();
            _db.Child(MARKET_PATH).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    foreach (var child in task.Result.Children)
                    {
                        var listing = JsonConvert.DeserializeObject<Economy.MarketListing>(child.GetRawJsonValue());
                        result.Add(listing);
                    }
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public void PostMarketListing(Economy.MarketListing listing)
        {
            string json = JsonConvert.SerializeObject(listing);
            _db.Child(MARKET_PATH).Child(listing.ListingId).SetRawJsonValueAsync(json);
        }

        public void RemoveMarketListing(string listingId)
        {
            _db.Child(MARKET_PATH).Child(listingId).RemoveValueAsync();
        }

        // ---- Transfers ----
        public void RecordTransfer(Economy.TransferRecord record)
        {
            string json = JsonConvert.SerializeObject(record);
            _db.Child(TRANSFERS_PATH).Child(record.TransferId).SetRawJsonValueAsync(json);
        }

        // ---- Match Results ----
        public void SaveMatchResult(Match.MatchResult result)
        {
            string json = JsonConvert.SerializeObject(result);
            _db.Child("matchHistory").Child(result.MatchId).SetRawJsonValueAsync(json);
        }

        // Overload for MatchState
        public void SaveMatchResult(Match.MatchState state)
        {
            var result = Match.MatchResult.FromState(state);
            SaveMatchResult(result);
        }

        // Fire-and-forget save for non-coroutine callers
        public void SavePlayerDataDirect(Player.PlayerData player, Action<bool> callback = null)
        {
            if (player == null) { callback?.Invoke(false); return; }
            string json = JsonConvert.SerializeObject(player);
            _db.Child(PLAYERS_PATH).Child(player.UserId).SetRawJsonValueAsync(json)
                .ContinueWithOnMainThread(task => callback?.Invoke(task.IsCompleted && !task.IsFaulted));
        }
    }
}
