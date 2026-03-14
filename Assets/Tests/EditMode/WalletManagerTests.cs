using NUnit.Framework;
using MatchThree.Scoring;
using UnityEngine;

namespace MatchThree.Tests
{
    [TestFixture]
    public class WalletManagerTests
    {
        private WalletManager _wallet;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(WalletManager.PrefsKey);
            _wallet = new WalletManager();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(WalletManager.PrefsKey);
        }

        [Test]
        public void StartsAtZero_WhenNoSavedData()
        {
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void AddFunds_IncreasesBalance()
        {
            _wallet.AddFunds(100);
            Assert.AreEqual(100, _wallet.Balance);
        }

        [Test]
        public void AddFunds_Accumulates()
        {
            _wallet.AddFunds(100);
            _wallet.AddFunds(250);
            Assert.AreEqual(350, _wallet.Balance);
        }

        [Test]
        public void CanAfford_ReturnsTrueWhenSufficientFunds()
        {
            _wallet.AddFunds(100);
            Assert.IsTrue(_wallet.CanAfford(50));
            Assert.IsTrue(_wallet.CanAfford(100));
        }

        [Test]
        public void CanAfford_ReturnsFalseWhenInsufficientFunds()
        {
            _wallet.AddFunds(100);
            Assert.IsFalse(_wallet.CanAfford(101));
        }

        [Test]
        public void Spend_DeductsBalance_ReturnsTrue()
        {
            _wallet.AddFunds(200);
            bool result = _wallet.Spend(75);
            Assert.IsTrue(result);
            Assert.AreEqual(125, _wallet.Balance);
        }

        [Test]
        public void Spend_FailsWhenInsufficientFunds()
        {
            _wallet.AddFunds(50);
            bool result = _wallet.Spend(100);
            Assert.IsFalse(result);
            Assert.AreEqual(50, _wallet.Balance);
        }

        [Test]
        public void Save_PersistsBalance()
        {
            _wallet.AddFunds(500);
            _wallet.Save();

            var loaded = new WalletManager();
            loaded.Load();
            Assert.AreEqual(500, loaded.Balance);
        }

        [Test]
        public void Load_RestoresSavedBalance()
        {
            _wallet.AddFunds(1234);
            _wallet.Save();

            _wallet = new WalletManager();
            Assert.AreEqual(0, _wallet.Balance);

            _wallet.Load();
            Assert.AreEqual(1234, _wallet.Balance);
        }

        [Test]
        public void OnBalanceChanged_FiresOnAddFunds()
        {
            int lastBalance = -1;
            _wallet.OnBalanceChanged += b => lastBalance = b;
            _wallet.AddFunds(100);
            Assert.AreEqual(100, lastBalance);
        }

        [Test]
        public void OnBalanceChanged_FiresOnSpend()
        {
            _wallet.AddFunds(200);
            int lastBalance = -1;
            _wallet.OnBalanceChanged += b => lastBalance = b;
            _wallet.Spend(75);
            Assert.AreEqual(125, lastBalance);
        }
    }
}
