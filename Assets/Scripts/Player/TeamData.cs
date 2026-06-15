using System;
using System.Collections.Generic;

namespace FootballGame.Player
{
    [Serializable]
    public class TeamData
    {
        public string TeamId;
        public string TeamName;
        public string OwnerId;
        public string Country;
        public string City;
        public string Formation;
        public List<string> PlayerIds;
        public List<string> StartingXI;
        public List<string> Substitutes;

        // Stats
        public int TotalMatches;
        public int Wins;
        public int Draws;
        public int Losses;
        public int GoalsScored;
        public int GoalsConceded;
        public int Points;

        public long CreatedAt;
        public string BadgeUrl;
        public string KitPrimaryColor;
        public string KitSecondaryColor;

        public TeamData()
        {
            PlayerIds = new List<string>();
            StartingXI = new List<string>();
            Substitutes = new List<string>();
            KitPrimaryColor = "#FF0000";
            KitSecondaryColor = "#FFFFFF";
        }

        public int GoalDifference => GoalsScored - GoalsConceded;

        public float WinRate => TotalMatches > 0 ? (float)Wins / TotalMatches : 0f;

        public void RecordResult(int goalsFor, int goalsAgainst)
        {
            TotalMatches++;
            GoalsScored += goalsFor;
            GoalsConceded += goalsAgainst;

            if (goalsFor > goalsAgainst) { Wins++; Points += 3; }
            else if (goalsFor == goalsAgainst) { Draws++; Points += 1; }
            else { Losses++; }
        }
    }
}
