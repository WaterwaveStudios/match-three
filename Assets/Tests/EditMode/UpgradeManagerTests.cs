using NUnit.Framework;
using UnityEngine;
using MatchThree.Scoring;

namespace MatchThree.Tests
{
    [TestFixture]
    public class UpgradeManagerTests
    {
        private UpgradeManager _upgrades;
        private WalletManager _wallet;

        [SetUp]
        public void SetUp()
        {
            _upgrades = new UpgradeManager();
            _wallet = new WalletManager();
            PlayerPrefs.DeleteKey(UpgradeManager.PrefsKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(UpgradeManager.PrefsKey);
        }

        // Tree structure

        [Test]
        public void Tree_HasNodes()
        {
            Assert.IsTrue(UpgradeManager.Tree.Length > 0);
        }

        [Test]
        public void ScoreBoost_IsRoot()
        {
            var node = _upgrades.GetNode("score_boost");
            Assert.IsNotNull(node);
            Assert.IsNull(node.ParentId);
            Assert.AreEqual("Score Boost", node.Name);
        }

        [Test]
        public void Tier1Nodes_HaveScoreBoostAsParent()
        {
            Assert.AreEqual("score_boost", _upgrades.GetNode("extra_row").ParentId);
            Assert.AreEqual("score_boost", _upgrades.GetNode("extra_col").ParentId);
            Assert.AreEqual("score_boost", _upgrades.GetNode("longer_round").ParentId);
        }

        // Levels

        [Test]
        public void GetLevel_DefaultsToZero()
        {
            Assert.AreEqual(0, _upgrades.GetLevel("score_boost"));
        }

        [Test]
        public void Purchase_IncrementsLevel()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(1, _upgrades.GetLevel("score_boost"));
        }

