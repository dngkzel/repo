using System;

namespace FootballGame.Ranking
{
    [Serializable]
    public class RankEntry
    {
        public string UserId;
        public string DisplayName;
        public string TeamName;
        public string Country;
        public string City;
        public int Points;
        public int Wins;
        public int Draws;
        public int Losses;
        public int GoalsFor;
        public int GoalsAgainst;
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public long LastUpdated;
        public int Rank;
    }
}
