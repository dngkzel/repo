using System;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Player;
using FootballGame.Core;

namespace FootballGame.Match
{
    // Avoid conflict with MatchEngine's MatchState enum and MatchEvents' MatchEvent class
    public enum LiveEventType
    {
        KickOff, Goal, YellowCard, RedCard, SecondYellow,
        Foul, Corner, Penalty, PenaltyMissed, Substitution,
        Injury, Offside, FreeKick, HalfTime, FullTime, GoalDisallowed
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

        public static MatchPlayerData FromFootballPlayerData(FootballPlayerData src)
        {
            if (src == null) return null;
            return new MatchPlayerData
            {
                Id = src.PlayerId,
                Name = src.Name,
                Position = src.Position,
                Nationality = src.Nationality,
                Overall = src.Overall,
                Speed = src.Speed,
                Shooting = src.Shooting,
                Passing = src.Passing,
                Defense = src.Defending,
                Physical = src.Physical,
                IsOnField = true,
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
                int sum = 0;
                foreach (var p in OnFieldPlayers) sum += p.Overall;
                return sum / OnFieldPlayers.Count;
            }
        }
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

    public class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }

        public LiveMatchState State { get; private set; }

        public event Action<LiveMatchEvent> OnGoalScored;
        public event Action<LiveMatchEvent> OnMatchEventFired;
        public event Action<int> OnMinuteUpdated;
        public event Action OnHalftime;
        public event Action OnFullTime;

        private int _homeSubsUsed;
        private int _awaySubsUsed;
        private const int MAX_SUBSTITUTIONS = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void InitMatch(MatchTeamData home, MatchTeamData away)
        {
            home.IsHomeTeam = true;
            away.IsHomeTeam = false;

            State = new LiveMatchState
            {
                HomeTeam = home,
                AwayTeam = away,
            };

            _homeSubsUsed = 0;
            _awaySubsUsed = 0;

            MatchSimulator.Instance?.StartSimulation(this);
        }

