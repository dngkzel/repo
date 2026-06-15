using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FootballGame.Core
{
    /// <summary>
    /// Manages scene transitions for the game. Named GameSceneManager to avoid
    /// collisions with Unity's built-in SceneManager class.
    ///
    /// Scenes must be added to the Build Settings in the same order as the
    /// <see cref="SceneName"/> enum values, or the string-based overload must
    /// be used so names are matched by build-settings name strings.
    /// </summary>
    public class GameSceneManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Scene name enum
        // -------------------------------------------------------------------------

        /// <summary>
        /// All scenes in the project.  The string value of each entry must match
        /// the scene asset name exactly (e.g. the file "Loading.unity").
        /// </summary>
        public enum SceneName
        {
            Loading,
            Authentication,
            Registration,
            MainMenu,
            Match,
            Market,
            Rankings,
            Settings,
            DailyReward
        }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired just before a scene load begins. Argument is the target scene.</summary>
        public static event Action<SceneName> OnSceneLoadStarted;

        /// <summary>
        /// Fired each frame during async loading with progress in [0, 1].
        /// Useful for updating progress bars.
        /// </summary>
        public static event Action<float> OnSceneLoadProgress;

        /// <summary>Fired after the new scene is fully active. Argument is the loaded scene.</summary>
        public static event Action<SceneName> OnSceneLoadCompleted;

        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        private static GameSceneManager _instance;

        public static GameSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameSceneManager");
                    _instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // -------------------------------------------------------------------------
        // Inspector-assignable loading overlay
        // -------------------------------------------------------------------------

        [Header("Loading Overlay")]
        [Tooltip("Optional CanvasGroup on a loading overlay UI. When assigned it will " +
                 "be faded in/out during transitions. The GameObject must be in a scene " +
                 "that is marked DontDestroyOnLoad, or be a child of this GameObject.")]
        [SerializeField] private CanvasGroup _loadingOverlay;

        [Tooltip("Seconds for the fade-in and fade-out of the loading overlay.")]
        [SerializeField] private float _fadeDuration = 0.25f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private SceneName _currentScene = SceneName.Loading;
        private bool _isLoading = false;

        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------

        /// <summary>The scene that is currently active.</summary>
        public SceneName CurrentScene => _currentScene;

        /// <summary>True while an async scene load is in progress.</summary>
        public bool IsLoading => _isLoading;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Sync _currentScene to whatever is active in the editor / at startup.
            SyncCurrentSceneFromActive();

            // Listen for Unity's own scene-loaded event so _currentScene stays in sync
            // even if a scene is loaded without going through GameSceneManager.
            SceneManager.sceneLoaded += OnUnitySceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnUnitySceneLoaded;
        }

        // -------------------------------------------------------------------------
        // Public API — scene loading
        // -------------------------------------------------------------------------

        /// <summary>
        /// Asynchronously loads the specified scene with an optional loading overlay.
        /// The call is ignored (with a warning) if a load is already in progress.
        /// </summary>
        /// <param name="scene">Target scene.</param>
        /// <param name="showLoadingOverlay">
        /// When true the loading overlay (if assigned) fades in before loading starts
        /// and fades out once the scene is ready.
        /// </param>
        public void LoadScene(SceneName scene, bool showLoadingOverlay = true)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[GameSceneManager] Ignoring LoadScene({scene}) — a load is already in progress.");
                return;
            }

            StartCoroutine(LoadSceneAsync(scene, showLoadingOverlay));
        }

        /// <summary>
        /// Reloads the currently active scene.
        /// </summary>
        /// <param name="showLoadingOverlay">Whether to show the loading overlay.</param>
        public void ReloadCurrentScene(bool showLoadingOverlay = true)
        {
            LoadScene(_currentScene, showLoadingOverlay);
        }

        // String overload for convenience
        public void LoadScene(string sceneName, bool showLoadingOverlay = true)
        {
            if (Enum.TryParse<SceneName>(sceneName, ignoreCase: true, out SceneName result))
                LoadScene(result, showLoadingOverlay);
            else
                Debug.LogError($"[GameSceneManager] Unknown scene name: {sceneName}");
        }

        // -------------------------------------------------------------------------
        // Internal coroutines
        // -------------------------------------------------------------------------

        private IEnumerator LoadSceneAsync(SceneName scene, bool showLoadingOverlay)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(scene);

            // --- Fade in loading overlay ---
            if (showLoadingOverlay && _loadingOverlay != null)
                yield return StartCoroutine(FadeOverlay(0f, 1f, _fadeDuration));

            // --- Begin async load ---
            string sceneName = scene.ToString();
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (asyncOp == null)
            {
                Debug.LogError($"[GameSceneManager] Failed to start async load for scene '{sceneName}'. " +
                               "Make sure it is added to Build Settings.");
                _isLoading = false;
                yield break;
            }

            // Prevent Unity from activating the scene automatically so we can
            // control the exact moment it becomes active (after any final prep).
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                // Unity reports progress up to 0.9 while loading, then jumps to 1.0
                // once allowSceneActivation is true and the scene activates.
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(progress);

                if (asyncOp.progress >= 0.9f)
                {
                    // Scene data is fully loaded; allow it to activate.
                    OnSceneLoadProgress?.Invoke(1f);
                    asyncOp.allowSceneActivation = true;
                }

                yield return null;
            }

            // _currentScene is updated by OnUnitySceneLoaded, but set it here too
            // for immediate access after yield.
            _currentScene = scene;

            // --- Fade out loading overlay ---
            if (showLoadingOverlay && _loadingOverlay != null)
                yield return StartCoroutine(FadeOverlay(1f, 0f, _fadeDuration));

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(scene);
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (_loadingOverlay == null) yield break;

            _loadingOverlay.gameObject.SetActive(true);
            _loadingOverlay.alpha = from;
            _loadingOverlay.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _loadingOverlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _loadingOverlay.alpha = to;

            // Hide the overlay when fully transparent so it doesn't block input.
            if (to <= 0f)
            {
                _loadingOverlay.blocksRaycasts = false;
                _loadingOverlay.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------------------------

        private void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (TryParseSceneName(scene.name, out SceneName parsed))
                _currentScene = parsed;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void SyncCurrentSceneFromActive()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (TryParseSceneName(activeSceneName, out SceneName parsed))
                _currentScene = parsed;
        }

        /// <summary>
        /// Attempts to convert a Unity scene name string to a <see cref="SceneName"/> value.
        /// Returns false if no match is found.
        /// </summary>
        private bool TryParseSceneName(string name, out SceneName result)
        {
            return Enum.TryParse<SceneName>(name, ignoreCase: true, out result);
        }
    }
}
