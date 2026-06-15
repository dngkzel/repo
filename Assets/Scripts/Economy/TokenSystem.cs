using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

namespace FootballGame.Economy
{
    public class TokenSystem : MonoBehaviour
    {
        public static TokenSystem Instance { get; private set; }

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnTokenBalanceChanged
        {
            add    => OnBalanceChanged += value;
            remove => OnBalanceChanged -= value;
        }

        [Serializable]
        public class TransactionRecord
        {
            public int amount;
            public string reason;
            public int newBalance;
            public DateTime timestamp;
            public TransactionRecord(int amt, string rsn, int bal)
            { amount = amt; reason = rsn; newBalance = bal; timestamp = DateTime.UtcNow; }
        }

        private int _balance;
        private List<TransactionRecord> _history = new List<TransactionRecord>();
        private DatabaseReference _dbRef;
        private string _userId;
        private bool _initialized;
        private bool _serverSynced; // true once Firebase load completes

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // knownBalance: pass from already-loaded PlayerData to avoid race with async Load
        public void Initialize(string userId, int knownBalance = -1)
        {
            if (_initialized && _userId == userId) return;

            _userId = userId;
            _dbRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{_userId}/tokens");
            _history.Clear();
            _serverSynced = false;

            if (knownBalance >= 0)
            {
                _balance = knownBalance;
                OnBalanceChanged?.Invoke(_balance);
            }

            _initialized = true;
            SyncFromServer();
        }

        public void ResetForLogout()
        {
            _initialized = false;
            _serverSynced = false;
            _userId = null;
            _dbRef = null;
            _balance = 0;
            _history.Clear();
        }

        private void SyncFromServer()
        {
            if (_dbRef == null) return;
            _dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!_initialized) return; // user logged out before response
                if (task.IsCompleted && !task.IsFaulted && task.Result.Exists
                    && task.Result.Child("balance").Value != null)
                {
                    // Only adopt the server value if we haven't made local changes yet
                    if (!_serverSynced)
                    {
                        long raw = Convert.ToInt64(task.Result.Child("balance").Value);
                        _balance = (int)Math.Min(raw, int.MaxValue);
                        OnBalanceChanged?.Invoke(_balance);
                    }
                }
                else if (!_serverSynced)
                {
                    // No server record yet — persist the known balance
                    Save();
                }
                _serverSynced = true;
            });
        }

        private void Save()
        {
            if (_dbRef == null) return;
            _dbRef.Child("balance").SetValueAsync(_balance);
        }

        public void AddTokens(int amount, string reason)
        {
            if (amount <= 0) return;
            _serverSynced = true; // block any pending SyncFromServer from overwriting
            _balance += amount;
            _history.Add(new TransactionRecord(amount, reason, _balance));
            Save();
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool SpendTokens(int amount, string reason)
        {
            if (amount <= 0 || _balance < amount) return false;
            _serverSynced = true;
            _balance -= amount;
            _history.Add(new TransactionRecord(-amount, reason, _balance));
            Save();
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        public int GetBalance() => _balance;
        public int Balance => _balance;
        public List<TransactionRecord> GetHistory() => new List<TransactionRecord>(_history);
    }
}