        public void TriggerGoal(LiveMatchEvent evt)
        {
            evt.Commentary = GenerateGoalCommentary(evt);
            State.Events.Add(evt);
            OnGoalScored?.Invoke(evt);
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerEvent(LiveMatchEvent evt)
        {
            evt.Commentary = GenerateCommentary(evt);
            State.Events.Add(evt);
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerHalftime()
        {
            State.IsPaused = true;
            var evt = new LiveMatchEvent { Type = LiveEventType.HalfTime, Minute = 45,
                Commentary = $"Half Time! {State.HomeTeam?.TeamName} {State.HomeScore} - {State.AwayScore} {State.AwayTeam?.TeamName}" };
            State.Events.Add(evt);
            OnHalftime?.Invoke();
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerFullTime()
        {
            State.IsFinished = true;
            var evt = new LiveMatchEvent { Type = LiveEventType.FullTime, Minute = State.CurrentMinute,
                Commentary = $"Full Time! {State.HomeTeam?.TeamName} {State.HomeScore} - {State.AwayScore} {State.AwayTeam?.TeamName}" };
            State.Events.Add(evt);
            OnFullTime?.Invoke();
            OnMatchEventFired?.Invoke(evt);
            SaveResult();
        }

        public void UpdateMinute(int minute) => OnMinuteUpdated?.Invoke(minute);

        public bool MakeSubstitution(MatchTeamData team, MatchPlayerData playerOut, MatchPlayerData playerIn)
        {
            bool isHome = team == State.HomeTeam;
            if ((isHome ? _homeSubsUsed : _awaySubsUsed) >= MAX_SUBSTITUTIONS) return false;
            if (!team.OnFieldPlayers.Contains(playerOut) || !team.Bench.Contains(playerIn)) return false;

            team.OnFieldPlayers.Remove(playerOut);
            team.Bench.Remove(playerIn);
            team.OnFieldPlayers.Add(playerIn);
            team.Bench.Add(playerOut);
            playerOut.IsOnField = false;
            playerIn.IsOnField = true;

            if (isHome) _homeSubsUsed++;
            else _awaySubsUsed++;

            TriggerEvent(new LiveMatchEvent
            {
                Type = LiveEventType.Substitution,
                Minute = State.CurrentMinute,
                Team = team,
                Player = playerOut,
                SubstituteIn = playerIn,
            });
            return true;
        }

        public void PauseMatch() => State.IsPaused = true;
        public void ResumeMatch() => State.IsPaused = false;
        public int GetSubsRemaining(bool home) => MAX_SUBSTITUTIONS - (home ? _homeSubsUsed : _awaySubsUsed);

        private string GenerateGoalCommentary(LiveMatchEvent evt)
        {
            string[] t =
            {
                $"GOAL! {evt.Player?.Name} scores in the {evt.Minute}th minute!",
                $"GOOOAL! {evt.Player?.Name} slots it home! {State.HomeScore}-{State.AwayScore}",
                $"{evt.Minute}' — {evt.Player?.Name} finds the net!",
            };
            string s = t[UnityEngine.Random.Range(0, t.Length)];
            if (evt.AssistPlayer != null) s += $" (Assist: {evt.AssistPlayer.Name})";
            return s;
        }

        private string GenerateCommentary(LiveMatchEvent evt)
        {
            return evt.Type switch
            {
                LiveEventType.YellowCard   => $"{evt.Minute}' Yellow card — {evt.Player?.Name}",
                LiveEventType.RedCard      => $"{evt.Minute}' RED CARD! {evt.Player?.Name} sent off!",
                LiveEventType.SecondYellow => $"{evt.Minute}' Second yellow = red! {evt.Player?.Name} walks!",
                LiveEventType.Penalty      => $"{evt.Minute}' PENALTY for {evt.Team?.TeamName}!",
                LiveEventType.PenaltyMissed=> $"{evt.Minute}' Penalty missed by {evt.Player?.Name}!",
                LiveEventType.Corner       => $"{evt.Minute}' Corner for {evt.Team?.TeamName}.",
                LiveEventType.Substitution => $"{evt.Minute}' Sub: {evt.SubstituteIn?.Name} on, {evt.Player?.Name} off.",
                LiveEventType.Foul         => $"{evt.Minute}' Foul by {evt.Player?.Name}.",
                _ => $"{evt.Minute}' {evt.Type}"
            };
        }

        private void SaveResult()
        {
            var dm = GameManager.Instance?.DataManager;
            if (dm == null) return;

            var result = new MatchResult
            {
                HomeTeamId   = State.HomeTeam?.OwnerId ?? "",
                HomeTeamName = State.HomeTeam?.TeamName ?? "",
                AwayTeamId   = State.AwayTeam?.OwnerId ?? "",
                AwayTeamName = State.AwayTeam?.TeamName ?? "",
                HomeScore    = State.HomeScore,
                AwayScore    = State.AwayScore,
                PlayedAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            foreach (var e in State.Events)
                if (e?.Commentary != null) result.EventSummary.Add(e.Commentary);

            dm.SaveMatchResult(result);

            // Update rankings
            string uid = GameManager.Instance?.CurrentUserId ?? "";
            var player = GameManager.Instance?.CurrentUserData;
            bool isHome = State.HomeTeam?.OwnerId == uid;
            bool win  = isHome ? State.HomeScore > State.AwayScore : State.AwayScore > State.HomeScore;
            bool draw = State.HomeScore == State.AwayScore;
            int gf = isHome ? State.HomeScore : State.AwayScore;
            int ga = isHome ? State.AwayScore : State.HomeScore;

            Ranking.RankingSystem.Instance?.UpdatePlayerRanking(
                uid, player?.DisplayName ?? "", player?.TeamId ?? "",
                player?.Country ?? "", player?.City ?? "", win, draw, gf, ga);
        }
    }
}
