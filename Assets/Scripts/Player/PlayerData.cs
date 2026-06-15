using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Player
{
    [Serializable]
    public class PlayerData
    {
        public string UserId;
        public string DisplayName;
        public string Email;
        public string TeamId;
        public int TokenBalance;
        public bool IsPremium;
        public long PremiumExpiry;
        public string Country;
        public string City;
        public int DailyRewardDay;
        public string LastLoginDate;
        public int TotalPoints;
        public int TotalWins;
        public int TotalDraws;
        public int TotalLosses;
        public long CreatedAt;
        public string PreferredLanguage;
        public bool SoundEnabled;
        public bool MusicEnabled;
        public float SoundVolume;
        public float MusicVolume;
        public string AvatarUrl;

        public PlayerData()
        {
            SoundEnabled = true;
            MusicEnabled = true;
            SoundVolume = 1.0f;
            MusicVolume = 0.7f;
        }
    }

    // Enums used across the game
    public enum PlayerTier { Bronze, Silver, Gold, Platinum, Legend }
    public enum PlayerPosition { GK, CB, LB, RB, CDM, CM, CAM, LM, RM, LW, RW, ST, CF }

    [Serializable]
    public class FootballPlayerData
    {
        public enum PlayerTier { Bronze, Silver, Gold, Platinum, Legend }

        public string PlayerId;
        public string Name;
        public string Position;
        public int Overall;
        public int Speed;
        public int Shooting;
        public int Passing;
        public int Dribbling;
        public int Defending;
        public int Physical;
        public int GoalKeeping;
        public PlayerTier Tier;
        public int Age;
        public string Nationality;
        public string OwnerId;
        public bool IsForSale;
        public int Price;
        public string PhotoUrl;

        // Match stats
        public int CareerGoals;
        public int CareerAssists;
        public int CareerMatches;
        public int CareerYellowCards;
        public int CareerRedCards;

        public int GetEffectiveOverall()
        {
            return Mathf.RoundToInt((Speed + Shooting + Passing + Dribbling + Defending + Physical) / 6f);
        }

        public float GetMatchRating(string vsPosition = null)
        {
            float base_rating = Overall / 10f;
            if (Position == "GK") base_rating += GoalKeeping * 0.03f;
            else if (Position.Contains("B")) base_rating += Defending * 0.02f;
            else if (Position.Contains("M")) base_rating += Passing * 0.02f;
            else base_rating += Shooting * 0.02f;
            return Mathf.Clamp(base_rating, 1f, 10f);
        }

        // Market system compatibility
        public PlayerTier tier;
        public string name => Name;
        public PlayerPosition position => System.Enum.TryParse<PlayerPosition>(Position, out var p) ? p : PlayerPosition.CM;

        public PlayerTier GetTier() => tier;

        public (int min, int max) GetTokenRange()
        {
            return tier switch
            {
                PlayerTier.Bronze   => (50, 200),
                PlayerTier.Silver   => (200, 500),
                PlayerTier.Gold     => (500, 1000),
                PlayerTier.Platinum => (1000, 2500),
                PlayerTier.Legend   => (2500, 5000),
                _ => (50, 200)
            };
        }

        public static FootballPlayerData GenerateRandomPlayer(PlayerTier tier)
        {
            string[] firstNames = { "Carlos", "Marco", "Luis", "Ahmed", "Kevin", "Pierre", "Gabriel", "James", "Yusuf", "Takeshi" };
            string[] lastNames  = { "Silva", "Santos", "Müller", "García", "Rossi", "Dupont", "Nakamura", "Park", "Kamara", "Costa" };
            string[] positions  = { "GK","CB","LB","RB","CDM","CM","CAM","LM","RM","LW","RW","ST","CF" };
            string[] nations    = { "Brazil","Germany","Spain","France","England","Italy","Portugal","Argentina","Netherlands","Belgium" };

            (int min, int max) overallRange = tier switch
            {
                PlayerTier.Bronze   => (40, 60),
                PlayerTier.Silver   => (60, 75),
                PlayerTier.Gold     => (75, 85),
                PlayerTier.Platinum => (85, 92),
                PlayerTier.Legend   => (92, 99),
                _ => (40, 60)
            };

            string fn = firstNames[UnityEngine.Random.Range(0, firstNames.Length)];
            string ln = lastNames[UnityEngine.Random.Range(0, lastNames.Length)];
            int overall = UnityEngine.Random.Range(overallRange.min, overallRange.max + 1);

            var p = new FootballPlayerData
            {
                PlayerId    = System.Guid.NewGuid().ToString(),
                Name        = $"{fn} {ln}",
                Position    = positions[UnityEngine.Random.Range(0, positions.Length)],
                Nationality = nations[UnityEngine.Random.Range(0, nations.Length)],
                Age         = UnityEngine.Random.Range(17, 35),
                Overall     = overall,
                tier        = tier,
            };
            p.Speed    = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.Shooting = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.Passing  = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.Dribbling = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.Defending = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.Physical  = Mathf.Clamp(overall + UnityEngine.Random.Range(-10, 11), 1, 99);
            p.GoalKeeping = p.Position == "GK" ? overall : UnityEngine.Random.Range(20, 40);
            return p;
        }

        public Dictionary<string, object> ToFirebaseDictionary()
        {
            return new Dictionary<string, object>
            {
                ["playerId"]    = PlayerId,
                ["name"]        = Name,
                ["position"]    = Position,
                ["overall"]     = Overall,
                ["nationality"] = Nationality,
                ["age"]         = Age,
                ["tier"]        = tier.ToString(),
                ["speed"]       = Speed,
                ["shooting"]    = Shooting,
                ["passing"]     = Passing,
                ["defending"]   = Defending,
                ["physical"]    = Physical,
                ["dribbling"]   = Dribbling,
                ["goalKeeping"] = GoalKeeping,
            };
        }

        public static FootballPlayerData FromFirebaseDictionary(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var p = new FootballPlayerData();
            if (d.TryGetValue("playerId",    out var id))   p.PlayerId    = id?.ToString();
            if (d.TryGetValue("name",        out var nm))   p.Name        = nm?.ToString();
            if (d.TryGetValue("position",    out var pos))  p.Position    = pos?.ToString();
            if (d.TryGetValue("overall",     out var ov))   p.Overall     = Convert.ToInt32(ov);
            if (d.TryGetValue("nationality", out var nat))  p.Nationality = nat?.ToString();
            if (d.TryGetValue("age",         out var age))  p.Age         = Convert.ToInt32(age);
            if (d.TryGetValue("tier",        out var tier)) System.Enum.TryParse<PlayerTier>(tier?.ToString(), out p.tier);
            if (d.TryGetValue("speed",       out var sp))   p.Speed       = Convert.ToInt32(sp);
            if (d.TryGetValue("shooting",    out var sh))   p.Shooting    = Convert.ToInt32(sh);
            if (d.TryGetValue("passing",     out var pa))   p.Passing     = Convert.ToInt32(pa);
            if (d.TryGetValue("defending",   out var def))  p.Defending   = Convert.ToInt32(def);
            if (d.TryGetValue("physical",    out var phy))  p.Physical    = Convert.ToInt32(phy);
            if (d.TryGetValue("dribbling",   out var dr))   p.Dribbling   = Convert.ToInt32(dr);
            if (d.TryGetValue("goalKeeping", out var gk))   p.GoalKeeping = Convert.ToInt32(gk);
            return p;
        }
    }
}
