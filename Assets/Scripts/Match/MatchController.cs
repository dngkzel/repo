using System;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Core;

namespace FootballGame.Match
{
    public class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }

        public LiveMatchState State { get; private set; }

        public event Action<LiveMatchEvent> OnGoalScored;
        public event Action<LiveMatchEvent> OnMatchEventFired;
        public event Action<int> OnMinuteUpdated;
        public event Action OnHalftime;
        public event Action OnFullTime;
        public event Action OnMatchStarted;

        private int _homeSubs, _awaySubs;
        private const int MAX_SUBS = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void InitMatch(MatchTeamData home, MatchTeamData away)
        {
            home.IsHomeTeam = true;
            away.IsHomeTeam = false;
            State = new LiveMatchState { HomeTeam = home, AwayTeam = away };
            _homeSubs = _awaySubs = 0;
            OnMatchStarted?.Invoke();
            MatchSimulator.Instance?.StartSimulation(this);
        }

        public void TriggerGoal(LiveMatchEvent evt)
        {
            if (string.IsNullOrEmpty(evt.Commentary))
                evt.Commentary = CommentaryGenerator.Goal(
                    evt.Player?.Name, evt.Minute, evt.AssistPlayer?.Name);
            State.Events.Add(evt);
            OnGoalScored?.Invoke(evt);
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerEvent(LiveMatchEvent evt)
        {
            if (string.IsNullOrEmpty(evt.Commentary))
                evt.Commentary = MakeCommentary(evt);
            State.Events.Add(evt);
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerHalftime()
        {
            State.IsPaused = true;
            var evt = new LiveMatchEvent
            {
                Type = LiveEventType.HalfTime, Minute = 45,
                Commentary = $"Half Time! {State.HomeTeam?.TeamName} {State.HomeScore} - {State.AwayScore} {State.AwayTeam?.TeamName}"
            };
            State.Events.Add(evt);
            OnHalftime?.Invoke();
            OnMatchEventFired?.Invoke(evt);
        }

        public void TriggerFullTime()
        {
            State.IsFinished = true;
            var evt = new LiveMatchEvent
            {
                Type = LiveEventType.FullTime, Minute = State.CurrentMinute,
                Commentary = $"Full Time! {State.HomeTeam?.TeamName} {State.HomeScore} - {State.AwayScore} {State.AwayTeam?.TeamName}"
            };
            State.Events.Add(evt);
            OnFullTime?.Invoke();
            OnMatchEventFired?.Invoke(evt);
            SaveResult();
        }

        public void UpdateMinute(int minute) => OnMinuteUpdated?.Invoke(minute);

        public bool MakeSubstitution(MatchTeamData team, MatchPlayerData playerOut, MatchPlayerData playerIn)
        {
            bool isHome = team == State.HomeTeam;
            if ((isHome ? _homeSubs : _awaySubs) >= MAX_SUBS) return false;
            if (!team.OnFieldPlayers.Contains(playerOut) || !team.Bench.Contains(playerIn)) return false;

            team.OnFieldPlayers.Remove(playerOut);
            team.Bench.Remove(playerIn);
            team.OnFieldPlayers.Add(playerIn);
            team.Bench.Add(playerOut);
            playerOut.IsOnField = false;
            playerIn.IsOnField = true;
            if (isHome) _homeSubs++; else _awaySubs++;

            TriggerEvent(new LiveMatchEvent
            {
                Type = LiveEventType.Substitution, Minute = State.CurrentMinute,
                Team = team, Player = playerOut, SubstituteIn = playerIn,
            });
            return true;
        }

        public void PauseMatch() => State.IsPaused = true;
        public void ResumeMatch() => State.IsPaused = false;
        public int GetSubsRemaining(bool home) => MAX_SUBS - (home ? _homeSubs : _awaySubs);

        private string MakeCommentary(LiveMatchEvent evt) => evt.Type switch
        {
            LiveEventType.YellowCard   => CommentaryGenerator.YellowCard(evt.Player?.Name, evt.Minute),
            LiveEventType.RedCard      => CommentaryGenerator.RedCard(evt.Player?.Name, evt.Minute),
            LiveEventType.SecondYellow => $"{evt.Minute}' Second yellow = red! {evt.Player?.Name} walks!",
            LiveEventType.Penalty      => $"{evt.Minute}' PENALTY for {evt.Team?.TeamName}!",
            LiveEventType.PenaltyMissed=> $"{evt.Minute}' Penalty missed by {evt.Player?.Name}!",
            LiveEventType.Corner       => $"{evt.Minute}' Corner for {evt.Team?.TeamName}.",
            LiveEventType.Substitution => CommentaryGenerator.Substitution(evt.SubstituteIn?.Name, evt.Player?.Name, evt.Minute),
            LiveEventType.Foul         => $"{evt.Minute}' Foul by {evt.Player?.Name}.",
            _ => $"{evt.Minute}' {evt.Type}"
        };

        private void SaveResult()
        {
            var dm = GameManager.Instance?.DataManager;
            if (dm == null) return;
            dm.SaveMatchResult(State);

            string uid = GameManager.Instance?.CurrentUserId ?? "";
            var pd = GameManager.Instance?.CurrentUserData;
            bool isHome = State.HomeTeam?.OwnerId == uid;
            bool win  = isHome ? State.HomeScore > State.AwayScore : State.AwayScore > State.HomeScore;
            bool draw = State.HomeScore == State.AwayScore;
            int gf = isHome ? State.HomeScore : State.AwayScore;
            int ga = isHome ? State.AwayScore : State.HomeScore;

            Ranking.RankingSystem.Instance?.UpdatePlayerRanking(
                uid, pd?.DisplayName ?? "", pd?.TeamId ?? "",
                pd?.Country ?? "", pd?.City ?? "", win, draw, gf, ga);
        }
    }
}
