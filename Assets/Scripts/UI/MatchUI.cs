using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Match;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class MatchUI : MonoBehaviour
    {
        [Header("Score / Minute")]
        public TextMeshProUGUI txtHomeTeam;
        public TextMeshProUGUI txtAwayTeam;
        public TextMeshProUGUI txtScore;
        public TextMeshProUGUI txtMinute;

        [Header("Commentary Feed")]
        public Transform commentaryContainer;
        public GameObject commentaryItemPrefab;
        public ScrollRect commentaryScroll;

        [Header("Match Controls")]
        public Button btnPause;
        public Button btnResume;
        public Button btnSpeed1;
        public Button btnSpeed2;
        public Button btnSpeed4;
        public TextMeshProUGUI txtPauseLabel;

        [Header("Substitution Panel")]
        public GameObject substitutionPanel;
        public Button btnOpenSubs;
        public Button btnCloseSubs;
        public Transform onFieldContainer;
        public Transform benchContainer;
        public GameObject playerRowPrefab;
        public TextMeshProUGUI txtSubsRemaining;

        [Header("Result Panel")]
        public GameObject resultPanel;
        public TextMeshProUGUI txtResultScore;
        public TextMeshProUGUI txtResultDescription;
        public Button btnBackToMenu;

        private MatchController _ctrl;
        private MatchPlayerData _selectedOnField;
        private bool _isPaused;

        private void Start()
        {
            _ctrl = MatchController.Instance;
            if (_ctrl == null) return;

            _ctrl.OnMatchStarted    += OnMatchReady;
            _ctrl.OnGoalScored      += OnGoal;
            _ctrl.OnMatchEventFired += OnEvent;
            _ctrl.OnMinuteUpdated   += OnMinute;
            _ctrl.OnHalftime        += OnHalftime;
            _ctrl.OnFullTime        += OnFullTime;

            // If match was already initialized before this Start ran
            if (_ctrl.State != null) RefreshTeamLabels();

            btnPause?.onClick.AddListener(TogglePause);
            btnSpeed1?.onClick.AddListener(() => SetSpeed(1f));
            btnSpeed2?.onClick.AddListener(() => SetSpeed(2f));
            btnSpeed4?.onClick.AddListener(() => SetSpeed(4f));
            btnOpenSubs?.onClick.AddListener(OpenSubPanel);
            btnCloseSubs?.onClick.AddListener(() => substitutionPanel?.SetActive(false));
            btnBackToMenu?.onClick.AddListener(BackToMenu);

            resultPanel?.SetActive(false);
            substitutionPanel?.SetActive(false);

            AudioManager.Instance?.PlayMusic(MusicTrack.Match);
        }

        private void OnDestroy()
        {
            if (_ctrl == null) return;
            _ctrl.OnMatchStarted    -= OnMatchReady;
            _ctrl.OnGoalScored      -= OnGoal;
            _ctrl.OnMatchEventFired -= OnEvent;
            _ctrl.OnMinuteUpdated   -= OnMinute;
            _ctrl.OnHalftime        -= OnHalftime;
            _ctrl.OnFullTime        -= OnFullTime;
        }

        private void OnMatchReady()
        {
            RefreshTeamLabels();
            UpdateScore();
            AudioManager.Instance?.PlaySFX(SFX.MatchKickoff);
        }

        private void RefreshTeamLabels()
        {
            var state = _ctrl?.State;
            if (txtHomeTeam) txtHomeTeam.text = state?.HomeTeam?.TeamName ?? "Home";
            if (txtAwayTeam) txtAwayTeam.text = state?.AwayTeam?.TeamName ?? "Away";
        }

        private void OnGoal(LiveMatchEvent evt)
        {
            UpdateScore();
            AddCommentary(evt.Commentary, Color.yellow);
            AudioManager.Instance?.PlaySFX(SFX.Goal);
            AudioManager.Instance?.PlaySFX(SFX.Cheer);
        }

        private void OnEvent(LiveMatchEvent evt)
        {
            Color col = Color.white;
            SFX? sfx = null;
            switch (evt.Type)
            {
                case LiveEventType.YellowCard:   col = new Color(1f, 0.9f, 0f);  sfx = SFX.YellowCard;  break;
                case LiveEventType.RedCard:
                case LiveEventType.SecondYellow: col = new Color(1f, 0.2f, 0.2f); sfx = SFX.RedCard;    break;
                case LiveEventType.HalfTime:     col = new Color(0.6f, 1f, 1f);  sfx = SFX.HalfTime;   break;
                case LiveEventType.FullTime:     col = new Color(0.6f, 1f, 1f);  sfx = SFX.FullTime;    break;
                case LiveEventType.Penalty:      sfx = SFX.Penalty;   break;
                case LiveEventType.Corner:       sfx = SFX.Corner;    break;
                case LiveEventType.Foul:         sfx = SFX.Foul;      break;
                case LiveEventType.Substitution: sfx = SFX.Substitution; break;
            }
            AddCommentary(evt.Commentary, col);
            if (sfx.HasValue) AudioManager.Instance?.PlaySFX(sfx.Value);
        }

        private void OnMinute(int min)
        {
            if (txtMinute) txtMinute.text = $"{min}'";
        }

        private void OnHalftime()
        {
            // Resume is needed so the simulator can continue after its 3-second internal wait
            _ctrl.ResumeMatch();
            AudioManager.Instance?.PlaySFX(SFX.Whistle);
        }

        private void OnFullTime()
        {
            AudioManager.Instance?.PlaySFX(SFX.Whistle);
            var state = _ctrl.State;
            string uid = GameManager.Instance?.CurrentUserId ?? "";
            bool isHome = state.HomeTeam?.OwnerId == uid;
            bool win  = isHome ? state.HomeScore > state.AwayScore : state.AwayScore > state.HomeScore;
            bool draw = state.HomeScore == state.AwayScore;

            if (resultPanel) resultPanel.SetActive(true);
            if (txtResultScore)
                txtResultScore.text = $"{state.HomeTeam?.TeamName} {state.HomeScore} - {state.AwayScore} {state.AwayTeam?.TeamName}";
            if (txtResultDescription)
            {
                var loc = LocalizationManager.Instance;
                txtResultDescription.text = draw ? loc?.Get("result_draw") :
                                            win  ? loc?.Get("result_win")  : loc?.Get("result_loss");
            }
            AudioManager.Instance?.PlayMusic(win ? MusicTrack.Victory : MusicTrack.Defeat);
        }

        private void AddCommentary(string text, Color color)
        {
            if (commentaryContainer == null || commentaryItemPrefab == null || string.IsNullOrEmpty(text)) return;
            var go = Instantiate(commentaryItemPrefab, commentaryContainer);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) { tmp.text = text; tmp.color = color; }
            if (commentaryScroll) Canvas.ForceUpdateCanvases();
        }

        private void UpdateScore()
        {
            var state = _ctrl?.State;
            if (txtScore && state != null)
                txtScore.text = $"{state.HomeScore} - {state.AwayScore}";
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            if (_isPaused) _ctrl?.PauseMatch(); else _ctrl?.ResumeMatch();
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            if (txtPauseLabel)
                txtPauseLabel.text = _isPaused
                    ? LocalizationManager.Instance?.Get("resume") ?? "Resume"
                    : LocalizationManager.Instance?.Get("pause")  ?? "Pause";
        }

        private void SetSpeed(float s)
        {
            MatchSimulator.Instance?.SetSpeed(s);
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private void OpenSubPanel()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            substitutionPanel?.SetActive(true);
            _selectedOnField = null;
            PopulateSubPanel();
        }

        private void PopulateSubPanel()
        {
            if (onFieldContainer == null || benchContainer == null) return;
            foreach (Transform c in onFieldContainer) Destroy(c.gameObject);
            foreach (Transform c in benchContainer)   Destroy(c.gameObject);

            string uid = GameManager.Instance?.CurrentUserId ?? "";
            bool isHome = _ctrl?.State?.HomeTeam?.OwnerId == uid;
            var team = isHome ? _ctrl?.State?.HomeTeam : _ctrl?.State?.AwayTeam;
            if (team == null) return;

            if (txtSubsRemaining)
                txtSubsRemaining.text = $"{_ctrl?.GetSubsRemaining(isHome)}/3 subs";

            foreach (var p in team.OnFieldPlayers ?? new List<MatchPlayerData>())
                AddPlayerRow(onFieldContainer, p, true);

            foreach (var p in team.Bench ?? new List<MatchPlayerData>())
                AddPlayerRow(benchContainer, p, false);
        }

        private void AddPlayerRow(Transform container, MatchPlayerData p, bool isOnField)
        {
            if (playerRowPrefab == null) return;
            var go  = Instantiate(playerRowPrefab, container);
            var txts = go.GetComponentsInChildren<TextMeshProUGUI>();
            if (txts.Length > 0) txts[0].text = p.Name;
            if (txts.Length > 1) txts[1].text = p.Position;
            if (txts.Length > 2) txts[2].text = p.Overall.ToString();

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                if (isOnField)
                    btn.onClick.AddListener(() => { _selectedOnField = p; HighlightRow(go); });
                else
                    btn.onClick.AddListener(() => PerformSub(p));
            }
        }

        private void HighlightRow(GameObject go)
        {
            var img = go.GetComponent<Image>();
            if (img) img.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        }

        private void PerformSub(MatchPlayerData playerIn)
        {
            if (_selectedOnField == null) return;
            string uid = GameManager.Instance?.CurrentUserId ?? "";
            bool isHome = _ctrl?.State?.HomeTeam?.OwnerId == uid;
            var team = isHome ? _ctrl?.State?.HomeTeam : _ctrl?.State?.AwayTeam;
            if (team == null) return;

            bool ok = _ctrl.MakeSubstitution(team, _selectedOnField, playerIn);
            if (ok)
            {
                AudioManager.Instance?.PlaySFX(SFX.Substitution);
                PopulateSubPanel();
            }
        }

        private void BackToMenu()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            MatchSimulator.Instance?.StopSimulation();
            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }
}
