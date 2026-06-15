using System;
using System.Collections;
using UnityEngine;
using Firebase.Auth;
using Newtonsoft.Json;
using FootballGame.Core;
using FootballGame.Player;

namespace FootballGame.Authentication
{
    public class RegistrationManager : MonoBehaviour
    {
        public static RegistrationManager Instance { get; private set; }

        public static event Action<string> OnRegistrationComplete;
        public static event Action<string> OnRegistrationFailed;

        [SerializeField] private int minTeamNameLength = 3;
        [SerializeField] private int maxTeamNameLength = 20;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterNewUser(string email, string password, string teamName,
            string country, string city, string displayName, Action<bool, string> callback)
        {
            string valErr = Validate(email, password, teamName, displayName);
            if (!string.IsNullOrEmpty(valErr))
            {
                callback?.Invoke(false, valErr);
                OnRegistrationFailed?.Invoke(valErr);
                return;
            }

            AuthManager.Instance.RegisterWithEmail(email, password, (user, authErr) =>
            {
                if (user == null) { callback?.Invoke(false, authErr); OnRegistrationFailed?.Invoke(authErr); return; }

                string userId = user.UserId;
                string teamId = $"team_{userId}";

                var playerData = new PlayerData
                {
                    UserId = userId,
                    DisplayName = string.IsNullOrEmpty(displayName) ? email.Split('@')[0] : displayName,
                    Email = email,
                    TeamId = teamId,
                    TokenBalance = 500, // starter tokens
                    Country = country,
                    City = city,
                    DailyRewardDay = 0,
                    LastLoginDate = "",
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    PreferredLanguage = LocalizationManager.Instance?.CurrentLanguageCode ?? "en",
                    SoundEnabled = true,
                    MusicEnabled = true,
                    SoundVolume = 1f,
                    MusicVolume = 0.7f,
                };

                var teamData = new TeamData
                {
                    TeamId = teamId,
                    TeamName = teamName,
                    OwnerId = userId,
                    Country = country,
                    City = city,
                    Formation = "4-4-2",
                    KitPrimaryColor = "#FF0000",
                    KitSecondaryColor = "#FFFFFF",
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };

                StartCoroutine(SaveAndFinish(playerData, teamData, callback));
            });
        }

        private IEnumerator SaveAndFinish(PlayerData player, TeamData team, Action<bool, string> callback)
        {
            bool p1done = false, p2done = false;
            bool p1ok = false, p2ok = false;

            var dm = GameManager.Instance.DataManager;
            dm.SavePlayerDataDirect(player, ok => { p1ok = ok; p1done = true; });
            dm.SaveTeamDataDirect(team,     ok => { p2ok = ok; p2done = true; });

            float elapsed = 0f;
            const float timeout = 15f;
            yield return new WaitUntil(() =>
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                return (p1done && p2done) || elapsed >= timeout;
            });

            if (p1ok && p2ok)
            {
                GameManager.Instance.SetCurrentPlayer(player);
                GameManager.Instance.SetCurrentTeam(team);
                OnRegistrationComplete?.Invoke(player.UserId);
                callback?.Invoke(true, null);
            }
            else
            {
                string reason = elapsed >= timeout ? "Network timeout. Please try again." : "Failed to save data. Please try again.";
                callback?.Invoke(false, reason);
            }
        }

        private string Validate(string email, string password, string teamName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Email is required.";
            if (!email.Contains("@") || !email.Contains(".")) return "Invalid email address.";
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return "Password must be at least 6 characters.";
            if (string.IsNullOrWhiteSpace(teamName) || teamName.Length < minTeamNameLength)
                return $"Team name must be at least {minTeamNameLength} characters.";
            if (teamName.Length > maxTeamNameLength)
                return $"Team name must be at most {maxTeamNameLength} characters.";
            return null;
        }
    }
}