        [Test]
        public void Purchase_MultipleLevelsStack()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet); // level 1
            _upgrades.Purchase("score_boost", _wallet); // level 2
            Assert.AreEqual(2, _upgrades.GetLevel("score_boost"));
        }

        [Test]
        public void Purchase_FailsAtMaxLevel()
        {
            _wallet.AddFunds(1000);
            var node = _upgrades.GetNode("score_boost");

            for (int i = 0; i < node.MaxLevel; i++)
                _upgrades.Purchase("score_boost", _wallet);

            Assert.IsFalse(_upgrades.Purchase("score_boost", _wallet));
            Assert.AreEqual(node.MaxLevel, _upgrades.GetLevel("score_boost"));
        }

        [Test]
        public void IsMaxed_TrueAtMaxLevel()
        {
            _wallet.AddFunds(1000);
            var node = _upgrades.GetNode("score_boost");

            for (int i = 0; i < node.MaxLevel; i++)
                _upgrades.Purchase("score_boost", _wallet);

            Assert.IsTrue(_upgrades.IsMaxed("score_boost"));
        }

        [Test]
        public void IsMaxed_FalseBeforeMaxLevel()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.IsFalse(_upgrades.IsMaxed("score_boost"));
        }

        // Costs

        [Test]
        public void GetCost_ReturnsFirstLevelCost()
        {
            Assert.AreEqual(10, _upgrades.GetCost("score_boost"));
        }

        [Test]
        public void GetCost_EscalatesPerLevel()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet); // costs 10
            Assert.AreEqual(20, _upgrades.GetCost("score_boost")); // next level costs 20
        }

        [Test]
        public void GetCost_ReturnsNegativeOneWhenMaxed()
        {
            _wallet.AddFunds(1000);
            var node = _upgrades.GetNode("score_boost");
            for (int i = 0; i < node.MaxLevel; i++)
                _upgrades.Purchase("score_boost", _wallet);

            Assert.AreEqual(-1, _upgrades.GetCost("score_boost"));
        }

        [Test]
        public void GetCost_ReturnsNegativeOneForUnknown()
        {
            Assert.AreEqual(-1, _upgrades.GetCost("nonexistent"));
        }

        [Test]
        public void Purchase_DeductsCorrectCostPerLevel()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet); // 10
            Assert.AreEqual(90, _wallet.Balance);

            _upgrades.Purchase("score_boost", _wallet); // 20
            Assert.AreEqual(70, _wallet.Balance);

            _upgrades.Purchase("score_boost", _wallet); // 40
            Assert.AreEqual(30, _wallet.Balance);
        }

        // Visibility

        [Test]
        public void RootNode_AlwaysVisible()
        {
            Assert.IsTrue(_upgrades.IsVisible("score_boost"));
        }

        [Test]
        public void ChildNode_HiddenWhenParentNotPurchased()
        {
            Assert.IsFalse(_upgrades.IsVisible("extra_row"));
            Assert.IsFalse(_upgrades.IsVisible("extra_col"));
            Assert.IsFalse(_upgrades.IsVisible("longer_round"));
        }

        [Test]
        public void ChildNode_VisibleWhenParentPurchased()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet);

            Assert.IsTrue(_upgrades.IsVisible("extra_row"));
            Assert.IsTrue(_upgrades.IsVisible("extra_col"));
            Assert.IsTrue(_upgrades.IsVisible("longer_round"));
        }

        [Test]
        public void IsVisible_FalseForUnknownId()
        {
            Assert.IsFalse(_upgrades.IsVisible("nonexistent"));
        }

        // Purchase guards

        [Test]
        public void Purchase_FailsIfCantAfford()
        {
            _wallet.AddFunds(5);
            Assert.IsFalse(_upgrades.Purchase("score_boost", _wallet));
            Assert.AreEqual(0, _upgrades.GetLevel("score_boost"));
            Assert.AreEqual(5, _wallet.Balance);
        }

        [Test]
        public void Purchase_FailsIfNotVisible()
        {
            _wallet.AddFunds(100);
            Assert.IsFalse(_upgrades.Purchase("extra_row", _wallet));
            Assert.AreEqual(0, _upgrades.GetLevel("extra_row"));
        }

        [Test]
        public void Purchase_FailsForUnknownId()
        {
            _wallet.AddFunds(100);
            Assert.IsFalse(_upgrades.Purchase("nonexistent", _wallet));
        }

        // Aggregated effects

        [Test]
        public void BonusPerTile_EqualsScoreBoostLevel()
        {
            _wallet.AddFunds(100);
            Assert.AreEqual(0, _upgrades.BonusPerTile);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(1, _upgrades.BonusPerTile);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(2, _upgrades.BonusPerTile);
        }

        [Test]
        public void ExtraRows_EqualsExtraRowLevel()
        {
            _wallet.AddFunds(1000);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(0, _upgrades.ExtraRows);
            _upgrades.Purchase("extra_row", _wallet);
            Assert.AreEqual(1, _upgrades.ExtraRows);
        }

        [Test]
        public void ExtraCols_EqualsExtraColLevel()
        {
            _wallet.AddFunds(1000);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(0, _upgrades.ExtraCols);
            _upgrades.Purchase("extra_col", _wallet);
            Assert.AreEqual(1, _upgrades.ExtraCols);
        }

        [Test]
        public void BonusRoundTime_EqualsLongerRoundLevel()
        {
            _wallet.AddFunds(1000);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(0, _upgrades.BonusRoundTime);
            _upgrades.Purchase("longer_round", _wallet);
            Assert.AreEqual(1, _upgrades.BonusRoundTime);
        }

        // Persistence

        [Test]
        public void SaveAndLoad_PreservesLevels()
        {
            _wallet.AddFunds(1000);
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("extra_row", _wallet);
            _upgrades.Save();

            var loaded = new UpgradeManager();
            loaded.Load();
            Assert.AreEqual(2, loaded.GetLevel("score_boost"));
            Assert.AreEqual(1, loaded.GetLevel("extra_row"));
            Assert.AreEqual(0, loaded.GetLevel("extra_col"));
        }

        [Test]
        public void Load_WithNoData_StartsEmpty()
        {
            _upgrades.Load();
            Assert.AreEqual(0, _upgrades.GetLevel("score_boost"));
            Assert.AreEqual(0, _upgrades.BonusPerTile);
        }

        [Test]
        public void Load_ClearsPreviousState()
        {
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.AreEqual(1, _upgrades.GetLevel("score_boost"));

            _upgrades.Load(); // no saved data, should clear
            Assert.AreEqual(0, _upgrades.GetLevel("score_boost"));
        }
    }
}
