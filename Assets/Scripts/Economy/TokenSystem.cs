using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;

namespace FootballGame.Economy
{
    public class TokenSystem : MonoBehaviour
    {
        // Singleton
        public static TokenSystem Instance { get; private set; }

        // Events
        public event Action<int> OnBalanceChanged;
        public event Action<TransactionRecord> OnTransactionComplete;

        [Serializable]
        public class TransactionRecord
        {
            public int amount;
            public string reason;
            public int newBalance;
            public DateTime timestamp;

            public TransactionRecord(int amount, string reason, int newBalance)
            {
                this.amount = amount;
                this.reason = reason;
                this.newBalance = newBalance;
                this.timestamp = DateTime.UtcNow;
            }
        }

        private int _balance;
        private List<TransactionRecord> _transactionHistory = new List<TransactionRecord>();
        private DatabaseReference _dbRef;
        private string _userId;
        private bool _isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(string userId)
        {
            if (_isInitialized) return;
            _userId = userId;
            _dbRef = FirebaseDatabase.DefaultInstance.GetReference($"users/{_userId}/tokens");
            LoadFromFirebase();
            _isInitialized = true;
        }

        private void LoadFromFirebase()
        {
            _dbRef.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[TokenSystem] Failed to load tokens: {task.Exception}");
                    return;
                }
                var snapshot = task.Result;
                if (snapshot.Exists && snapshot.Child("balance").Value != null)
                {
                    _balance = int.Parse(snapshot.Child("balance").Value.ToString());
                    // Load transaction history
                    _transactionHistory.Clear();
                    if (snapshot.Child("history").Exists)
                    {
                        foreach (var child in snapshot.Child("history").Children)
                        {
                            var record = new TransactionRecord(
                                int.Parse(child.Child("amount").Value?.ToString() ?? "0"),
                                child.Child("reason").Value?.ToString() ?? "",
                                int.Parse(child.Child("newBalance").Value?.ToString() ?? "0")
                            );
                            if (DateTime.TryParse(child.Child("timestamp").Value?.ToString(), out DateTime ts))
                                record.timestamp = ts;
                            _transactionHistory.Add(record);
                        }
                    }
                }
                else
                {
                    _balance = 0;
                    SaveToFirebase();
                }
                UnityMainThread.Execute(() => OnBalanceChanged?.Invoke(_balance));
            });
        }

        private void SaveToFirebase()
        {
            var data = new Dictionary<string, object>
            {
                ["balance"] = _balance
            };
            // Save last 50 transactions
            var historyData = new Dictionary<string, object>();
            int start = Mathf.Max(0, _transactionHistory.Count - 50);
            for (int i = start; i < _transactionHistory.Count; i++)
            {
                var r = _transactionHistory[i];
                historyData[i.ToString()] = new Dictionary<string, object>
                {
                    ["amount"] = r.amount,
                    ["reason"] = r.reason,
                    ["newBalance"] = r.newBalance,
                    ["timestamp"] = r.timestamp.ToString("O")
                };
            }
            data["history"] = historyData;
            _dbRef.SetValueAsync(data).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError($"[TokenSystem] Failed to save tokens: {task.Exception}");
            });
        }

        public void AddTokens(int amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[TokenSystem] AddTokens called with non-positive amount: {amount}");
                return;
            }
            _balance += amount;
            var record = new TransactionRecord(amount, reason, _balance);
            _transactionHistory.Add(record);
            SaveToFirebase();
            OnBalanceChanged?.Invoke(_balance);
            OnTransactionComplete?.Invoke(record);
            Debug.Log($"[TokenSystem] Added {amount} tokens ({reason}). New balance: {_balance}");
        }

        public bool SpendTokens(int amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[TokenSystem] SpendTokens called with non-positive amount: {amount}");
                return false;
            }
            if (_balance < amount)
            {
                Debug.LogWarning($"[TokenSystem] Insufficient tokens. Balance: {_balance}, Required: {amount}");
                return false;
            }
            _balance -= amount;
            var record = new TransactionRecord(-amount, reason, _balance);
            _transactionHistory.Add(record);
            SaveToFirebase();
            OnBalanceChanged?.Invoke(_balance);
            OnTransactionComplete?.Invoke(record);
            Debug.Log($"[TokenSystem] Spent {amount} tokens ({reason}). New balance: {_balance}");
            return true;
        }

        public int GetBalance() => _balance;
        public int Balance => _balance;

        // Alias for compatibility
        public event Action<int> OnTokenBalanceChanged
        {
            add => OnBalanceChanged += value;
            remove => OnBalanceChanged -= value;
        }

        // Async overload for MarketSystem compatibility
        public async System.Threading.Tasks.Task<bool> SpendTokensAsync(int amount, string reason)
        {
            return await System.Threading.Tasks.Task.FromResult(SpendTokens(amount, reason));
        }

        public List<TransactionRecord> GetTransactionHistory() => new List<TransactionRecord>(_transactionHistory);
    }

    // Helper to run callbacks on main thread
    public class UnityMainThread : MonoBehaviour
    {
        private static UnityMainThread _instance;
        private readonly Queue<Action> _actions = new Queue<Action>();

        public static void Execute(Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThread");
                _instance = go.AddComponent<UnityMainThread>();
                DontDestroyOnLoad(go);
            }
            lock (_instance._actions)
                _instance._actions.Enqueue(action);
        }

        private void Update()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                    _actions.Dequeue()?.Invoke();
            }
        }
    }
}
