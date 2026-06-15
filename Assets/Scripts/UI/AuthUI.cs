using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Authentication;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class AuthUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject loginPanel;
        public GameObject registerPanel;
        public GameObject resetPanel;

        [Header("Login")]
        public TMP_InputField loginEmail;
        public TMP_InputField loginPassword;
        public Button btnLogin;
        public Button btnGoogleLogin;
        public Button btnToRegister;
        public Button btnToReset;
        public TextMeshProUGUI txtLoginError;

        [Header("Register")]
        public TMP_InputField regEmail;
        public TMP_InputField regPassword;
        public TMP_InputField regDisplayName;
        public TMP_InputField regTeamName;
        public TMP_InputField regCountry;
        public TMP_InputField regCity;
        public Button btnRegister;
        public Button btnToLogin;
        public TextMeshProUGUI txtRegError;

        [Header("Reset Password")]
        public TMP_InputField resetEmail;
        public Button btnSendReset;
        public Button btnBackToLogin;
        public TextMeshProUGUI txtResetMsg;

        private void OnEnable()
        {
            AuthManager.OnLoginFailed += OnLoginFailed;
            RegistrationManager.OnRegistrationFailed += OnRegFailed;
        }

        private void OnDisable()
        {
            AuthManager.OnLoginFailed -= OnLoginFailed;
            RegistrationManager.OnRegistrationFailed -= OnRegFailed;
        }

        private void Start()
        {
            ShowLogin();
            btnLogin?.onClick.AddListener(OnLogin);
            btnGoogleLogin?.onClick.AddListener(OnGoogleLogin);
            btnToRegister?.onClick.AddListener(ShowRegister);
            btnToReset?.onClick.AddListener(ShowReset);
            btnRegister?.onClick.AddListener(OnRegister);
            btnToLogin?.onClick.AddListener(ShowLogin);
            btnSendReset?.onClick.AddListener(OnSendReset);
            btnBackToLogin?.onClick.AddListener(ShowLogin);
        }

        private void ShowLogin()
        {
            loginPanel?.SetActive(true);
            registerPanel?.SetActive(false);
            resetPanel?.SetActive(false);
            if (txtLoginError) txtLoginError.text = "";
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private void ShowRegister()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(true);
            resetPanel?.SetActive(false);
            if (txtRegError) txtRegError.text = "";
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private void ShowReset()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(false);
            resetPanel?.SetActive(true);
            if (txtResetMsg) txtResetMsg.text = "";
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private void OnLogin()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            if (txtLoginError) txtLoginError.text = "";
            string email = loginEmail?.text?.Trim() ?? "";
            string pass  = loginPassword?.text ?? "";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                if (txtLoginError) txtLoginError.text = LocalizationManager.Instance?.Get("error_empty_fields") ?? "Please fill in all fields.";
                return;
            }
            AuthManager.Instance?.LoginWithEmail(email, pass, (user, err) =>
            {
                if (!string.IsNullOrEmpty(err) && txtLoginError) txtLoginError.text = err;
            });
        }

        private void OnGoogleLogin()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            AuthManager.Instance?.LoginWithGoogle((user, err) =>
            {
                if (!string.IsNullOrEmpty(err) && txtLoginError) txtLoginError.text = err;
            });
        }

        private void OnRegister()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            if (txtRegError) txtRegError.text = "";
            string email   = regEmail?.text?.Trim()       ?? "";
            string pass    = regPassword?.text            ?? "";
            string name    = regDisplayName?.text?.Trim() ?? "";
            string team    = regTeamName?.text?.Trim()    ?? "";
            string country = regCountry?.text?.Trim()     ?? "";
            string city    = regCity?.text?.Trim()        ?? "";

            var loc = LocalizationManager.Instance;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) ||
                string.IsNullOrEmpty(name)  || string.IsNullOrEmpty(team))
            {
                if (txtRegError) txtRegError.text = loc?.Get("error_empty_fields") ?? "Please fill in all fields.";
                return;
            }

            if (btnRegister) btnRegister.interactable = false;
            RegistrationManager.Instance?.RegisterNewUser(email, pass, team, country, city, name,
                (ok, err) =>
                {
                    if (btnRegister) btnRegister.interactable = true;
                    if (!ok && txtRegError) txtRegError.text = err ?? "Registration failed.";
                });
        }

        private void OnSendReset()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            string email = resetEmail?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(email))
            {
                if (txtResetMsg) txtResetMsg.text = LocalizationManager.Instance?.Get("error_empty_fields") ?? "Enter your email.";
                return;
            }
            AuthManager.Instance?.SendPasswordReset(email, (ok, err) =>
            {
                if (txtResetMsg)
                    txtResetMsg.text = ok
                        ? LocalizationManager.Instance?.Get("reset_email_sent") ?? "Reset email sent."
                        : err ?? "Failed to send reset email.";
            });
        }

        private void OnLoginFailed(string msg)
        {
            if (loginPanel?.activeSelf == true && txtLoginError) txtLoginError.text = msg;
        }

        private void OnRegFailed(string msg)
        {
            if (registerPanel?.activeSelf == true && txtRegError) txtRegError.text = msg;
        }
    }
}
