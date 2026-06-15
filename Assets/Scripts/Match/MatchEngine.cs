using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using FootballGame.Player;
using FootballGame.Core;

namespace FootballGame.Match
{
    // =========================================================================
    // Supporting enums and data classes
    // =========================================================================

    public enum MatchState
    {
        Idle,
        PreMatch,
        FirstHalf,
        HalfTime,
        SecondHalf,
        ExtraTime,
        PenaltyShootout,
        FullTime
    }

    [Serializable]
    public class MatchData
    {
        public Player.TeamData     homeTeam;
        public Player.TeamData     awayTeam;
        public int                 homeScore;
        public int                 awayScore;
        public List<MatchEvent>    events;
        public int                 currentMinute;
        public bool                isLive;

        public MatchData()
        {
            events        = new List<MatchEvent>();
            homeScore     = 0;
            awayScore     = 0;
            currentMinute = 0;
            isLive        = false;
        }
    }

    // =========================================================================
    // MatchEngine
    // =========================================================================

    public class MatchEngine : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        private static MatchEngine _instance;

        public static MatchEngine Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("MatchEngine");
                    _instance = go.AddComponent<MatchEngine>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ── Public events ─────────────────────────────────────────────────────

        /// <summary>Fired for every match event (goals, cards, etc.).</summary>
        public event Action<MatchEvent>  OnMatchEvent;

        /// <summary>Fired when the match ends; carries the final MatchData.</summary>
        public event Action<MatchData>   OnMatchEnd;

        /// <summary>Fired each simulated minute with the current minute value.</summary>
        public event Action<int>         OnMinuteTick;

        /// <summary>Fired when the first half ends.</summary>
        public event Action              OnHalfTime;

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>
        /// Playback speed multiplier.  1 = real-time (1 second per game minute),
        /// 2 = double speed, 4 = quadruple speed.
        /// Clamp to supported values: 1, 2, 4.
        /// </summary>
        private float _matchSpeed = 1f;

        public float MatchSpeed
        {
            get => _matchSpeed;
            set => _matchSpeed = Mathf.Clamp(value, 0.25f, 8f);
        }

        // ── State ─────────────────────────────────────────────────────────────

        public MatchState    CurrentState { get; private set; } = MatchState.Idle;
        public MatchData     CurrentMatch { get; private set; }

        private Coroutine    _matchCoroutine;

        // Extra time / stoppage time added to each half (1–5 min)
        private const int FirstHalfStoppageMin  = 45;
        private const int SecondHalfStoppageMin = 90;
        private const int ExtraTimeEndMin       = 120;

        // Substitution tracking — max 3 per team
        private int _homeSubCount;
        private int _awaySubCount;
        private const int MaxSubstitutions = 3;

        // ── Firebase ──────────────────────────────────────────────────────────

