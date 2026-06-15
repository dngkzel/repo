using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Match;
using FootballGame.Player;
using FootballGame.Core;

namespace FootballGame.UI
{
    /// <summary>
    /// Drives all in-match HUD elements: scoreboard, time, possession, cards,
    /// event log, popup notifications, substitution panel, and simulation speed.
    /// </summary>
    public class MatchUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Scoreboard & Match Info
        // ─────────────────────────────────────────────────────────────────────

        [Header("Scoreboard")]
        [SerializeField] private TextMeshProUGUI homeScoreText;
        [SerializeField] private TextMeshProUGUI awayScoreText;
        [SerializeField] private TextMeshProUGUI matchTimeText;
        [SerializeField] private TextMeshProUGUI homeTeamName;
        [SerializeField] private TextMeshProUGUI awayTeamName;

        [Header("Cards")]
        [SerializeField] private TextMeshProUGUI homeYellowCards;
        [SerializeField] private TextMeshProUGUI homeRedCards;
        [SerializeField] private TextMeshProUGUI awayYellowCards;
        [SerializeField] private TextMeshProUGUI awayRedCards;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI possessionText;
        [SerializeField] private TextMeshProUGUI shotsText;
        [SerializeField] private Slider possessionSlider;

        // ─────────────────────────────────────────────────────────────────────
        // Event Log
        // ─────────────────────────────────────────────────────────────────────

        [Header("Event Log")]
        [SerializeField] private Transform eventLogContainer;
        [SerializeField] private GameObject eventLogItemPrefab;

        // ─────────────────────────────────────────────────────────────────────
        // Popup Panels
        // ─────────────────────────────────────────────────────────────────────

        [Header("Popups")]
        [SerializeField] private GameObject goalPopup;
        [SerializeField] private GameObject cardPopup;
        [SerializeField] private GameObject substitutionPopup;
        [SerializeField] private TextMeshProUGUI goalPopupText;
        [SerializeField] private TextMeshProUGUI cardPopupText;

        // ─────────────────────────────────────────────────────────────────────
        // Controls
        // ─────────────────────────────────────────────────────────────────────

        [Header("Controls")]
        [SerializeField] private Button substituteButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private TextMeshProUGUI speedText;

        // ─────────────────────────────────────────────────────────────────────
        // Substitution Panel
        // ─────────────────────────────────────────────────────────────────────

        [Header("Substitution Panel")]
        [SerializeField] private GameObject substitutionPanel;

        // ─────────────────────────────────────────────────────────────────────
        // Popup Timing
        // ─────────────────────────────────────────────────────────────────────

        [Header("Popup Settings")]
        [SerializeField] private float goalPopupDuration   = 3.5f;
        [SerializeField] private float cardPopupDuration   = 2.5f;
        [SerializeField] private float subPopupDuration    = 2.0f;
        [SerializeField] private float popupFadeDuration   = 0.25f;

        // ─────────────────────────────────────────────────────────────────────
        // Speed cycling
        // ─────────────────────────────────────────────────────────────────────

        private static readonly float[] SpeedSteps = { 1f, 2f, 4f };
        private int   _speedIndex     = 0;
        private float _currentSpeed   = 1f;

        // ─────────────────────────────────────────────────────────────────────
        // Card counters
        // ─────────────────────────────────────────────────────────────────────

        private int _homeYellow;
        private int _homeRed;
        private int _awayYellow;
        private int _awayRed;

        // ─────────────────────────────────────────────────────────────────────
        // Shot counters
        // ─────────────────────────────────────────────────────────────────────

        private int _homeShots;
        private int _awayShots;

        // ─────────────────────────────────────────────────────────────────────
        // Coroutine guards – prevent simultaneous popup overlaps
        // ─────────────────────────────────────────────────────────────────────

        private Coroutine _goalPopupCoroutine;
        private Coroutine _cardPopupCoroutine;
        private Coroutine _subPopupCoroutine;

        // ─────────────────────────────────────────────────────────────────────
        // Cached home team name for card/sub routing
        // ─────────────────────────────────────────────────────────────────────

        private string _homeTeam = string.Empty;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (substituteButton != null)
                substituteButton.onClick.AddListener(OpenSubstitutionPanel);

            if (speedButton != null)
                speedButton.onClick.AddListener(CycleMatchSpeed);

