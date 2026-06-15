using System;
using UnityEngine;

namespace FootballGame.Economy
{
    [Serializable]
    public class MarketListing
    {
        public string ListingId;
        public string SellerId;
        public string PlayerId;
        public string PlayerName;
        public string Position;
        public int Overall;
        public int Price;
        public string Tier;
        public long ExpiresAt;
        public bool IsActive;

        public MarketListing()
        {
            ListingId = Guid.NewGuid().ToString();
            IsActive = true;
        }
    }

    [Serializable]
    public class TransferRecord
    {
        public string TransferId;
        public string BuyerId;
        public string SellerId;
        public string PlayerId;
        public string PlayerName;
        public int Price;
        public long Timestamp;

        public TransferRecord()
        {
            TransferId = Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
