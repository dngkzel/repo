using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Google;

namespace FootballGame.Authentication
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        public static event Action<FirebaseUser> OnLoginSuccess;
        public static event Action<string> OnLoginFailed;
        public static event Action OnLogoutSuccess;
        public static event Action OnPasswordResetSent;

        private FirebaseAuth _auth;
        private GoogleSignInConfiguration _googleSignInConfig;

        [Header("Google Sign-In")]
        [SerializeField] private string googleWebClientId = "YOUR_WEB_CLIENT_ID_HERE";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _googleSignInConfig = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                RequestIdToken = true,
                RequestEmail = true
            };
        }

        // ---- Email/Password Auth ----
        public void RegisterWithEmail(string email, string password, Action<FirebaseUser, string> callback)
        {
            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string error = GetAuthError(task.Exception);
                    Debug.LogError($"[AuthManager] Register failed: {error}");
                    callback?.Invoke(null, error);
                    OnLoginFailed?.Invoke(error);
                    return;
                }
                var user = task.Result.User;
                Debug.Log($"[AuthManager] Registered: {user.Email}");
                callback?.Invoke(user, null);
                OnLoginSuccess?.Invoke(user);
            });
        }

        public void LoginWithEmail(string email, string password, Action<FirebaseUser, string> callback)
        {
            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string error = GetAuthError(task.Exception);
                    Debug.LogError($"[AuthManager] Login failed: {error}");
                    callback?.Invoke(null, error);
                    OnLoginFailed?.Invoke(error);
                    return;
                }
                var user = task.Result.User;
                Debug.Log($"[AuthManager] Logged in: {user.Email}");
                callback?.Invoke(user, null);
                OnLoginSuccess?.Invoke(user);
            });
        }

        public void SendPasswordReset(string email, Action<bool, string> callback)
        {
            _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    string error = GetAuthError(task.Exception);
                    callback?.Invoke(false, error);
                    return;
                }
                OnPasswordResetSent?.Invoke();
                callback?.Invoke(true, null);
            });
        }

        // ---- Google Sign-In ----
        public void LoginWithGoogle(Action<FirebaseUser, string> callback)
        {
            GoogleSignIn.Configuration = _googleSignInConfig;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string error = task.IsCanceled ? "Sign-in cancelled." : task.Exception?.Message ?? "Google sign-in failed.";
                    Debug.LogError($"[AuthManager] Google sign-in failed: {error}");
                    callback?.Invoke(null, error);
                    OnLoginFailed?.Invoke(error);
                    return;
                }
                OnGoogleSignInResult(task.Result, callback);
            });
        }

        private void OnGoogleSignInResult(GoogleSignInUser googleUser, Action<FirebaseUser, string> callback)
        {
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string error = GetAuthError(task.Exception);
                    callback?.Invoke(null, error);
                    OnLoginFailed?.Invoke(error);
                    return;
                }
                var user = task.Result.User;
                Debug.Log($"[AuthManager] Google login success: {user.DisplayName}");
                callback?.Invoke(user, null);
                OnLoginSuccess?.Invoke(user);
            });
        }

        // ---- Logout ----
        public void Logout()
        {
            _auth.SignOut();
            GoogleSignIn.DefaultInstance?.SignOut();
            OnLogoutSuccess?.Invoke();
            Debug.Log("[AuthManager] User logged out.");
        }

        // ---- Helpers ----
        public bool IsLoggedIn => _auth.CurrentUser != null;
        public bool IsAuthenticated => IsLoggedIn;
        public FirebaseUser CurrentUser => _auth.CurrentUser;
        public void SignOut() => Logout();

        private string GetAuthError(AggregateException ex)
        {
            if (ex == null) return "Unknown error.";
            Firebase.FirebaseException fbEx = ex.GetBaseException() as Firebase.FirebaseException;
            if (fbEx != null)
            {
                AuthError code = (AuthError)fbEx.ErrorCode;
                switch (code)
                {
                    case AuthError.EmailAlreadyInUse: return "This email is already in use.";
                    case AuthError.InvalidEmail: return "Invalid email address.";
                    case AuthError.WeakPassword: return "Password is too weak.";
                    case AuthError.UserNotFound: return "No account found with this email.";
                    case AuthError.WrongPassword: return "Incorrect password.";
                    case AuthError.NetworkRequestFailed: return "Network error. Check your connection.";
                    case AuthError.TooManyRequests: return "Too many attempts. Try again later.";
                    default: return $"Auth error: {code}";
                }
            }
            return ex.Message;
        }
    }
}
