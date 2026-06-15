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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(string userId)
        {
            if (_initialized) return;
            _userId = userId;
            _dbRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{_userId}/tokens");
            Load();
            _initialized = true;
        }

        private void Load()
        {
            _dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists && task.Result.Child("balance").Value != null)
                    _balance = int.Parse(task.Result.Child("balance").Value.ToString());
                else
                    Save();
                OnBalanceChanged?.Invoke(_balance);
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
            _balance += amount;
            _history.Add(new TransactionRecord(amount, reason, _balance));
            Save();
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool SpendTokens(int amount, string reason)
        {
            if (amount <= 0 || _balance < amount) return false;
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
