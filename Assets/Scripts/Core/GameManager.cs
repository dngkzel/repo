using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

namespace FootballGame.Core
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<GameManager>();
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action OnGameInitialized;
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<string> OnError;
        #endregion

        #region State
        public enum GameState
        {
            Initializing, Login, MainMenu, TeamSelection,
            Match, Market, Rankings, Settings, DailyReward
        }

        private GameState _currentState = GameState.Initializing;
        public GameState CurrentState => _currentState;
        public bool IsFirebaseReady { get; private set; }
        public bool IsUserLoggedIn => FirebaseAuth.DefaultInstance?.CurrentUser != null;
        public string CurrentUserId => FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        #endregion

        #region References
        [Header("System References")]
        public DataManager DataManager;
        public LocalizationManager LocalizationManager;
        public Audio.AudioManager AudioManager;
        public Economy.TokenSystem TokenSystem;
        public Economy.DailyRewardSystem DailyRewardSystem;
        public Ranking.RankingSystem RankingSystem;
        public Player.TransferSystem TransferSystem;
        #endregion

        public Player.PlayerData CurrentPlayer { get; private set; }
        public Player.TeamData CurrentTeam { get; private set; }
        public Player.PlayerData CurrentUserData => CurrentPlayer;
        public int CurrentTokenBalance { get; private set; }
        public bool IsPremium { get; private set; }

        private bool _tokenListenerRegistered;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystems();
        }

        private void Start() => StartCoroutine(InitializeFirebase());

        private void InitializeSystems()
        {
            if (DataManager == null)        DataManager        = GetOrAddComponent<DataManager>();
            if (LocalizationManager == null) LocalizationManager = GetOrAddComponent<LocalizationManager>();
            if (AudioManager == null)       AudioManager       = GetOrAddComponent<Audio.AudioManager>();
            if (TokenSystem == null)        TokenSystem        = GetOrAddComponent<Economy.TokenSystem>();
            if (DailyRewardSystem == null)  DailyRewardSystem  = GetOrAddComponent<Economy.DailyRewardSystem>();
            if (RankingSystem == null)      RankingSystem      = GetOrAddComponent<Ranking.RankingSystem>();
            if (TransferSystem == null)     TransferSystem     = GetOrAddComponent<Player.TransferSystem>();
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        private IEnumerator InitializeFirebase()
        {
            SetState(GameState.Initializing);
            var task = FirebaseApp.CheckAndFixDependenciesAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Result == DependencyStatus.Available)
            {
                IsFirebaseReady = true;
                OnGameInitialized?.Invoke();
                FirebaseAuth.DefaultInstance.StateChanged += OnAuthStateChanged;
                CheckInitialAuthState();
            }
            else
            {
                string err = $"Firebase error: {task.Result}";
                Debug.LogError(err);
                OnError?.Invoke(err);
            }
        }

        private void CheckInitialAuthState()
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null) StartCoroutine(LoadPlayerData(user.UserId));
            else SetState(GameState.Login);
        }

        private void OnAuthStateChanged(object sender, System.EventArgs e)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user != null)
                StartCoroutine(LoadPlayerData(user.UserId));
            else
            {
                // Unsubscribe from previous user's token updates
                UnregisterTokenListener();
                CurrentPlayer = null;
                CurrentTeam = null;
                TokenSystem?.ResetForLogout();
                SetState(GameState.Login);
            }
        }

        private IEnumerator LoadPlayerData(string userId)
        {
            yield return StartCoroutine(DataManager.LoadPlayerData(userId, p => CurrentPlayer = p));

            if (CurrentPlayer == null)
                SetState(GameState.TeamSelection);
            else
            {
                yield return StartCoroutine(DataManager.LoadTeamData(CurrentPlayer.TeamId, t => CurrentTeam = t));

                // Initialize TokenSystem with the already-loaded balance as starting value
                // to avoid a race where AddTokens fires before the Firebase load completes
                TokenSystem?.Initialize(userId, CurrentPlayer.TokenBalance);
                if (!_tokenListenerRegistered && TokenSystem != null)
                {
                    TokenSystem.OnBalanceChanged += OnTokenBalanceChanged;
                    _tokenListenerRegistered = true;
                }

                Economy.PremiumSystem.Instance?.LoadPremiumStatus(userId);

                CurrentTokenBalance = CurrentPlayer.TokenBalance;
                IsPremium = CurrentPlayer.IsPremium;
                DailyRewardSystem?.CheckDailyReward(CurrentPlayer);
                SetState(GameState.MainMenu);
            }
        }

        private void OnTokenBalanceChanged(int newBalance)
        {
            CurrentTokenBalance = newBalance;
            if (CurrentPlayer != null) CurrentPlayer.TokenBalance = newBalance;
        }

        private void UnregisterTokenListener()
        {
            if (_tokenListenerRegistered && TokenSystem != null)
            {
                TokenSystem.OnBalanceChanged -= OnTokenBalanceChanged;
                _tokenListenerRegistered = false;
            }
        }

        public void SetCurrentPlayer(Player.PlayerData player)
        {
            CurrentPlayer = player;
            CurrentTokenBalance = player.TokenBalance;
            IsPremium = player.IsPremium;
        }

        public void SetCurrentTeam(Player.TeamData team) => CurrentTeam = team;

        public void UpdateTokenBalance(int newBalance)
        {
            CurrentTokenBalance = newBalance;
            if (CurrentPlayer != null)
            {
                CurrentPlayer.TokenBalance = newBalance;
                DataManager?.SavePlayerTokenBalance(CurrentPlayer.UserId, newBalance);
            }
        }

        public void AddTokens(int amount) => TokenSystem?.AddTokens(amount, "Manual add");

        public bool SpendTokens(int amount) => TokenSystem?.SpendTokens(amount, "Manual spend") ?? false;

        public void SaveTeamData(string teamName, string country, string city, string color, int badgeIndex)
        {
            if (CurrentPlayer == null) return;
            CurrentPlayer.Country = country;
            CurrentPlayer.City = city;
            if (CurrentTeam == null)
            {
                CurrentTeam = new Player.TeamData
                {
                    TeamId = CurrentPlayer.TeamId,
                    OwnerId = CurrentPlayer.UserId,
                };
            }
            CurrentTeam.TeamName = teamName;
            CurrentTeam.Country = country;
            CurrentTeam.City = city;
            CurrentTeam.KitPrimaryColor = color;
            // Persist both PlayerData (country/city) and TeamData (name/kit)
            DataManager?.SavePlayerDataDirect(CurrentPlayer);
            DataManager?.SaveTeamDataDirect(CurrentTeam);
        }

        public void UpdateTeamName(string teamName)
        {
            if (CurrentTeam != null)
            {
                CurrentTeam.TeamName = teamName;
                DataManager?.SaveTeamDataDirect(CurrentTeam);
            }
        }

        public void SetState(GameState newState)
        {
            _currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        public void GoToMainMenu() => SetState(GameState.MainMenu);
        public void GoToMatch() => SetState(GameState.Match);
        public void GoToMarket() => SetState(GameState.Market);
        public void GoToRankings() => SetState(GameState.Rankings);
        public void GoToSettings() => SetState(GameState.Settings);
        public void GoToLogin() => SetState(GameState.Login);
        public void GoToTeamSelection() => SetState(GameState.TeamSelection);

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            UnregisterTokenListener();
            if (IsFirebaseReady && FirebaseAuth.DefaultInstance != null)
                FirebaseAuth.DefaultInstance.StateChanged -= OnAuthStateChanged;
        }
    }
}
