using System;
using System.Collections.Generic;

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

        public static MatchResult FromLiveState(LiveMatchState state)
        {
            var r = new MatchResult
            {
                HomeTeamId   = state.HomeTeam?.OwnerId ?? "",
                HomeTeamName = state.HomeTeam?.TeamName ?? "",
                AwayTeamId   = state.AwayTeam?.OwnerId ?? "",
                AwayTeamName = state.AwayTeam?.TeamName ?? "",
                HomeScore    = state.HomeScore,
                AwayScore    = state.AwayScore,
                PlayedAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            foreach (var e in state.Events)
                if (!string.IsNullOrEmpty(e?.Commentary)) r.EventSummary.Add(e.Commentary);
            return r;
        }
    }
}
