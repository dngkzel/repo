using System;
using UnityEngine;

namespace FootballGame.Player
{
    // Enums are defined in PlayerData.cs — no duplicates here

    [Serializable]
    public class FootballPlayer
    {
        public string Id;
        public string Name;
        public string Position;
        public string Nationality;
        public string NationalityFlag;
        public int Age;
        public int Overall;
        public int Price;
        public PlayerTier Tier;
        public string TeamId;

        // Stats
        public int Speed;
        public int Shooting;
        public int Passing;
        public int Defense;
        public int Physical;
        public int Dribbling;
        public int GoalKeeping;

        // Match runtime
        [NonSerialized] public bool IsOnField;
        [NonSerialized] public int YellowCards;
        [NonSerialized] public bool RedCard;
        [NonSerialized] public int MatchGoals;
        [NonSerialized] public int MatchAssists;
        [NonSerialized] public int MatchRating;
        [NonSerialized] public bool IsInjured;

        public FootballPlayer() { }

        public FootballPlayer(string name, string position, string nationality,
            int overall, int age, PlayerTier tier)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Position = position;
            Nationality = nationality;
            Overall = overall;
            Age = age;
            Tier = tier;
            Price = CalculatePrice(tier, overall);
            GenerateStats(overall);
            IsOnField = true;
        }

        private int CalculatePrice(PlayerTier tier, int overall)
        {
            return tier switch
            {
                PlayerTier.Bronze => UnityEngine.Random.Range(50, 200),
                PlayerTier.Silver => UnityEngine.Random.Range(200, 500),
                PlayerTier.Gold => UnityEngine.Random.Range(500, 1000),
                PlayerTier.Platinum => UnityEngine.Random.Range(1000, 2500),
                PlayerTier.Legend => UnityEngine.Random.Range(2500, 5000),
                _ => 100
            };
        }

        private void GenerateStats(int overall)
        {
            int variance = 10;
            bool isGK = Position == "GK";

            Speed = isGK ? overall - 20 : Clamp(overall + UnityEngine.Random.Range(-variance, variance));
            Shooting = isGK ? overall - 40 : Clamp(overall + UnityEngine.Random.Range(-variance, variance));
            Passing = Clamp(overall + UnityEngine.Random.Range(-variance, variance));
            Defense = (Position == "CB" || Position == "LB" || Position == "RB" || Position == "CDM")
                ? Clamp(overall + UnityEngine.Random.Range(0, variance))
                : Clamp(overall + UnityEngine.Random.Range(-variance * 2, 0));
            Physical = Clamp(overall + UnityEngine.Random.Range(-variance, variance));
            Dribbling = Clamp(overall + UnityEngine.Random.Range(-variance, variance));
            GoalKeeping = isGK ? Clamp(overall + UnityEngine.Random.Range(0, variance)) : UnityEngine.Random.Range(20, 40);
        }

        private int Clamp(int v) => Mathf.Clamp(v, 1, 99);

        public void ResetMatchStats()
        {
            YellowCards = 0;
            RedCard = false;
            MatchGoals = 0;
            MatchAssists = 0;
            MatchRating = 6;
            IsInjured = false;
            IsOnField = true;
        }
    }
}
