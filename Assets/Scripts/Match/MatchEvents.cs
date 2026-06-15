using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballGame.Match
{
    public enum LiveEventType
    {
        KickOff, Goal, YellowCard, RedCard, SecondYellow,
        Foul, Corner, Penalty, PenaltyMissed, Substitution,
        Injury, Offside, FreeKick, HalfTime, FullTime, GoalDisallowed
    }

    [Serializable]
    public class MatchPlayerData
    {
        public string Id;
        public string Name;
        public string Position;
        public string Nationality;
        public int Overall;
        public int Speed;
        public int Shooting;
        public int Passing;
        public int Defense;
        public int Physical;
        public int YellowCards;
        public bool RedCard;
        public int MatchGoals;
        public int MatchAssists;
        public bool IsOnField = true;

        public static MatchPlayerData From(Player.FootballPlayerData src)
        {
            if (src == null) return null;
            return new MatchPlayerData
            {
                Id = src.PlayerId, Name = src.Name, Position = src.Position,
                Nationality = src.Nationality, Overall = src.Overall,
                Speed = src.Speed, Shooting = src.Shooting, Passing = src.Passing,
                Defense = src.Defending, Physical = src.Physical, IsOnField = true,
            };
        }
    }

    [Serializable]
    public class MatchTeamData
    {
        public string OwnerId;
        public string TeamName;
        public string Formation;
        public bool IsHomeTeam;
        public List<MatchPlayerData> OnFieldPlayers = new List<MatchPlayerData>();
        public List<MatchPlayerData> Bench = new List<MatchPlayerData>();

        public int TotalOverall
        {
            get
            {
                if (OnFieldPlayers == null || OnFieldPlayers.Count == 0) return 50;
                int s = 0;
                foreach (var p in OnFieldPlayers) s += p.Overall;
                return s / OnFieldPlayers.Count;
            }
        }
    }

    [Serializable]
    public class LiveMatchEvent
    {
        public LiveEventType Type;
        public int Minute;
        public MatchTeamData Team;
        public MatchPlayerData Player;
        public MatchPlayerData AssistPlayer;
        public MatchPlayerData SubstituteIn;
        public string Commentary;
    }

    [Serializable]
    public class LiveMatchState
    {
        public MatchTeamData HomeTeam;
        public MatchTeamData AwayTeam;
        public int HomeScore;
        public int AwayScore;
        public int CurrentMinute;
        public int InjuryTime;
        public bool HalftimeDone;
        public bool IsFinished;
        public bool IsPaused;
        public string HomeFormation = "4-4-2";
        public string AwayFormation = "4-3-3";
        public List<LiveMatchEvent> Events = new List<LiveMatchEvent>();
    }

    public static class CommentaryGenerator
    {
        private static readonly string[] GoalTemplates =
        {
            "GOAL! {0} scores in the {1}th minute!", "GOOOAL! {0} slots it home!",
            "{1}' — {0} finds the net! The crowd goes wild!", "What a finish! {0} makes it!",
        };

        private static readonly string[] YellowCardTemplates =
        {
            "{1}' — Yellow card shown to {0}.", "{0} picks up a caution. {1}'",
        };

        private static readonly string[] RedCardTemplates =
        {
            "RED CARD! {0} is sent off! {1}'", "{1}' — Off goes {0}! Down to ten men!",
        };

        public static string Goal(string playerName, int minute, string assistName = null)
        {
            string t = GoalTemplates[UnityEngine.Random.Range(0, GoalTemplates.Length)];
            string s = string.Format(t, playerName, minute);
            if (!string.IsNullOrEmpty(assistName)) s += $" (Assist: {assistName})";
            return s;
        }

        public static string YellowCard(string playerName, int minute) =>
            string.Format(YellowCardTemplates[UnityEngine.Random.Range(0, YellowCardTemplates.Length)], playerName, minute);

        public static string RedCard(string playerName, int minute) =>
            string.Format(RedCardTemplates[UnityEngine.Random.Range(0, RedCardTemplates.Length)], playerName, minute);

        public static string Substitution(string inName, string outName, int minute) =>
            $"{minute}' — Sub: {inName} on, {outName} off.";
    }
}
