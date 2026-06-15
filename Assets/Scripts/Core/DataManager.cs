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

        private void Awake() => _db = FirebaseDatabase.DefaultInstance.RootReference;

        public IEnumerator LoadPlayerData(string userId, Action<Player.PlayerData> callback)
        {
            bool done = false;
            Player.PlayerData result = null;
            _db.Child(PLAYERS_PATH).Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                    result = JsonConvert.DeserializeObject<Player.PlayerData>(task.Result.GetRawJsonValue());
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator SavePlayerData(Player.PlayerData player, Action<bool> callback = null)
        {
            bool done = false; bool success = false;
            _db.Child(PLAYERS_PATH).Child(player.UserId).SetRawJsonValueAsync(JsonConvert.SerializeObject(player))
                .ContinueWithOnMainThread(t => { success = t.IsCompleted && !t.IsFaulted; done = true; });
            yield return new WaitUntil(() => done);
            callback?.Invoke(success);
        }

        public void SavePlayerDataDirect(Player.PlayerData player, Action<bool> callback = null)
        {
            if (player == null) { callback?.Invoke(false); return; }
            _db.Child(PLAYERS_PATH).Child(player.UserId).SetRawJsonValueAsync(JsonConvert.SerializeObject(player))
                .ContinueWithOnMainThread(t => callback?.Invoke(t.IsCompleted && !t.IsFaulted));
        }

        public void SavePlayerTokenBalance(string userId, int balance) =>
            _db.Child(PLAYERS_PATH).Child(userId).Child("tokenBalance").SetValueAsync(balance);

        public void SavePlayerPremiumStatus(string userId, bool isPremium, long expiryTimestamp)
        {
            _db.Child(PLAYERS_PATH).Child(userId).UpdateChildrenAsync(new Dictionary<string, object>
            {
                ["isPremium"] = isPremium,
                ["premiumExpiry"] = expiryTimestamp
            });
        }

        public void SaveLastLoginDate(string userId, string dateString) =>
            _db.Child(PLAYERS_PATH).Child(userId).Child("lastLoginDate").SetValueAsync(dateString);

        public void SaveDailyRewardDay(string userId, int day) =>
            _db.Child(PLAYERS_PATH).Child(userId).Child("dailyRewardDay").SetValueAsync(day);

        public IEnumerator LoadTeamData(string teamId, Action<Player.TeamData> callback)
        {
            if (string.IsNullOrEmpty(teamId)) { callback?.Invoke(null); yield break; }
            bool done = false; Player.TeamData result = null;
            _db.Child(TEAMS_PATH).Child(teamId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                    result = JsonConvert.DeserializeObject<Player.TeamData>(task.Result.GetRawJsonValue());
                done = true;
            });
            yield return new WaitUntil(() => done);
            callback?.Invoke(result);
        }

        public IEnumerator SaveTeamData(Player.TeamData team, Action<bool> callback = null)
        {
            bool done = false; bool success = false;
            _db.Child(TEAMS_PATH).Child(team.TeamId).SetRawJsonValueAsync(JsonConvert.SerializeObject(team))
                .ContinueWithOnMainThread(t => { success = t.IsCompleted && !t.IsFaulted; done = true; });
            yield return new WaitUntil(() => done);
            callback?.Invoke(success);
        }

        public void SaveTeamDataDirect(Player.TeamData team, Action<bool> callback = null)
        {
            if (team == null || string.IsNullOrEmpty(team.TeamId)) { callback?.Invoke(false); return; }
            _db.Child(TEAMS_PATH).Child(team.TeamId).SetRawJsonValueAsync(JsonConvert.SerializeObject(team))
                .ContinueWithOnMainThread(t => callback?.Invoke(t.IsCompleted && !t.IsFaulted));
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

        public void PostMarketListing(Economy.MarketListing listing) =>
            _db.Child(MARKET_PATH).Child(listing.ListingId).SetRawJsonValueAsync(JsonConvert.SerializeObject(listing));

        public void RemoveMarketListing(string listingId) =>
            _db.Child(MARKET_PATH).Child(listingId).RemoveValueAsync();

        public void RecordTransfer(Economy.TransferRecord record) =>
            _db.Child(TRANSFERS_PATH).Child(record.TransferId).SetRawJsonValueAsync(JsonConvert.SerializeObject(record));

        public void SaveMatchResult(Match.MatchResult result) =>
            _db.Child("matchHistory").Child(result.MatchId).SetRawJsonValueAsync(JsonConvert.SerializeObject(result));

        public void SaveMatchResult(Match.LiveMatchState state) => SaveMatchResult(Match.MatchResult.FromLiveState(state));
    }
}
