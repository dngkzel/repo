using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Ranking;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class RankingUI : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button _worldTabBtn;
        [SerializeField] private Button _countryTabBtn;
        [SerializeField] private Button _cityTabBtn;

        [Header("Tab Indicators")]
        [SerializeField] private GameObject _worldTabIndicator;
        [SerializeField] private GameObject _countryTabIndicator;
        [SerializeField] private GameObject _cityTabIndicator;

        [Header("List")]
        [SerializeField] private Transform _rankingListParent;
        [SerializeField] private GameObject _rankingRowPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("My Rank")]
        [SerializeField] private TMP_Text _myRankText;
        [SerializeField] private TMP_Text _myPointsText;
        [SerializeField] private TMP_Text _myTeamText;

        [Header("Filter")]
        [SerializeField] private TMP_Text _filterLabel;

        [Header("Loading")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _emptyPanel;

        private RankingScope _currentScope = RankingScope.World;
        private List<GameObject> _rowObjects = new List<GameObject>();

        private void Start()
        {
            RankingSystem.Instance.OnRankingLoaded += OnRankingLoaded;

            _worldTabBtn?.onClick.AddListener(() => SwitchTab(RankingScope.World));
            _countryTabBtn?.onClick.AddListener(() => SwitchTab(RankingScope.Country));
            _cityTabBtn?.onClick.AddListener(() => SwitchTab(RankingScope.City));

            SwitchTab(RankingScope.World);
        }

        private void OnDestroy()
        {
            if (RankingSystem.Instance != null)
                RankingSystem.Instance.OnRankingLoaded -= OnRankingLoaded;
        }

        private void SwitchTab(RankingScope scope)
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            _currentScope = scope;

            _worldTabIndicator?.SetActive(scope == RankingScope.World);
            _countryTabIndicator?.SetActive(scope == RankingScope.Country);
            _cityTabIndicator?.SetActive(scope == RankingScope.City);

            if (_loadingPanel != null) _loadingPanel.SetActive(true);

            string userId = GameManager.Instance?.CurrentUserId;
            var userData = GameManager.Instance?.CurrentUserData;
            string filterValue = scope == RankingScope.Country ? userData?.Country
                               : scope == RankingScope.City ? userData?.City
                               : null;

            if (_filterLabel != null)
            {
                _filterLabel.text = scope == RankingScope.World
                    ? LocalizationManager.Instance?.Get("world") ?? "World"
                    : filterValue ?? "";
            }

            RankingSystem.Instance?.LoadRankings(scope, filterValue);
        }

        private void OnRankingLoaded(List<RankingEntry> entries, RankingScope scope)
        {
            if (scope != _currentScope) return;

            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            if (_emptyPanel != null) _emptyPanel.SetActive(entries.Count == 0);

            ClearRows();

            foreach (var entry in entries)
            {
                var row = Instantiate(_rankingRowPrefab, _rankingListParent);
                PopulateRow(row, entry);
                _rowObjects.Add(row);
            }

            string userId = GameManager.Instance?.CurrentUserId;
            int myRank = RankingSystem.Instance.GetMyRank(userId, scope);
            var myEntry = entries.Find(e => e.UserId == userId);

            if (_myRankText != null) _myRankText.text = myRank > 0 ? $"#{myRank}" : "-";
            if (_myPointsText != null) _myPointsText.text = myEntry != null ? $"{myEntry.TotalPoints} pts" : "-";
            if (_myTeamText != null) _myTeamText.text = myEntry?.TeamName ?? GameManager.Instance?.CurrentUserData?.TeamName ?? "";
        }

        private void PopulateRow(GameObject row, RankingEntry entry)
        {
            row.transform.Find("RankText")?.GetComponent<TMP_Text>()?.SetText($"#{entry.Rank}");
            row.transform.Find("TeamNameText")?.GetComponent<TMP_Text>()?.SetText(entry.TeamName);
            row.transform.Find("PlayerNameText")?.GetComponent<TMP_Text>()?.SetText(entry.DisplayName);
            row.transform.Find("PointsText")?.GetComponent<TMP_Text>()?.SetText($"{entry.TotalPoints}");
            row.transform.Find("WDLText")?.GetComponent<TMP_Text>()?.SetText($"{entry.Wins}/{entry.Draws}/{entry.Losses}");

            // Highlight current user row
            string userId = GameManager.Instance?.CurrentUserId;
            if (entry.UserId == userId)
            {
                var highlight = row.GetComponent<Image>();
                if (highlight != null) highlight.color = new Color(1f, 0.9f, 0.2f, 0.3f);
            }
        }

        private void ClearRows()
        {
            foreach (var obj in _rowObjects)
                if (obj != null) Destroy(obj);
            _rowObjects.Clear();
        }
    }
}
