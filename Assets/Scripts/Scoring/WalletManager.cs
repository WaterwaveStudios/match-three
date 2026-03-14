using System;
using UnityEngine;

namespace MatchThree.Scoring
{
    public class WalletManager
    {
        public const string PrefsKey = "MatchThree_Wallet";

        public int Balance { get; private set; }

        public event Action<int> OnBalanceChanged;

        public void AddFunds(int amount)
        {
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool CanAfford(int cost)
        {
            return Balance >= cost;
        }

        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Balance -= cost;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(PrefsKey, Balance);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            Balance = PlayerPrefs.GetInt(PrefsKey, 0);
        }
    }
}
