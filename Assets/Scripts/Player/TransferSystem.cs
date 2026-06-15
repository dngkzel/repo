using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using FootballGame.Core;

namespace FootballGame.Player
{
    public class TransferSystem : MonoBehaviour
    {
        public static TransferSystem Instance { get; private set; }

        public event Action<FootballPlayerData> OnPlayerTransferred;
        public event Action<string> OnTransferFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void TransferPlayer(FootballPlayerData player, int price, Action<bool> callback)
        {
            string userId = GameManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                OnTransferFailed?.Invoke("Not logged in");
                callback?.Invoke(false);
                return;
            }

            bool spent = Economy.TokenSystem.Instance?.SpendTokens(price, $"Transfer: {player.Name}") ?? false;
            if (!spent)
            {
                string msg = "Not enough tokens";
                OnTransferFailed?.Invoke(msg);
                callback?.Invoke(false);
                return;
            }

            player.OwnerId = userId;
            var db = FirebaseDatabase.DefaultInstance.RootReference;
            db.Child("squads").Child(userId).Child(player.PlayerId)
              .SetValueAsync(player.ToFirebaseDictionary())
              .ContinueWithOnMainThread(task =>
              {
                  if (task.IsCompleted && !task.IsFaulted)
                  {
                      OnPlayerTransferred?.Invoke(player);
                      callback?.Invoke(true);
                  }
                  else
                  {
                      // Refund tokens on failure
                      Economy.TokenSystem.Instance?.AddTokens(price, "Transfer refund");
                      OnTransferFailed?.Invoke("Transfer failed, tokens refunded");
                      callback?.Invoke(false);
                  }
              });
        }

        public void LoadUserSquad(string userId, Action<List<FootballPlayerData>> callback)
        {
            var db = FirebaseDatabase.DefaultInstance.RootReference;
            db.Child("squads").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                var squad = new List<FootballPlayerData>();
                if (task.IsCompleted && task.Result.Exists)
                {
                    foreach (var child in task.Result.Children)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var kv in child.Children)
                            dict[kv.Key] = kv.Value;
                        var p = FootballPlayerData.FromFirebaseDictionary(dict);
                        if (p != null) squad.Add(p);
                    }
                }
                callback?.Invoke(squad);
            });
        }
    }
}
