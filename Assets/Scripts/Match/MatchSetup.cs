using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FootballGame.Core;
using FootballGame.Player;

namespace FootballGame.Match
{
    /// <summary>
    /// Place this on a GameObject in the Match scene (alongside MatchController + MatchSimulator).
    /// It loads the player's squad from Firebase and initializes the match.
    /// </summary>
    public class MatchSetup : MonoBehaviour
    {
        [Header("Optional: show while loading squad")]
        public GameObject loadingPanel;

        private static readonly string[] AITeamNames =
            { "FC Rivals", "City United", "Athletic Pro", "Royal FC", "Dynamo Stars",
              "Red Phoenix", "Blue Storm", "Golden Lions", "Iron Eagles", "Silver Wolves" };

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            loadingPanel?.SetActive(true);

            string uid = GameManager.Instance?.CurrentUserId ?? "";
            List<FootballPlayerData> squad = null;

            // Try to load the player's real squad
            if (!string.IsNullOrEmpty(uid) && TransferSystem.Instance != null)
            {
                bool done = false;
                TransferSystem.Instance.LoadUserSquad(uid, s => { squad = s; done = true; });
                yield return new WaitUntil(() => done);
            }

            loadingPanel?.SetActive(false);

            var home = BuildPlayerTeam(uid, GameManager.Instance?.CurrentTeam, squad);
            var away = BuildAITeam();

            MatchController.Instance?.InitMatch(home, away);
        }

        private MatchTeamData BuildPlayerTeam(string uid, TeamData teamData,
            List<FootballPlayerData> squad)
        {
            var team = new MatchTeamData
            {
                OwnerId  = uid,
                TeamName = teamData?.TeamName ?? "My Team",
                Formation = teamData?.Formation ?? "4-4-2",
            };

            if (squad == null || squad.Count == 0)
                squad = GenerateDefaultSquad(PlayerTier.Bronze);

            // Best 11 start, next 7 on bench
            squad.Sort((a, b) => b.Overall.CompareTo(a.Overall));
            int starters = Mathf.Min(11, squad.Count);
            for (int i = 0; i < starters; i++)
                team.OnFieldPlayers.Add(MatchPlayerData.From(squad[i]));

            int bench = Mathf.Min(7, squad.Count - starters);
            for (int i = starters; i < starters + bench; i++)
                team.Bench.Add(MatchPlayerData.From(squad[i]));

            // Fill to 11 if squad is smaller
            while (team.OnFieldPlayers.Count < 11)
                team.OnFieldPlayers.Add(MatchPlayerData.From(
                    FootballPlayerData.GenerateRandomPlayer(PlayerTier.Bronze)));

            return team;
        }

        private MatchTeamData BuildAITeam()
        {
            PlayerTier[] tiers = { PlayerTier.Bronze, PlayerTier.Silver, PlayerTier.Gold };
            var tier = tiers[Random.Range(0, tiers.Length)];

            var team = new MatchTeamData
            {
                OwnerId   = "ai",
                TeamName  = AITeamNames[Random.Range(0, AITeamNames.Length)],
                Formation = "4-3-3",
            };

            for (int i = 0; i < 11; i++)
                team.OnFieldPlayers.Add(MatchPlayerData.From(
                    FootballPlayerData.GenerateRandomPlayer(tier)));

            for (int i = 0; i < 7; i++)
                team.Bench.Add(MatchPlayerData.From(
                    FootballPlayerData.GenerateRandomPlayer(tier)));

            return team;
        }

        private List<FootballPlayerData> GenerateDefaultSquad(PlayerTier tier)
        {
            var list = new List<FootballPlayerData>();
            for (int i = 0; i < 18; i++)
                list.Add(FootballPlayerData.GenerateRandomPlayer(tier));
            return list;
        }
    }
}
