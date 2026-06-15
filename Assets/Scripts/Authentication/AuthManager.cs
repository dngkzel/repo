using System;
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

        private FirebaseAuth _auth;
        private GoogleSignInConfiguration _googleConfig;

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
            // Wait for GameManager to confirm Firebase is ready before accessing DefaultInstance
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.IsFirebaseReady)
                InitFirebase();
            else
                Core.GameManager.OnGameInitialized += InitFirebase;
        }

        private void InitFirebase()
        {
            Core.GameManager.OnGameInitialized -= InitFirebase;
            _auth = FirebaseAuth.DefaultInstance;
            _googleConfig = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                RequestIdToken = true,
                RequestEmail = true
            };
        }

        public void RegisterWithEmail(string email, string password, Action<FirebaseUser, string> callback)
        {
            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string err = ParseAuthError(task.Exception);
                    OnLoginFailed?.Invoke(err);
                    callback?.Invoke(null, err);
                    return;
                }
                var user = task.Result.User;
                OnLoginSuccess?.Invoke(user);
                callback?.Invoke(user, null);
            });
        }

        public void LoginWithEmail(string email, string password, Action<FirebaseUser, string> callback)
        {
            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string err = ParseAuthError(task.Exception);
                    OnLoginFailed?.Invoke(err);
                    callback?.Invoke(null, err);
                    return;
                }
                var user = task.Result.User;
                OnLoginSuccess?.Invoke(user);
                callback?.Invoke(user, null);
            });
        }

        public void SendPasswordReset(string email, Action<bool, string> callback)
        {
            _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { callback?.Invoke(false, ParseAuthError(task.Exception)); return; }
                callback?.Invoke(true, null);
            });
        }

        public void LoginWithGoogle(Action<FirebaseUser, string> callback)
        {
            GoogleSignIn.Configuration = _googleConfig;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string err = task.Exception?.Message ?? "Google sign-in cancelled";
                    OnLoginFailed?.Invoke(err);
                    callback?.Invoke(null, err);
                    return;
                }
                var cred = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
                _auth.SignInWithCredentialAsync(cred).ContinueWithOnMainThread(fbTask =>
                {
                    if (fbTask.IsFaulted || fbTask.IsCanceled)
                    {
                        string err = ParseAuthError(fbTask.Exception);
                        OnLoginFailed?.Invoke(err);
                        callback?.Invoke(null, err);
                        return;
                    }
                    var user = fbTask.Result.User;
                    OnLoginSuccess?.Invoke(user);
                    callback?.Invoke(user, null);
                });
            });
        }

        public void Logout()
        {
            _auth.SignOut();
            GoogleSignIn.DefaultInstance?.SignOut();
            OnLogoutSuccess?.Invoke();
        }

        public void SignOut() => Logout();
        public bool IsLoggedIn => _auth?.CurrentUser != null;
        public bool IsAuthenticated => IsLoggedIn;
        public FirebaseUser CurrentUser => _auth?.CurrentUser;

        private string ParseAuthError(AggregateException ex)
        {
            if (ex == null) return "Unknown error";
            if (ex.GetBaseException() is Firebase.FirebaseException fbEx)
            {
                return (AuthError)fbEx.ErrorCode switch
                {
                    AuthError.EmailAlreadyInUse => "This email is already registered.",
                    AuthError.InvalidEmail => "Invalid email address.",
                    AuthError.WeakPassword => "Password is too weak.",
                    AuthError.UserNotFound => "No account found with this email.",
                    AuthError.WrongPassword => "Incorrect password.",
                    AuthError.NetworkRequestFailed => "Network error. Check your connection.",
                    AuthError.TooManyRequests => "Too many attempts. Try again later.",
                    _ => $"Auth error ({(AuthError)fbEx.ErrorCode})"
                };
            }
            return ex.Message;
        }
    }
}
