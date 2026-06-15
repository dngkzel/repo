using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FootballGame.Core
{
    public enum SceneName { Loading, Authentication, Registration, MainMenu, Match, Market, Rankings, Settings, DailyReward }

    public class GameSceneManager : MonoBehaviour
    {

        public static event Action<SceneName> OnSceneLoadStarted;
        public static event Action<float> OnSceneLoadProgress;
        public static event Action<SceneName> OnSceneLoadCompleted;

        private static GameSceneManager _instance;
        public static GameSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameSceneManager");
                    _instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [SerializeField] private CanvasGroup _loadingOverlay;
        [SerializeField] private float _fadeDuration = 0.25f;

        private SceneName _currentScene = SceneName.Loading;
        private bool _isLoading;

        public SceneName CurrentScene => _currentScene;
        public bool IsLoading => _isLoading;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnUnitySceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnUnitySceneLoaded;

        public void LoadScene(SceneName scene, bool overlay = true)
        {
            if (_isLoading) return;
            StartCoroutine(LoadAsync(scene, overlay));
        }

        public void LoadScene(string sceneName, bool overlay = true)
        {
            if (Enum.TryParse<SceneName>(sceneName, true, out var result))
                LoadScene(result, overlay);
            else
                Debug.LogError($"[SceneManager] Unknown scene: {sceneName}");
        }

        public void ReloadCurrentScene(bool overlay = true) => LoadScene(_currentScene, overlay);

        private IEnumerator LoadAsync(SceneName scene, bool overlay)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(scene);

            if (overlay && _loadingOverlay != null)
                yield return StartCoroutine(Fade(0f, 1f));

            var op = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single);
            if (op == null) { _isLoading = false; yield break; }
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                float p = Mathf.Clamp01(op.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(p);
                if (op.progress >= 0.9f) { OnSceneLoadProgress?.Invoke(1f); op.allowSceneActivation = true; }
                yield return null;
            }

            _currentScene = scene;
            if (overlay && _loadingOverlay != null) yield return StartCoroutine(Fade(1f, 0f));
            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(scene);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_loadingOverlay == null) yield break;
            _loadingOverlay.gameObject.SetActive(true);
            _loadingOverlay.alpha = from;
            _loadingOverlay.blocksRaycasts = true;
            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _loadingOverlay.alpha = Mathf.Lerp(from, to, t / _fadeDuration);
                yield return null;
            }
            _loadingOverlay.alpha = to;
            if (to <= 0f) { _loadingOverlay.blocksRaycasts = false; _loadingOverlay.gameObject.SetActive(false); }
        }

        private void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Enum.TryParse<SceneName>(scene.name, true, out var parsed))
                _currentScene = parsed;
        }
    }
}
