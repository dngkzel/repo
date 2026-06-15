using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using FootballGame.Core;

namespace FootballGame.Authentication
{
    public class RegistrationManager : MonoBehaviour
    {
        public static RegistrationManager Instance { get; private set; }

        public static event Action<string> OnRegistrationComplete;
        public static event Action<string> OnRegistrationFailed;

        [Header("Team Name Validation")]
        [SerializeField] private int minTeamNameLength = 3;
        [SerializeField] private int maxTeamNameLength = 20;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterNewUser(string email, string password, string teamName, string country, string city,
            string displayName, Action<bool, string> callback)
        {
            // Validate inputs
            string validationError = ValidateRegistrationInputs(email, password, teamName, displayName);
            if (!string.IsNullOrEmpty(validationError))
            {
                callback?.Invoke(false, validationError);
                OnRegistrationFailed?.Invoke(validationError);
                return;
            }

            // Register with Firebase Auth
            AuthManager.Instance.RegisterWithEmail(email, password, (user, authError) =>
            {
                if (user == null)
                {
                    callback?.Invoke(false, authError);
                    return;
                }

                // Create player profile
                StartCoroutine(CreatePlayerProfile(user, teamName, displayName, country, city, callback));
            });
        }

        public void RegisterWithGoogle(string teamName, string country, string city, Action<bool, string> callback)
        {
            string teamError = ValidateTeamName(teamName);
            if (!string.IsNullOrEmpty(teamError))
            {
                callback?.Invoke(false, teamError);
                return;
            }

            AuthManager.Instance.LoginWithGoogle((user, error) =>
            {
                if (user == null)
                {
                    callback?.Invoke(false, error);
                    return;
                }
                StartCoroutine(CreatePlayerProfile(user, teamName, user.DisplayName ?? "Player", country, city, callback));
            });
        }

        private IEnumerator CreatePlayerProfile(FirebaseUser user, string teamName, string displayName,
            string country, string city, Action<bool, string> callback)
        {
            // Create starter roster
            var starterPlayers = GenerateStarterRoster(teamName);
            string teamId = System.Guid.NewGuid().ToString();

            var team = new Player.TeamData
            {
                TeamId = teamId,
                TeamName = teamName,
                OwnerId = user.UserId,
                Country = country,
                City = city,
                Formation = "4-3-3",
                TotalMatches = 0,
                Wins = 0,
                Draws = 0,
                Losses = 0,
                GoalsScored = 0,
                GoalsConceded = 0,
                Points = 0,
                PlayerIds = new List<string>(),
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (var p in starterPlayers)
                team.PlayerIds.Add(p.PlayerId);

            var playerProfile = new Player.PlayerData
            {
                UserId = user.UserId,
                DisplayName = string.IsNullOrEmpty(displayName) ? user.DisplayName ?? "Manager" : displayName,
                Email = user.Email,
                TeamId = teamId,
                TokenBalance = 500, // Starting tokens
                IsPremium = false,
                PremiumExpiry = 0,
                Country = country,
                City = city,
                DailyRewardDay = 0,
                LastLoginDate = "",
                TotalPoints = 0,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            bool playerSaved = false;
            bool teamSaved = false;

            yield return StartCoroutine(GameManager.Instance.DataManager.SavePlayerData(playerProfile, success => playerSaved = true));
            yield return StartCoroutine(GameManager.Instance.DataManager.SaveTeamData(team, success => teamSaved = true));

            // Save individual football players
            foreach (var fp in starterPlayers)
            {
                yield return StartCoroutine(SaveFootballPlayer(fp));
            }

            // Initialize ranking entry
            var rankEntry = new Ranking.RankEntry
            {
                UserId = user.UserId,
                TeamName = teamName,
                DisplayName = playerProfile.DisplayName,
                Points = 0,
                Wins = 0,
                Country = country,
                City = city
            };
            GameManager.Instance.DataManager.UpdateRankingEntry(user.UserId, rankEntry);

            // Update game manager
            GameManager.Instance.SetCurrentPlayer(playerProfile);
            GameManager.Instance.SetCurrentTeam(team);

            callback?.Invoke(true, null);
            OnRegistrationComplete?.Invoke(user.UserId);
        }

        private IEnumerator SaveFootballPlayer(Player.FootballPlayerData player)
        {
            bool done = false;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(player);
            Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference
                .Child("footballPlayers").Child(player.PlayerId).SetRawJsonValueAsync(json)
                .ContinueWith(t => done = true);
            yield return new WaitUntil(() => done);
        }

        private List<Player.FootballPlayerData> GenerateStarterRoster(string teamName)
        {
            var players = new List<Player.FootballPlayerData>();
            var positions = new[]
            {
                "GK", "CB", "CB", "LB", "RB",
                "CM", "CM", "CM",
                "LW", "RW", "ST"
            };

            string[] firstNames = { "Alex", "Marco", "Lucas", "James", "Carlos", "David", "Pedro", "Luca", "Kai", "Finn", "Leon" };
            string[] lastNames = { "Silva", "Müller", "Garcia", "Johnson", "Rossi", "Dupont", "Santos", "Costa", "Schmidt", "Martin", "López" };

            System.Random rng = new System.Random();
            for (int i = 0; i < 11; i++)
            {
                int overall = rng.Next(55, 70);
                players.Add(new Player.FootballPlayerData
                {
                    PlayerId = System.Guid.NewGuid().ToString(),
                    Name = $"{firstNames[i % firstNames.Length]} {lastNames[i % lastNames.Length]}",
                    Position = positions[i],
                    Overall = overall,
                    Speed = rng.Next(50, 75),
                    Shooting = positions[i] == "GK" ? rng.Next(20, 40) : rng.Next(50, 75),
                    Passing = rng.Next(50, 75),
                    Dribbling = rng.Next(50, 75),
                    Defending = positions[i] == "GK" || positions[i].Contains("B") ? rng.Next(60, 80) : rng.Next(40, 65),
                    Physical = rng.Next(55, 75),
                    GoalKeeping = positions[i] == "GK" ? rng.Next(65, 80) : rng.Next(10, 30),
                    Tier = Player.FootballPlayerData.PlayerTier.Bronze,
                    Age = rng.Next(19, 33),
                    Nationality = "International",
                    OwnerId = "",
                    IsForSale = false,
                    Price = 0
                });
            }
            return players;
        }

        private string ValidateRegistrationInputs(string email, string password, string teamName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
            if (!email.Contains("@") || !email.Contains(".")) return "Invalid email format.";
            if (string.IsNullOrWhiteSpace(password)) return "Password is required.";
            if (password.Length < 6) return "Password must be at least 6 characters.";
            return ValidateTeamName(teamName);
        }

        private string ValidateTeamName(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName)) return "Team name is required.";
            if (teamName.Length < minTeamNameLength) return $"Team name must be at least {minTeamNameLength} characters.";
            if (teamName.Length > maxTeamNameLength) return $"Team name must be at most {maxTeamNameLength} characters.";
            foreach (char c in teamName)
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
                    return "Team name contains invalid characters.";
            return null;
        }
    }
}
