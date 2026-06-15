using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Ranking;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class RankingUI : MonoBehaviour
    {
        [Header("Scope Tabs")]
        public Button btnWorld;
        public Button btnCountry;
        public Button btnCity;

        [Header("My Rank")]
        public TextMeshProUGUI txtMyRank;

        [Header("List")]
        public Transform listContainer;
        public GameObject rankRowPrefab;
        public ScrollRect listScroll;

        [Header("Navigation")]
        public Button btnBack;

        private RankingScope _currentScope = RankingScope.World;
        private RankingSystem _sys;

        private void Start()
        {
            _sys = RankingSystem.Instance;
            if (_sys != null) _sys.OnRankingLoaded += OnRankingLoaded;

            btnWorld?.onClick.AddListener(()   => LoadScope(RankingScope.World));
            btnCountry?.onClick.AddListener(() => LoadScope(RankingScope.Country));
            btnCity?.onClick.AddListener(()    => LoadScope(RankingScope.City));
            btnBack?.onClick.AddListener(OnBack);

            LoadScope(RankingScope.World);
        }

        private void OnDestroy()
        {
            if (_sys != null) _sys.OnRankingLoaded -= OnRankingLoaded;
        }

        private void LoadScope(RankingScope scope)
        {
            _currentScope = scope;
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);

            string uid   = GameManager.Instance?.CurrentUserId ?? "";
            var pd       = GameManager.Instance?.CurrentUserData;
            string filter = scope == RankingScope.Country ? pd?.Country :
                            scope == RankingScope.City    ? pd?.City    : null;

            _sys?.LoadRankings(scope, filter);

            // Show cached while loading
            ShowEntries(_sys?.GetCached(scope) ?? new List<RankEntry>());
            UpdateMyRank();
        }

        private void OnRankingLoaded(List<RankEntry> entries, RankingScope scope)
        {
            if (scope != _currentScope) return;
            ShowEntries(entries);
            UpdateMyRank();
        }

        private void ShowEntries(List<RankEntry> entries)
        {
            if (listContainer == null) return;
            foreach (Transform c in listContainer) Destroy(c.gameObject);

            foreach (var e in entries)
            {
                if (rankRowPrefab == null) continue;
                var go  = Instantiate(rankRowPrefab, listContainer);
                var txts = go.GetComponentsInChildren<TextMeshProUGUI>();
                if (txts.Length > 0) txts[0].text = $"#{e.Rank}";
                if (txts.Length > 1) txts[1].text = e.DisplayName;
                if (txts.Length > 2) txts[2].text = e.TeamName;
                if (txts.Length > 3) txts[3].text = e.Points.ToString();
                if (txts.Length > 4) txts[4].text = $"{e.Wins}W {e.Draws}D {e.Losses}L";
                if (txts.Length > 5) txts[5].text = $"{e.GoalsFor}:{e.GoalsAgainst}";

                // Highlight current user row
                string uid = GameManager.Instance?.CurrentUserId ?? "";
                if (e.UserId == uid)
                {
                    var img = go.GetComponent<Image>();
                    if (img) img.color = new Color(1f, 0.85f, 0.2f, 0.3f);
                }
            }
        }

        private void UpdateMyRank()
        {
            if (txtMyRank == null) return;
            string uid  = GameManager.Instance?.CurrentUserId ?? "";
            int rank    = _sys?.GetMyRank(uid, _currentScope) ?? -1;
            txtMyRank.text = rank > 0 ? $"#{rank}" : "-";
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }
}
