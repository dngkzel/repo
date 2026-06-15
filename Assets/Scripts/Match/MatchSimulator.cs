using System.Collections;
using System.Linq;
using UnityEngine;

namespace FootballGame.Match
{
    public class MatchSimulator : MonoBehaviour
    {
        public static MatchSimulator Instance { get; private set; }

        public float SimulationSpeed = 1f;
        private const float BASE_MINUTE_INTERVAL = 1f;

        private MatchController _controller;
        private Coroutine _coroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartSimulation(MatchController controller)
        {
            _controller = controller;
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(SimulateMatch());
        }

        public void StopSimulation()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
        }

        public void SetSpeed(float speed) => SimulationSpeed = Mathf.Clamp(speed, 0.5f, 8f);

        private IEnumerator SimulateMatch()
        {
            var state = _controller.State;

            while (state.CurrentMinute <= 90 + state.InjuryTime)
            {
                yield return new WaitForSeconds(BASE_MINUTE_INTERVAL / SimulationSpeed);

                if (state.IsPaused)
                    yield return new WaitUntil(() => !state.IsPaused);

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

                if (state.CurrentMinute == 90)
                    state.InjuryTime = Random.Range(2, 8);

                SimulateMinute(state);
            }

            _controller.TriggerFullTime();
        }

        private void SimulateMinute(LiveMatchState state)
        {
            float homeStr = state.HomeTeam?.TotalOverall + 3f ?? 53f; // +3 home advantage
            float awayStr = state.AwayTeam?.TotalOverall ?? 50f;
            float total = homeStr + awayStr;
            if (total <= 0) total = 100f;

            float roll = Random.value;

            const float goalChance   = 0.028f;
            const float foulChance   = 0.06f;
            const float cornerChance = 0.04f;

            if (roll < goalChance)
            {
                bool homeScores = Random.value < (homeStr / total);
                var scoringTeam = homeScores ? state.HomeTeam : state.AwayTeam;
                var scorer  = GetAttacker(scoringTeam);
                var assist  = GetMidfielder(scoringTeam);

                if (scorer != null)
                {
                    scorer.MatchGoals++;
                    if (assist != null) assist.MatchAssists++;
                    if (homeScores) state.HomeScore++;
                    else state.AwayScore++;

                    _controller.TriggerGoal(new LiveMatchEvent
                    {
                        Type = LiveEventType.Goal,
                        Minute = state.CurrentMinute,
                        Team = scoringTeam,
                        Player = scorer,
                        AssistPlayer = assist,
                    });
                }
            }
            else if (roll < goalChance + foulChance)
            {
                bool homeCommits = Random.value < 0.5f;
                var foulingTeam = homeCommits ? state.HomeTeam : state.AwayTeam;
                var foulingOpponent = homeCommits ? state.AwayTeam : state.HomeTeam;
                var fouler = GetDefender(foulingTeam);
                if (fouler == null) return;

                float cardRoll = Random.value;
                LiveEventType eventType = LiveEventType.Foul;

                if (cardRoll < 0.03f)
                {
                    fouler.RedCard = true;
                    fouler.IsOnField = false;
                    eventType = LiveEventType.RedCard;
                    foulingTeam.OnFieldPlayers?.Remove(fouler);
                }
                else if (cardRoll < 0.15f)
                {
                    fouler.YellowCards++;
                    eventType = LiveEventType.YellowCard;
                    if (fouler.YellowCards >= 2)
                    {
                        fouler.RedCard = true;
                        fouler.IsOnField = false;
                        eventType = LiveEventType.SecondYellow;
                        foulingTeam.OnFieldPlayers?.Remove(fouler);
                    }
                }

                _controller.TriggerEvent(new LiveMatchEvent
                {
                    Type = eventType,
                    Minute = state.CurrentMinute,
                    Team = foulingTeam,
                    Player = fouler,
                });

                // Penalty chance
                if (cardRoll < 0.05f && eventType != LiveEventType.RedCard)
                    SimulatePenalty(state, foulingOpponent, !homeCommits);
            }
            else if (roll < goalChance + foulChance + cornerChance)
            {
                bool homeCorner = Random.value < (homeStr / total);
                _controller.TriggerEvent(new LiveMatchEvent
                {
                    Type = LiveEventType.Corner,
                    Minute = state.CurrentMinute,
                    Team = homeCorner ? state.HomeTeam : state.AwayTeam,
                });
            }

            _controller.UpdateMinute(state.CurrentMinute);
        }

        private void SimulatePenalty(LiveMatchState state, MatchTeamData team, bool isHome)
        {
            var taker = team?.OnFieldPlayers?.OrderByDescending(p => p.Shooting).FirstOrDefault();
            bool scored = Random.value < 0.75f;

            if (scored && taker != null)
            {
                taker.MatchGoals++;
                if (isHome) state.HomeScore++;
                else state.AwayScore++;

                _controller.TriggerGoal(new LiveMatchEvent
                {
                    Type = LiveEventType.Penalty,
                    Minute = state.CurrentMinute,
                    Team = team,
                    Player = taker,
                });
            }
            else
            {
                _controller.TriggerEvent(new LiveMatchEvent
                {
                    Type = LiveEventType.PenaltyMissed,
                    Minute = state.CurrentMinute,
                    Team = team,
                    Player = taker,
                });
            }
        }

        private MatchPlayerData GetAttacker(MatchTeamData team)
        {
            var list = team?.OnFieldPlayers?.Where(p =>
                p.IsOnField && !p.RedCard &&
                (p.Position == "ST" || p.Position == "CF" || p.Position == "LW" || p.Position == "RW")).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : GetRandom(team);
        }

        private MatchPlayerData GetMidfielder(MatchTeamData team)
        {
            var list = team?.OnFieldPlayers?.Where(p =>
                p.IsOnField && !p.RedCard &&
                (p.Position == "CM" || p.Position == "CAM" || p.Position == "CDM" || p.Position == "LM" || p.Position == "RM")).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        private MatchPlayerData GetDefender(MatchTeamData team)
        {
            var list = team?.OnFieldPlayers?.Where(p =>
                p.IsOnField && !p.RedCard &&
                (p.Position == "CB" || p.Position == "LB" || p.Position == "RB" || p.Position == "CDM")).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : GetRandom(team);
        }

        private MatchPlayerData GetRandom(MatchTeamData team)
        {
            var list = team?.OnFieldPlayers?.Where(p => p.IsOnField && !p.RedCard).ToList();
            return list?.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }
    }
}
