using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class TeamSelectionUI : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField inputTeamName;
        public TMP_InputField inputCountry;
        public TMP_InputField inputCity;

        [Header("Kit Colors")]
        public Image primaryColorPreview;
        public Image secondaryColorPreview;
        public Slider sliderPrimaryR;
        public Slider sliderPrimaryG;
        public Slider sliderPrimaryB;
        public Slider sliderSecondaryR;
        public Slider sliderSecondaryG;
        public Slider sliderSecondaryB;

        [Header("Buttons")]
        public Button btnConfirm;

        [Header("Feedback")]
        public TextMeshProUGUI txtError;

        private void Start()
        {
            btnConfirm?.onClick.AddListener(OnConfirm);

            sliderPrimaryR?.onValueChanged.AddListener(_ => UpdatePrimaryPreview());
            sliderPrimaryG?.onValueChanged.AddListener(_ => UpdatePrimaryPreview());
            sliderPrimaryB?.onValueChanged.AddListener(_ => UpdatePrimaryPreview());
            sliderSecondaryR?.onValueChanged.AddListener(_ => UpdateSecondaryPreview());
            sliderSecondaryG?.onValueChanged.AddListener(_ => UpdateSecondaryPreview());
            sliderSecondaryB?.onValueChanged.AddListener(_ => UpdateSecondaryPreview());

            SetDefaultSliders();
            if (txtError) txtError.text = "";
        }

        private void SetDefaultSliders()
        {
            if (sliderPrimaryR) sliderPrimaryR.value = 0.9f;
            if (sliderPrimaryG) sliderPrimaryG.value = 0.1f;
            if (sliderPrimaryB) sliderPrimaryB.value = 0.1f;
            if (sliderSecondaryR) sliderSecondaryR.value = 1f;
            if (sliderSecondaryG) sliderSecondaryG.value = 1f;
            if (sliderSecondaryB) sliderSecondaryB.value = 1f;
            UpdatePrimaryPreview();
            UpdateSecondaryPreview();
        }

        private void UpdatePrimaryPreview()
        {
            if (primaryColorPreview)
                primaryColorPreview.color = new Color(
                    sliderPrimaryR?.value ?? 0.9f,
                    sliderPrimaryG?.value ?? 0.1f,
                    sliderPrimaryB?.value ?? 0.1f);
        }

        private void UpdateSecondaryPreview()
        {
            if (secondaryColorPreview)
                secondaryColorPreview.color = new Color(
                    sliderSecondaryR?.value ?? 1f,
                    sliderSecondaryG?.value ?? 1f,
                    sliderSecondaryB?.value ?? 1f);
        }

        private async void OnConfirm()
        {
            if (txtError) txtError.text = "";
            string teamName = inputTeamName?.text?.Trim() ?? "";
            string country  = inputCountry?.text?.Trim()  ?? "";
            string city     = inputCity?.text?.Trim()     ?? "";

            var loc = LocalizationManager.Instance;
            if (string.IsNullOrEmpty(teamName))
            {
                if (txtError) txtError.text = loc?.Get("error_team_name_empty") ?? "Please enter a team name.";
                return;
            }
            if (teamName.Length < 3 || teamName.Length > 20)
            {
                if (txtError) txtError.text = loc?.Get("error_team_name_length") ?? "Team name must be 3–20 characters.";
                return;
            }

            btnConfirm.interactable = false;
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                var pd = gm.CurrentUserData;
                if (pd != null)
                {
                    pd.Country = country;
                    pd.City    = city;
                }
                await System.Threading.Tasks.Task.Run(() => { }); // yield frame
                gm.UpdateTeamName(teamName);
            }

            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }
}