            // Ensure popups start hidden.
            SafeSetActive(goalPopup,         false);
            SafeSetActive(cardPopup,         false);
            SafeSetActive(substitutionPopup, false);
            SafeSetActive(substitutionPanel, false);
        }

        private void Start()
        {
            UpdateSpeedLabel();
            RefreshCardDisplays();
            RefreshShotsDisplay();
        }

        private void OnDestroy()
        {
            if (substituteButton != null)
                substituteButton.onClick.RemoveListener(OpenSubstitutionPanel);

            if (speedButton != null)
                speedButton.onClick.RemoveListener(CycleMatchSpeed);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Score & Time
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Updates both score labels.</summary>
        public void UpdateScore(int home, int away)
        {
            if (homeScoreText != null) homeScoreText.text = home.ToString();
            if (awayScoreText != null) awayScoreText.text = away.ToString();
        }

        /// <summary>Updates the match clock label. Clamps to 0-120 for extra-time.</summary>
        public void UpdateTime(int minute)
        {
            if (matchTimeText == null) return;
            minute = Mathf.Clamp(minute, 0, 120);
            matchTimeText.text = minute >= 90
                ? $"90+{minute - 90}'"
                : $"{minute}'";
        }

        /// <summary>Sets both team name labels and caches the home team name.</summary>
        public void SetTeamNames(string home, string away)
        {
            _homeTeam = home ?? string.Empty;
            if (homeTeamName != null) homeTeamName.text = home;
            if (awayTeamName != null) awayTeamName.text = away;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Possession
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the possession percentage and slider.
        /// <paramref name="homePoss"/> should be in [0, 1].
        /// </summary>
        public void UpdatePossession(float homePoss)
        {
            homePoss = Mathf.Clamp01(homePoss);
            float awayPoss = 1f - homePoss;

            if (possessionText != null)
                possessionText.text = $"{Mathf.RoundToInt(homePoss * 100)}% - {Mathf.RoundToInt(awayPoss * 100)}%";

            if (possessionSlider != null)
            {
                possessionSlider.minValue = 0f;
                possessionSlider.maxValue = 1f;
                possessionSlider.value    = homePoss;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Match Speed
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cycles through 1x / 2x / 4x and applies Time.timeScale.</summary>
        private void CycleMatchSpeed()
        {
            _speedIndex  = (_speedIndex + 1) % SpeedSteps.Length;
            _currentSpeed = SpeedSteps[_speedIndex];
            SetMatchSpeed(_currentSpeed);
        }

        /// <summary>
        /// Applies the requested speed multiplier via Time.timeScale and refreshes the label.
        /// External callers can pass 1f, 2f, or 4f directly.
        /// </summary>
        public void SetMatchSpeed(float speed)
        {
            _currentSpeed = speed;
            Time.timeScale = speed;
            UpdateSpeedLabel();
        }

        private void UpdateSpeedLabel()
        {
            if (speedText == null) return;
            // Format as "1x", "2x", "4x"
            speedText.text = $"{_currentSpeed:0.##}x";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Substitution Panel
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Opens the substitution panel and resets any previous content.</summary>
        public void OpenSubstitutionPanel()
        {
            if (substitutionPanel == null)
            {
                Debug.LogWarning("[MatchUI] SubstitutionPanel reference not set.");
                return;
            }

            // Clear stale entries before populating.
            ClearChildren(substitutionPanel.transform);
            substitutionPanel.SetActive(true);
        }

        /// <summary>
        /// Closes the substitution panel. Assign to a "Close" button inside the panel.
        /// </summary>
        public void CloseSubstitutionPanel()
        {
            SafeSetActive(substitutionPanel, false);
        }

        /// <summary>
        /// Populates the substitution panel with the bench players from <paramref name="team"/>.
        /// Creates a simple row for each bench player showing name, position, and overall.
        /// </summary>
        public void PopulateSubstitutionPanel(TeamData team)
        {
            if (substitutionPanel == null || eventLogItemPrefab == null) return;
            if (team == null) return;

            ClearChildren(substitutionPanel.transform);

            foreach (PlayerData player in team.bench)
            {
                if (player == null) continue;

                GameObject row = Instantiate(eventLogItemPrefab, substitutionPanel.transform);

                TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{player.position}  {player.name}  OVR {player.overall}";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Log
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates a log row from <see cref="eventLogItemPrefab"/>, sets its text,
        /// and parents it under <see cref="eventLogContainer"/> so the ScrollView scrolls it.
        /// </summary>
        public void AddEventToLog(MatchEvent evt)
        {
            if (eventLogContainer == null || eventLogItemPrefab == null || evt == null) return;

            GameObject item = Instantiate(eventLogItemPrefab, eventLogContainer);

            TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = FormatEventForLog(evt);

            // Keep the newest entry visible by moving the new item to the bottom.
            item.transform.SetAsLastSibling();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event Routing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Central dispatch: routes a <see cref="MatchEvent"/> to the appropriate
        /// popup and appends it to the event log.
        /// </summary>
        public void OnEventReceived(MatchEvent evt)
        {
            if (evt == null) return;

            switch (evt)
            {
                case GoalEvent goal:
                    UpdateShotsOnGoal(goal);
                    if (_goalPopupCoroutine != null) StopCoroutine(_goalPopupCoroutine);
                    _goalPopupCoroutine = StartCoroutine(ShowGoalPopup(goal));
                    break;

                case CardEvent card:
                    AccumulateCard(card);
                    if (_cardPopupCoroutine != null) StopCoroutine(_cardPopupCoroutine);
                    _cardPopupCoroutine = StartCoroutine(ShowCardPopup(card));
                    break;

                case SubstitutionEvent sub:
                    if (_subPopupCoroutine != null) StopCoroutine(_subPopupCoroutine);
                    _subPopupCoroutine = StartCoroutine(ShowSubstitutionPopup(sub));
                    break;

                case ShotEvent shot:
                    AccumulateShot(shot);
                    break;
            }

            AddEventToLog(evt);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Goal Popup
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Shows the goal popup with animated scale-in, hold, and fade-out.</summary>
        public IEnumerator ShowGoalPopup(GoalEvent evt)
        {
            if (goalPopup == null) yield break;

            string text = evt.isOwnGoal
                ? $"OWN GOAL!\n{evt.scorer?.name ?? "Unknown"}"
                : $"GOAL!\n{evt.scorer?.name ?? "Unknown"}{(evt.assister != null ? $"\nAssist: {evt.assister.name}" : string.Empty)}";

            if (goalPopupText != null)
                goalPopupText.text = text;

            yield return AnimatePopup(goalPopup, goalPopupDuration);
            _goalPopupCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Card Popup
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Shows the card popup coloured yellow or red.</summary>
        public IEnumerator ShowCardPopup(CardEvent evt)
        {
            if (cardPopup == null) yield break;

            string cardLabel = evt.type == CardType.Yellow ? "YELLOW CARD" : "RED CARD";
            if (cardPopupText != null)
                cardPopupText.text = $"{cardLabel}\n{evt.player?.name ?? "Unknown"}";

            // Tint the popup to match the card colour.
            Image popupImage = cardPopup.GetComponent<Image>();
            if (popupImage != null)
                popupImage.color = evt.type == CardType.Yellow
                    ? new Color(1f, 0.85f, 0f)
                    : new Color(0.85f, 0.1f, 0.1f);

            yield return AnimatePopup(cardPopup, cardPopupDuration);
            _cardPopupCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Substitution Popup
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Shows a brief substitution notification banner.</summary>
        public IEnumerator ShowSubstitutionPopup(SubstitutionEvent evt)
        {
            if (substitutionPopup == null) yield break;

            // Reuse the card popup text if a dedicated label isn't wired up.
            TextMeshProUGUI label = substitutionPopup.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"SUBSTITUTION\n{evt.playerIn?.name ?? "?"} IN  |  {evt.playerOut?.name ?? "?"} OUT";

            yield return AnimatePopup(substitutionPopup, subPopupDuration);
            _subPopupCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Generic Animated Popup
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows <paramref name="popup"/> with a scale-in animation, holds for
        /// <paramref name="duration"/> seconds, then fades it out and hides it.
        /// Safe to call on any GameObject that has a CanvasGroup or can simply
        /// be toggled via SetActive.
        /// </summary>
        public IEnumerator AnimatePopup(GameObject popup, float duration)
        {
            if (popup == null) yield break;

            popup.SetActive(true);

            // ── Scale in ─────────────────────────────────────────────────────
            RectTransform rt = popup.GetComponent<RectTransform>();
            CanvasGroup   cg = popup.GetComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < popupFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popupFadeDuration);
                float eased = t * t * (3f - 2f * t);          // smooth-step

                if (rt != null) rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, eased);
                if (cg != null) cg.alpha = eased;

                yield return null;
            }

            if (rt != null) rt.localScale = Vector3.one;
            if (cg != null) cg.alpha = 1f;

            // ── Hold ─────────────────────────────────────────────────────────
            float holdTimer = 0f;
            while (holdTimer < duration)
            {
                holdTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            // ── Fade / scale out ─────────────────────────────────────────────
            elapsed = 0f;
            while (elapsed < popupFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popupFadeDuration);
                float eased = 1f - t * t;

                if (rt != null) rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                if (cg != null) cg.alpha = eased;

                yield return null;
            }

            popup.SetActive(false);

            if (rt != null) rt.localScale = Vector3.one;
            if (cg != null) cg.alpha = 1f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Card Accumulation & Display
        // ─────────────────────────────────────────────────────────────────────

        private void AccumulateCard(CardEvent card)
        {
            bool isHome = card.team == _homeTeam;

            if (card.type == CardType.Yellow)
            {
                if (isHome) _homeYellow++; else _awayYellow++;
            }
            else
            {
                if (isHome) _homeRed++; else _awayRed++;
            }

            RefreshCardDisplays();
        }

        private void RefreshCardDisplays()
        {
            if (homeYellowCards != null) homeYellowCards.text = _homeYellow.ToString();
            if (homeRedCards    != null) homeRedCards.text    = _homeRed.ToString();
            if (awayYellowCards != null) awayYellowCards.text = _awayYellow.ToString();
            if (awayRedCards    != null) awayRedCards.text    = _awayRed.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shot Tracking
        // ─────────────────────────────────────────────────────────────────────

        private void AccumulateShot(ShotEvent shot)
        {
            bool isHome = shot.team == _homeTeam;
            if (isHome) _homeShots++; else _awayShots++;
            RefreshShotsDisplay();
        }

        private void UpdateShotsOnGoal(GoalEvent goal)
        {
            // A goal is always a shot on target.
            bool isHome = goal.team == _homeTeam;
            if (isHome) _homeShots++; else _awayShots++;
            RefreshShotsDisplay();
        }

        private void RefreshShotsDisplay()
        {
            if (shotsText != null)
                shotsText.text = $"{_homeShots} - {_awayShots}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Converts a MatchEvent to a short log-friendly string.</summary>
        private static string FormatEventForLog(MatchEvent evt)
        {
            string minute = $"{evt.minute}'";

            switch (evt)
            {
                case GoalEvent g:
                    string og = g.isOwnGoal ? " (OG)" : string.Empty;
                    return $"{minute}  GOAL  {g.scorer?.name ?? "?"}{og} ({g.team})";

                case CardEvent c:
                    string cardSymbol = c.type == CardType.Yellow ? "Y" : "R";
                    return $"{minute}  [{cardSymbol}]  {c.player?.name ?? "?"} ({c.team})";

                case SubstitutionEvent s:
                    return $"{minute}  SUB  {s.playerIn?.name ?? "?"} ↑  {s.playerOut?.name ?? "?"} ↓  ({s.team})";

                case FoulEvent f:
                    return $"{minute}  FOUL  {f.fouler?.name ?? "?"} on {f.fouled?.name ?? "?"}";

                case ShotEvent sh:
                    string onTarget = sh.onTarget ? "on target" : "off target";
                    return $"{minute}  SHOT  {sh.shooter?.name ?? "?"}  ({onTarget})";

                case PenaltyEvent p:
                    string result = p.scored ? "scored" : "missed";
                    return $"{minute}  PENALTY  {p.taker?.name ?? "?"}  {result}  ({p.team})";

                case CornerEvent corner:
                    return $"{minute}  CORNER  ({corner.team})";

                case OffsidesEvent offside:
                    return $"{minute}  OFFSIDE  ({offside.team})";

                default:
                    return $"{minute}  {evt.description ?? "Event"}";
            }
        }

        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
