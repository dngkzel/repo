using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class TeamSelectionUI : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private bool _isRegistrationMode = true;

        [Header("Team Name Input")]
        [SerializeField] private TMP_InputField _teamNameInput;
        [SerializeField] private TMP_Text _charCountText;
        [SerializeField] private TMP_Text _validationText;

        [Header("Preset Names")]
        [SerializeField] private Transform _presetNamesParent;
        [SerializeField] private GameObject _presetNameButtonPrefab;

        [Header("Color Picker")]
        [SerializeField] private Button[] _colorButtons;
        [SerializeField] private GameObject _selectedColorIndicator;

        [Header("Badge Selection")]
        [SerializeField] private Button[] _badgeButtons;
        [SerializeField] private Image _selectedBadgePreview;

        [Header("Action Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _confirmButtonText;

        [Header("Country/City")]
        [SerializeField] private TMP_Dropdown _countryDropdown;
        [SerializeField] private TMP_InputField _cityInput;

        private string _selectedColor = "#FF0000";
        private int _selectedBadgeIndex = 0;
        private bool _isChangeMode = false;

        private readonly List<string> _presetNames = new List<string>
        {
            "Thunder FC", "Storm United", "Eagle Warriors", "Iron Lions",
            "Royal Blues", "Golden Stars", "Dragon FC", "Phoenix Rising",
            "Wolves United", "Falcon City", "King's XI", "Serpent Bay"
        };

        private readonly List<string> _countries = new List<string>
        {
            "Turkey", "USA", "Germany", "Spain", "France", "Italy",
            "Brazil", "Argentina", "England", "Portugal", "Netherlands",
            "Mexico", "Japan", "South Korea", "Australia", "Saudi Arabia",
            "UAE", "Qatar", "Egypt", "Nigeria", "Thailand", "Indonesia"
        };

        private void Start()
        {
            SetupPresetNames();
            SetupCountryDropdown();
            SetupColorButtons();
            SetupBadgeButtons();

            _teamNameInput?.onValueChanged.AddListener(OnTeamNameChanged);
            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);

            if (_confirmButtonText != null)
                _confirmButtonText.text = _isRegistrationMode
                    ? LocalizationManager.Instance?.Get("continue") ?? "CONTINUE"
                    : LocalizationManager.Instance?.Get("save") ?? "SAVE";

            if (_cancelButton != null)
                _cancelButton.gameObject.SetActive(!_isRegistrationMode);
        }

        private void SetupPresetNames()
        {
            if (_presetNamesParent == null || _presetNameButtonPrefab == null) return;
            foreach (var name in _presetNames)
            {
                var btn = Instantiate(_presetNameButtonPrefab, _presetNamesParent);
                btn.GetComponentInChildren<TMP_Text>()?.SetText(name);
                var n = name;
                btn.GetComponent<Button>()?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                    if (_teamNameInput != null) _teamNameInput.text = n;
                });
            }
        }

        private void SetupCountryDropdown()
        {
            if (_countryDropdown == null) return;
            _countryDropdown.ClearOptions();
            _countryDropdown.AddOptions(_countries);
        }

        private void SetupColorButtons()
        {
            string[] colors = { "#FF0000", "#0000FF", "#00FF00", "#FFFF00", "#FF6600", "#800080", "#000000", "#FFFFFF" };
            for (int i = 0; i < _colorButtons.Length && i < colors.Length; i++)
            {
                int idx = i;
                string color = colors[i];
                if (ColorUtility.TryParseHtmlString(color, out Color c))
                    _colorButtons[i].GetComponent<Image>().color = c;

                _colorButtons[i].onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                    _selectedColor = color;
                    if (_selectedColorIndicator != null)
                        _selectedColorIndicator.transform.SetParent(_colorButtons[idx].transform, false);
                });
            }
        }

        private void SetupBadgeButtons()
        {
            for (int i = 0; i < _badgeButtons.Length; i++)
            {
                int idx = i;
                _badgeButtons[i]?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                    _selectedBadgeIndex = idx;
                });
            }
        }

        private void OnTeamNameChanged(string value)
        {
            if (_charCountText != null)
                _charCountText.text = $"{value.Length}/20";

            bool isValid = IsValidTeamName(value);
            if (_validationText != null)
            {
                _validationText.gameObject.SetActive(!isValid && value.Length > 0);
                _validationText.text = value.Length < 3
                    ? LocalizationManager.Instance?.Get("team_name_too_short") ?? "Min 3 characters"
                    : LocalizationManager.Instance?.Get("team_name_invalid") ?? "Invalid characters";
            }

            if (_confirmButton != null) _confirmButton.interactable = isValid;
        }

        private bool IsValidTeamName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 20) return false;
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '\'') return false;
            return true;
        }

        private void OnConfirmClicked()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            string teamName = _teamNameInput?.text?.Trim();
            if (!IsValidTeamName(teamName)) return;

            string country = _countries.Count > 0 && _countryDropdown != null
                ? _countries[_countryDropdown.value] : "";
            string city = _cityInput?.text?.Trim() ?? "";

            if (_isRegistrationMode)
            {
                GameManager.Instance?.SaveTeamData(teamName, country, city, _selectedColor, _selectedBadgeIndex);
                GameSceneManager.Instance?.LoadScene("MainMenu");
            }
            else
            {
                GameManager.Instance?.UpdateTeamName(teamName);
                gameObject.SetActive(false);
            }
        }

        private void OnCancelClicked()
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            gameObject.SetActive(false);
        }

        public void ShowChangeMode()
        {
            _isChangeMode = true;
            _isRegistrationMode = false;
            gameObject.SetActive(true);
            var userData = GameManager.Instance?.CurrentUserData;
            if (userData != null && _teamNameInput != null)
                _teamNameInput.text = userData.TeamName;
        }
    }
}
