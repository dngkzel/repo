using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Core;
using FootballGame.Economy;

namespace FootballGame.Player
{
    public class TransferSystem : MonoBehaviour
    {
        public static TransferSystem Instance { get; private set; }

        public static event Action<FootballPlayerData> OnPlayerPurchased;
        public static event Action<FootballPlayerData> OnPlayerSold;
        public static event Action<string> OnTransferFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void BuyPlayerFromMarket(MarketListing listing, Action<bool, string> callback)
        {
            var gm = GameManager.Instance;
            if (gm.CurrentPlayer == null) { callback?.Invoke(false, "Not logged in."); return; }

            if (gm.CurrentTokenBalance < listing.Price)
            {
                string msg = "Insufficient tokens.";
                callback?.Invoke(false, msg);
                OnTransferFailed?.Invoke(msg);
                return;
            }

            if (gm.CurrentTeam.PlayerIds.Count >= 25)
            {
                string msg = "Team is full (max 25 players).";
                callback?.Invoke(false, msg);
                OnTransferFailed?.Invoke(msg);
                return;
            }

            StartCoroutine(ProcessPurchase(listing, callback));
        }

        private IEnumerator ProcessPurchase(MarketListing listing, Action<bool, string> callback)
        {
            var gm = GameManager.Instance;

            // Deduct tokens
            bool spent = gm.SpendTokens(listing.Price);
            if (!spent) { callback?.Invoke(false, "Insufficient tokens."); yield break; }

            // Load player data
            FootballPlayerData player = null;
            bool loaded = false;
            Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference
                .Child("footballPlayers").Child(listing.PlayerId).GetValueAsync()
                .ContinueWith(t =>
                {
                    if (t.IsCompleted && t.Result.Exists)
                        player = Newtonsoft.Json.JsonConvert.DeserializeObject<FootballPlayerData>(t.Result.GetRawJsonValue());
                    loaded = true;
                });
            yield return new WaitUntil(() => loaded);

            if (player == null)
            {
                gm.AddTokens(listing.Price); // refund
                callback?.Invoke(false, "Player not found.");
                yield break;
            }

            // Update ownership
            string previousOwner = player.OwnerId;
            player.OwnerId = gm.CurrentUserId;
            player.IsForSale = false;
            player.Price = 0;

            // Save player
            bool savedPlayer = false;
            string pJson = Newtonsoft.Json.JsonConvert.SerializeObject(player);
            Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference
                .Child("footballPlayers").Child(player.PlayerId).SetRawJsonValueAsync(pJson)
                .ContinueWith(t => savedPlayer = true);
            yield return new WaitUntil(() => savedPlayer);

            // Add to team roster
            gm.CurrentTeam.PlayerIds.Add(player.PlayerId);
            yield return StartCoroutine(gm.DataManager.UpdateTeamRoster(gm.CurrentTeam.TeamId, gm.CurrentTeam.PlayerIds));

            // Remove from market
            gm.DataManager.RemoveMarketListing(listing.ListingId);

            // Pay seller if not system listing
            if (!string.IsNullOrEmpty(previousOwner) && previousOwner != "system")
            {
                int sellerCut = Mathf.RoundToInt(listing.Price * 0.9f);
                // Load and update seller token balance
                StartCoroutine(PaySeller(previousOwner, sellerCut));
            }

            // Record transfer
            var record = new TransferRecord
            {
                TransferId = System.Guid.NewGuid().ToString(),
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                FromUserId = previousOwner,
                ToUserId = gm.CurrentUserId,
                Price = listing.Price,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            gm.DataManager.RecordTransfer(record);

            callback?.Invoke(true, null);
            OnPlayerPurchased?.Invoke(player);
            Debug.Log($"[TransferSystem] Player {player.Name} purchased for {listing.Price} tokens.");
        }

        private IEnumerator PaySeller(string sellerId, int amount)
        {
            bool done = false;
            PlayerData sellerData = null;
            yield return StartCoroutine(GameManager.Instance.DataManager.LoadPlayerData(sellerId, p => sellerData = p));
            if (sellerData != null)
            {
                sellerData.TokenBalance += amount;
                GameManager.Instance.DataManager.SavePlayerTokenBalance(sellerId, sellerData.TokenBalance);
            }
        }

        public void SellPlayerToMarket(FootballPlayerData player, int price, Action<bool, string> callback)
        {
            var gm = GameManager.Instance;
            if (player.OwnerId != gm.CurrentUserId)
            {
                callback?.Invoke(false, "You don't own this player.");
                return;
            }

            // Validate price range based on tier
            var (minPrice, maxPrice) = GetTierPriceRange(player.Tier);
            if (price < minPrice || price > maxPrice)
            {
                callback?.Invoke(false, $"Price must be between {minPrice} and {maxPrice} tokens for this tier.");
                return;
            }

            var listing = new MarketListing
            {
                ListingId = System.Guid.NewGuid().ToString(),
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                PlayerPosition = player.Position,
                PlayerOverall = player.Overall,
                PlayerTier = player.Tier.ToString(),
                Price = price,
                SellerId = gm.CurrentUserId,
                SellerTeamName = gm.CurrentTeam?.TeamName ?? "Unknown",
                ListedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            player.IsForSale = true;
            player.Price = price;

            string pJson = Newtonsoft.Json.JsonConvert.SerializeObject(player);
            Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference
                .Child("footballPlayers").Child(player.PlayerId).SetRawJsonValueAsync(pJson);

            gm.DataManager.PostMarketListing(listing);

            // Remove from team
            gm.CurrentTeam.PlayerIds.Remove(player.PlayerId);
            StartCoroutine(gm.DataManager.UpdateTeamRoster(gm.CurrentTeam.TeamId, gm.CurrentTeam.PlayerIds));

            callback?.Invoke(true, null);
            OnPlayerSold?.Invoke(player);
        }

        public (int min, int max) GetTierPriceRange(FootballPlayerData.PlayerTier tier)
        {
            switch (tier)
            {
                case FootballPlayerData.PlayerTier.Bronze: return (50, 200);
                case FootballPlayerData.PlayerTier.Silver: return (200, 500);
                case FootballPlayerData.PlayerTier.Gold: return (500, 1000);
                case FootballPlayerData.PlayerTier.Platinum: return (1000, 2500);
                case FootballPlayerData.PlayerTier.Legend: return (2500, 5000);
                default: return (50, 200);
            }
        }
    }
}
