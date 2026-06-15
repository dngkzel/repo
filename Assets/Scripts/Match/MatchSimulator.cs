using System.Collections;
using System.Linq;
using UnityEngine;

namespace FootballGame.Match
{
    public class MatchSimulator : MonoBehaviour
    {
        public static MatchSimulator Instance { get; private set; }

        public float SimulationSpeed = 1f;
        private const float BASE_INTERVAL = 1f;
        private MatchController _controller;
        private Coroutine _coroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartSimulation(MatchController ctrl)
        {
            _controller = ctrl;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(Simulate());
        }

        public void StopSimulation() { if (_coroutine != null) StopCoroutine(_coroutine); }
        public void SetSpeed(float s) => SimulationSpeed = Mathf.Clamp(s, 0.5f, 8f);

        private IEnumerator Simulate()
        {
            var state = _controller.State;
            while (state.CurrentMinute <= 90 + state.InjuryTime)
            {
                yield return new WaitForSeconds(BASE_INTERVAL / SimulationSpeed);
                if (state.IsPaused) yield return new WaitUntil(() => !state.IsPaused);

                state.CurrentMinute++;

                if (state.CurrentMinute == 45 && !state.HalftimeDone)
                {
                    state.InjuryTime = Random.Range(1, 6);
                    _controller.TriggerHalftime();
                    yield return new WaitForSeconds(3f / SimulationSpeed);
                    state.HalftimeDone = true;
                    state.InjuryTime = 0;
                    continue;
                }
                if (state.CurrentMinute == 90) state.InjuryTime = Random.Range(2, 8);

                SimulateMinute(state);
            }
            _controller.TriggerFullTime();
        }

        private void SimulateMinute(LiveMatchState state)
        {
            float hStr = (state.HomeTeam?.TotalOverall ?? 50) + 3f;
            float aStr = state.AwayTeam?.TotalOverall ?? 50;
            float total = hStr + aStr;
            float roll = Random.value;

            const float goalChance   = 0.028f;
            const float foulChance   = 0.06f;
            const float cornerChance = 0.04f;

            if (roll < goalChance)
            {
                bool homeScores = Random.value < hStr / total;
                var team = homeScores ? state.HomeTeam : state.AwayTeam;
                var scorer = GetAttacker(team) ?? GetRandom(team);
                var assist = GetMidfielder(team);

                if (scorer != null)
                {
                    scorer.MatchGoals++;
                    if (assist != null) assist.MatchAssists++;
                    if (homeScores) state.HomeScore++; else state.AwayScore++;

                    _controller.TriggerGoal(new LiveMatchEvent
                    {
                        Type = LiveEventType.Goal, Minute = state.CurrentMinute,
                        Team = team, Player = scorer, AssistPlayer = assist,
                    });
                }
            }
            else if (roll < goalChance + foulChance)
            {
                bool homeCommits = Random.value < 0.5f;
                var fouler_team = homeCommits ? state.HomeTeam : state.AwayTeam;
                var victim_team = homeCommits ? state.AwayTeam : state.HomeTeam;
                var fouler = GetDefender(fouler_team) ?? GetRandom(fouler_team);
                if (fouler == null) goto updateMinute;

                float croll = Random.value;
                LiveEventType etype = LiveEventType.Foul;

                if (croll < 0.03f)
                {
                    fouler.RedCard = true; fouler.IsOnField = false;
                    etype = LiveEventType.RedCard;
                    fouler_team.OnFieldPlayers.Remove(fouler);
                }
                else if (croll < 0.15f)
                {
                    fouler.YellowCards++;
                    etype = LiveEventType.YellowCard;
                    if (fouler.YellowCards >= 2)
                    {
                        fouler.RedCard = true; fouler.IsOnField = false;
                        etype = LiveEventType.SecondYellow;
                        fouler_team.OnFieldPlayers.Remove(fouler);
                    }
                }

                _controller.TriggerEvent(new LiveMatchEvent
                {
                    Type = etype, Minute = state.CurrentMinute, Team = fouler_team, Player = fouler
                });

                if (croll < 0.05f && etype != LiveEventType.RedCard)
                    SimulatePenalty(state, victim_team, !homeCommits);
            }
            else if (roll < goalChance + foulChance + cornerChance)
            {
                bool homeCorner = Random.value < hStr / total;
                _controller.TriggerEvent(new LiveMatchEvent
                {
                    Type = LiveEventType.Corner, Minute = state.CurrentMinute,
                    Team = homeCorner ? state.HomeTeam : state.AwayTeam
                });
            }

            updateMinute:
            _controller.UpdateMinute(state.CurrentMinute);
        }

        private void SimulatePenalty(LiveMatchState state, MatchTeamData team, bool isHome)
        {
            var taker = team?.OnFieldPlayers?.Where(p => p.IsOnField && !p.RedCard)
                .OrderByDescending(p => p.Shooting).FirstOrDefault();
            if (taker == null) return; // no eligible player — skip penalty
            bool scored = Random.value < 0.75f;
            if (scored)
            {
                taker.MatchGoals++;
                if (isHome) state.HomeScore++; else state.AwayScore++;
                _controller.TriggerGoal(new LiveMatchEvent { Type = LiveEventType.Penalty, Minute = state.CurrentMinute, Team = team, Player = taker });
            }
            else
                _controller.TriggerEvent(new LiveMatchEvent { Type = LiveEventType.PenaltyMissed, Minute = state.CurrentMinute, Team = team, Player = taker });
        }

        private MatchPlayerData GetAttacker(MatchTeamData t) => GetFiltered(t, new[] { "ST","CF","LW","RW" });
        private MatchPlayerData GetMidfielder(MatchTeamData t) => GetFiltered(t, new[] { "CM","CAM","CDM","LM","RM" });
        private MatchPlayerData GetDefender(MatchTeamData t) => GetFiltered(t, new[] { "CB","LB","RB","CDM" });

        private MatchPlayerData GetFiltered(MatchTeamData team, string[] positions)
        {
            var list = team?.OnFieldPlayers?.Where(p => p.IsOnField && !p.RedCard && System.Array.IndexOf(positions, p.Position) >= 0).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        private MatchPlayerData GetRandom(MatchTeamData team)
        {
            var list = team?.OnFieldPlayers?.Where(p => p.IsOnField && !p.RedCard).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }
    }
}
