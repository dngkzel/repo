using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Player
{
    public enum PlayerTier { Bronze, Silver, Gold, Platinum, Legend }
    public enum PlayerPosition { GK, CB, LB, RB, CDM, CM, CAM, LM, RM, LW, RW, ST, CF }

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
            SoundVolume = 1f;
            MusicVolume = 0.7f;
        }
    }

    [Serializable]
    public class FootballPlayerData
    {
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
        public PlayerTier tier;
        public int Age;
        public string Nationality;
        public string OwnerId;
        public bool IsForSale;
        public int Price;

        // Career stats
        public int CareerGoals;
        public int CareerAssists;
        public int CareerMatches;
        public int CareerYellowCards;
        public int CareerRedCards;

        // Lower-case accessors for MarketSystem
        public string name => Name;
        public PlayerPosition position
        {
            get
            {
                if (Enum.TryParse<PlayerPosition>(Position, out var p)) return p;
                return PlayerPosition.CM;
            }
        }

        public PlayerTier GetTier() => tier;

        public (int min, int max) GetTokenRange() => tier switch
        {
            PlayerTier.Bronze   => (50, 200),
            PlayerTier.Silver   => (200, 500),
            PlayerTier.Gold     => (500, 1000),
            PlayerTier.Platinum => (1000, 2500),
            PlayerTier.Legend   => (2500, 5000),
            _                   => (50, 200)
        };

        public int GetEffectiveOverall() =>
            Mathf.RoundToInt((Speed + Shooting + Passing + Dribbling + Defending + Physical) / 6f);

        public static FootballPlayerData GenerateRandomPlayer(PlayerTier t)
        {
            string[] fn = { "Carlos","Marco","Luis","Ahmed","Kevin","Pierre","Gabriel","James","Yusuf","Takeshi","Luca","Sergio" };
            string[] ln = { "Silva","Santos","Müller","García","Rossi","Dupont","Nakamura","Park","Kamara","Costa","Neves","Perez" };
            string[] pos = { "GK","CB","LB","RB","CDM","CM","CAM","LM","RM","LW","RW","ST","CF" };
            string[] nat = { "Brazil","Germany","Spain","France","England","Italy","Portugal","Argentina","Netherlands","Belgium","Turkey","Japan" };

            (int min, int max) ovr = t switch
            {
                PlayerTier.Bronze   => (40, 60),
                PlayerTier.Silver   => (60, 75),
                PlayerTier.Gold     => (75, 85),
                PlayerTier.Platinum => (85, 92),
                PlayerTier.Legend   => (92, 99),
                _                   => (40, 60)
            };

            int overall = UnityEngine.Random.Range(ovr.min, ovr.max + 1);
            int v = 10;
            var p = new FootballPlayerData
            {
                PlayerId    = Guid.NewGuid().ToString(),
                Name        = $"{fn[UnityEngine.Random.Range(0, fn.Length)]} {ln[UnityEngine.Random.Range(0, ln.Length)]}",
                Position    = pos[UnityEngine.Random.Range(0, pos.Length)],
                Nationality = nat[UnityEngine.Random.Range(0, nat.Length)],
                Age         = UnityEngine.Random.Range(17, 36),
                Overall     = overall,
                tier        = t,
            };
            p.Speed     = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.Shooting  = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.Passing   = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.Dribbling = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.Defending = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.Physical  = Clamp(overall + UnityEngine.Random.Range(-v, v + 1));
            p.GoalKeeping = p.Position == "GK" ? Clamp(overall + UnityEngine.Random.Range(0, v)) : UnityEngine.Random.Range(20, 40);
            return p;
        }

        private static int Clamp(int v) => Mathf.Clamp(v, 1, 99);

        public Dictionary<string, object> ToFirebaseDictionary() => new Dictionary<string, object>
        {
            ["playerId"] = PlayerId, ["name"] = Name, ["position"] = Position,
            ["overall"] = Overall, ["nationality"] = Nationality, ["age"] = Age,
            ["tier"] = tier.ToString(), ["speed"] = Speed, ["shooting"] = Shooting,
            ["passing"] = Passing, ["defending"] = Defending, ["physical"] = Physical,
            ["dribbling"] = Dribbling, ["goalKeeping"] = GoalKeeping,
        };

        public static FootballPlayerData FromFirebaseDictionary(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var p = new FootballPlayerData();
            if (d.TryGetValue("playerId",    out var id))  p.PlayerId    = id?.ToString();
            if (d.TryGetValue("name",        out var nm))  p.Name        = nm?.ToString();
            if (d.TryGetValue("position",    out var ps))  p.Position    = ps?.ToString();
            if (d.TryGetValue("overall",     out var ov))  p.Overall     = Convert.ToInt32(ov);
            if (d.TryGetValue("nationality", out var na))  p.Nationality = na?.ToString();
            if (d.TryGetValue("age",         out var ag))  p.Age         = Convert.ToInt32(ag);
            if (d.TryGetValue("tier",        out var tr))  Enum.TryParse<PlayerTier>(tr?.ToString(), out p.tier);
            if (d.TryGetValue("speed",       out var sp))  p.Speed       = Convert.ToInt32(sp);
            if (d.TryGetValue("shooting",    out var sh))  p.Shooting    = Convert.ToInt32(sh);
            if (d.TryGetValue("passing",     out var pa))  p.Passing     = Convert.ToInt32(pa);
            if (d.TryGetValue("defending",   out var df))  p.Defending   = Convert.ToInt32(df);
            if (d.TryGetValue("physical",    out var ph))  p.Physical    = Convert.ToInt32(ph);
            if (d.TryGetValue("dribbling",   out var dr))  p.Dribbling   = Convert.ToInt32(dr);
            if (d.TryGetValue("goalKeeping", out var gk))  p.GoalKeeping = Convert.ToInt32(gk);
            return p;
        }
    }
}