        private FirebaseFirestore _firestore;
        private bool              _firestoreReady;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitFirestore();
        }

        private void InitFirestore()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _firestore      = FirebaseFirestore.DefaultInstance;
                    _firestoreReady = true;
                    Debug.Log("[MatchEngine] Firebase ready.");
                }
                else
                {
                    Debug.LogWarning($"[MatchEngine] Firebase unavailable: {task.Result}. Match results will not be persisted.");
                }
            });
        }

        // =========================================================================
        // Public API
        // =========================================================================

        /// <summary>Initialises a new match and begins the simulation coroutine.</summary>
        public void StartMatch(Player.TeamData home, Player.TeamData away)
        {
            if (CurrentState != MatchState.Idle && CurrentState != MatchState.FullTime)
            {
                Debug.LogWarning("[MatchEngine] StartMatch called while a match is already in progress.");
                return;
            }

            if (home == null || away == null)
            {
                Debug.LogError("[MatchEngine] StartMatch: home or away TeamData is null.");
                return;
            }

            // Reset state
            CurrentMatch = new MatchData
            {
                homeTeam = home,
                awayTeam = away,
                isLive   = false
            };

            _homeSubCount = 0;
            _awaySubCount = 0;

            SetState(MatchState.PreMatch);

            if (_matchCoroutine != null)
                StopCoroutine(_matchCoroutine);

            _matchCoroutine = StartCoroutine(RunMatch());
        }

        /// <summary>Allows the owning controller to request a substitution mid-match.</summary>
        public bool RequestSubstitution(Player.TeamData team, string playerOutId, string playerInId)
        {
            if (CurrentMatch == null) return false;

            bool isHome = team == CurrentMatch.homeTeam;
            int  count  = isHome ? _homeSubCount : _awaySubCount;

            if (count >= MaxSubstitutions)
            {
                Debug.LogWarning($"[MatchEngine] Max substitutions ({MaxSubstitutions}) reached for {team.name}.");
                return false;
            }

            bool swapped = team.MakeSubstitution(playerOutId, playerInId);
            if (!swapped)
            {
                Debug.LogWarning("[MatchEngine] RequestSubstitution: player IDs not found in expected lists.");
                return false;
            }

            if (isHome) _homeSubCount++;
            else        _awaySubCount++;

            PlayerData playerOut = team.bench.Find(p => p.id == playerOutId)
                                ?? team.starters.Find(p => p.id == playerOutId);
            PlayerData playerIn  = team.starters.Find(p => p.id == playerInId);

            SubstitutionEvent subEvt = new SubstitutionEvent(
                CurrentMatch.currentMinute,
                playerIn,
                playerOut,
                team.name);

            subEvt.description = CommentaryGenerator.GenerateSubstitutionCommentary(subEvt);
            TriggerEvent(subEvt);
            return true;
        }

        // =========================================================================
        // Match coroutine
        // =========================================================================

        /// <summary>Main simulation loop. Runs the full 90-minute match with halftime.</summary>
        private IEnumerator RunMatch()
        {
            // ----- PRE-MATCH delay -----
            CurrentMatch.isLive = false;
            yield return new WaitForSeconds(1f);

            // ===== FIRST HALF =====
            SetState(MatchState.FirstHalf);
            CurrentMatch.isLive = true;

            int stoppageFirstHalf = UnityEngine.Random.Range(1, 6);   // 1–5 extra minutes
            int firstHalfEnd     = FirstHalfStoppageMin + stoppageFirstHalf;

            for (int minute = 1; minute <= firstHalfEnd; minute++)
            {
                CurrentMatch.currentMinute = minute;
                OnMinuteTick?.Invoke(minute);
                SimulateMinute(minute);
                yield return new WaitForSeconds(SecondsPerMinute());
            }

            // ===== HALF-TIME =====
            SetState(MatchState.HalfTime);
            CurrentMatch.isLive = false;
            OnHalfTime?.Invoke();

            yield return new WaitForSeconds(Mathf.Max(1f, 3f / _matchSpeed));

            // ===== SECOND HALF =====
            SetState(MatchState.SecondHalf);
            CurrentMatch.isLive = true;

            int stoppageSecondHalf = UnityEngine.Random.Range(2, 8);  // 2–7 extra minutes
            int secondHalfEnd     = SecondHalfStoppageMin + stoppageSecondHalf;

            for (int minute = FirstHalfStoppageMin + 1; minute <= secondHalfEnd; minute++)
            {
                CurrentMatch.currentMinute = minute;
                OnMinuteTick?.Invoke(minute);
                SimulateMinute(minute);
                yield return new WaitForSeconds(SecondsPerMinute());
            }

            // Full-time
            EndMatch();
        }

        // =========================================================================
        // Per-minute simulation
        // =========================================================================

        /// <summary>
        /// Calculates probabilities for each possible event type this minute and
        /// fires any that occur.  Multiple events can happen in the same minute.
        /// </summary>
        private void SimulateMinute(int minute)
        {
            if (CurrentMatch == null) return;

            Player.TeamData home = CurrentMatch.homeTeam;
            Player.TeamData away = CurrentMatch.awayTeam;

            // Determine which team attacks this minute based on a midfield coin-flip
            // weighted by team ratings.
            bool homeAttacks = MatchSimulator.HomeTeamAttacksThisMinute(home, away);
            Player.TeamData attacking = homeAttacks ? home : away;
            Player.TeamData defending = homeAttacks ? away : home;

            // ── Offsides ──────────────────────────────────────────────────────
            if (UnityEngine.Random.value < MatchSimulator.CalculateOffsideProbability())
            {
                OffsidesEvent offEvt = new OffsidesEvent(minute, attacking.name);
                offEvt.description = CommentaryGenerator.GenerateOffsideCommentary(offEvt);
                TriggerEvent(offEvt);
                return; // offsides stops further play this minute
            }

            // ── Foul ──────────────────────────────────────────────────────────
            if (UnityEngine.Random.value < MatchSimulator.CalculateFoulProbability())
            {
                PlayerData fouler = MatchSimulator.SelectRandomPlayer(defending);
                PlayerData fouled = MatchSimulator.SelectRandomPlayer(attacking);
                bool bookable     = UnityEngine.Random.value < 0.35f;

                FoulEvent foulEvt = new FoulEvent(minute, fouler, fouled, bookable, defending.name);
                foulEvt.description = CommentaryGenerator.GenerateFoulCommentary(foulEvt);
                TriggerEvent(foulEvt);

                // Bookable fouls may lead to a card
                if (bookable)
                {
                    SimulateCard(minute, fouler, defending.name);
                }
                return;
            }

            // ── Corner ────────────────────────────────────────────────────────
            if (UnityEngine.Random.value < MatchSimulator.CalculateCornerProbability())
            {
                CornerEvent cornerEvt = new CornerEvent(minute, attacking.name);
                cornerEvt.description = CommentaryGenerator.GenerateCornerCommentary(cornerEvt);
                TriggerEvent(cornerEvt);
                // Corner may still lead to a shot/goal below — intentional fall-through
            }

            // ── Shot ──────────────────────────────────────────────────────────
            int shotCount = MatchSimulator.CalculateShotCount(attacking, minute);
            for (int i = 0; i < shotCount; i++)
            {
                PlayerData shooter  = MatchSimulator.SelectGoalScorer(attacking);
                float goalProb      = MatchSimulator.CalculateGoalProbability(attacking, defending, minute);
                bool  onTarget      = UnityEngine.Random.value < 0.45f;
                bool  isGoal        = onTarget && (UnityEngine.Random.value < goalProb);

                ShotEvent shotEvt = new ShotEvent(minute, shooter, onTarget, isGoal, attacking.name);
                shotEvt.description = CommentaryGenerator.GenerateShotCommentary(shotEvt);
                TriggerEvent(shotEvt);

                if (isGoal)
                {
                    SimulateGoal(minute, shooter, attacking, defending);
                }
            }

            // ── Substitution ──────────────────────────────────────────────────
            // Check for AI-controlled substitutions (both teams)
            TrySimulateSubstitution(minute, home, ref _homeSubCount);
            TrySimulateSubstitution(minute, away, ref _awaySubCount);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SimulateGoal(int minute, PlayerData scorer, Player.TeamData attacking, Player.TeamData defending)
        {
            // Check own goal (rare – ~3 % chance)
            bool isOwnGoal = UnityEngine.Random.value < 0.03f;

            PlayerData assister = null;
            if (!isOwnGoal && UnityEngine.Random.value > 0.25f)
                assister = MatchSimulator.SelectAssister(attacking, scorer);

            string teamName = isOwnGoal ? defending.name : attacking.name;

            GoalEvent goalEvt = new GoalEvent(minute, scorer, assister, isOwnGoal, teamName);
            goalEvt.description = CommentaryGenerator.GenerateGoalCommentary(goalEvt);

            // Update score
            if (CurrentMatch != null)
            {
                bool homeScored = isOwnGoal
                    ? (attacking == CurrentMatch.awayTeam)   // own goal by away = home scores
                    : (attacking == CurrentMatch.homeTeam);

                if (homeScored) CurrentMatch.homeScore++;
                else            CurrentMatch.awayScore++;
            }

            TriggerEvent(goalEvt);
        }

        private void SimulateCard(int minute, PlayerData player, string team)
        {
            if (player == null) return;

            CardType cardType = MatchSimulator.IsYellowCard() ? CardType.Yellow : CardType.Red;
            string   reason   = cardType == CardType.Yellow ? "Reckless challenge" : "Serious foul play";

            CardEvent cardEvt = new CardEvent(minute, player, cardType, reason, team);
            cardEvt.description = CommentaryGenerator.GenerateCardCommentary(cardEvt);
            TriggerEvent(cardEvt);
        }

        private void TrySimulateSubstitution(int minute, Player.TeamData team, ref int subCount)
        {
            if (subCount >= MaxSubstitutions) return;
            if (team.bench == null || team.bench.Count == 0) return;
            if (!MatchSimulator.ShouldSubstitute(minute, team)) return;

            var (playerOut, playerIn) = MatchSimulator.SelectSubstitutionPair(team);
            if (playerOut == null || playerIn == null) return;

            team.MakeSubstitution(playerOut.id, playerIn.id);
            subCount++;

            SubstitutionEvent subEvt = new SubstitutionEvent(minute, playerIn, playerOut, team.name);
            subEvt.description = CommentaryGenerator.GenerateSubstitutionCommentary(subEvt);
            TriggerEvent(subEvt);
        }

        /// <summary>Seconds to wait per simulated minute, adjusted for MatchSpeed.</summary>
        private float SecondsPerMinute() => 1f / Mathf.Max(0.01f, _matchSpeed);

        // =========================================================================
        // Event dispatch
        // =========================================================================

        public void TriggerEvent(MatchEvent evt)
        {
            if (evt == null) return;
            CurrentMatch?.events.Add(evt);
            OnMatchEvent?.Invoke(evt);
        }

        // =========================================================================
        // End match
        // =========================================================================

        public void EndMatch()
        {
            if (CurrentMatch == null) return;

            CurrentMatch.isLive = false;
            SetState(MatchState.FullTime);

            Debug.Log($"[MatchEngine] Match ended: {CurrentMatch.homeTeam?.name} {CurrentMatch.homeScore} – " +
                      $"{CurrentMatch.awayScore} {CurrentMatch.awayTeam?.name}");

            OnMatchEnd?.Invoke(CurrentMatch);

            // Persist to Firebase on a background task – fire and forget
            _ = SaveMatchToFirebase(CurrentMatch);
        }

        private async Task SaveMatchToFirebase(MatchData data)
        {
            if (!_firestoreReady)
            {
                Debug.LogWarning("[MatchEngine] Firestore not ready — match result not persisted.");
                return;
            }

            try
            {
                string userId = Authentication.AuthManager.Instance?.GetCurrentUser()?.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.LogWarning("[MatchEngine] No authenticated user — match result not persisted.");
                    return;
                }

                string homeScore = data.homeScore.ToString();
                string awayScore = data.awayScore.ToString();
                int    home      = data.homeScore;
                int    away      = data.awayScore;

                string outcome = home > away ? "win"
                               : home < away ? "loss"
                               : "draw";

                int tokensEarned = outcome == "win"  ? 50
                                 : outcome == "draw" ? 20
                                 : 10;

                // Build a Firestore-compatible dictionary (no MatchEvent serialization required)
                var matchDoc = new Dictionary<string, object>
                {
                    { "matchId",       Guid.NewGuid().ToString() },
                    { "userId",        userId },
                    { "homeTeamName",  data.homeTeam?.name ?? string.Empty },
                    { "awayTeamName",  data.awayTeam?.name ?? string.Empty },
                    { "homeScore",     home },
                    { "awayScore",     away },
                    { "outcome",       outcome },
                    { "tokensEarned",  tokensEarned },
                    { "totalMinutes",  data.currentMinute },
                    { "totalEvents",   data.events?.Count ?? 0 },
                    { "playedAt",      DateTime.UtcNow.ToString("o") }
                };

                string matchId = matchDoc["matchId"].ToString();

                await _firestore
                    .Collection("players")
                    .Document(userId)
                    .Collection("matches")
                    .Document(matchId)
                    .SetAsync(matchDoc);

                Debug.Log($"[MatchEngine] Match '{matchId}' saved for user '{userId}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchEngine] SaveMatchToFirebase error: {ex.Message}");
            }
        }

        // =========================================================================
        // State machine helper
        // =========================================================================

        private void SetState(MatchState newState)
        {
            Debug.Log($"[MatchEngine] State: {CurrentState} → {newState}");
            CurrentState = newState;
        }
    }
}
