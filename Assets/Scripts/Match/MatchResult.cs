using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Match
{
    [Serializable]
    public class MatchResult
    {
        public string MatchId;
        public string HomeTeamId;
        public string HomeTeamName;
        public string AwayTeamId;
        public string AwayTeamName;
        public int HomeScore;
        public int AwayScore;
        public long PlayedAt;
        public List<string> EventSummary = new List<string>();

        public MatchResult() { MatchId = Guid.NewGuid().ToString(); }

        public static MatchResult FromState(MatchState state)
        {
            var result = new MatchResult
            {
                HomeTeamId   = state.HomeTeam?.OwnerId ?? "",
                HomeTeamName = state.HomeTeam?.TeamName ?? "",
                AwayTeamId   = state.AwayTeam?.OwnerId ?? "",
                AwayTeamName = state.AwayTeam?.TeamName ?? "",
                HomeScore    = state.HomeScore,
                AwayScore    = state.AwayScore,
                PlayedAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            foreach (var evt in state.Events)
                if (evt != null) result.EventSummary.Add(evt.Commentary ?? evt.Type.ToString());
            return result;
        }
    }
}
