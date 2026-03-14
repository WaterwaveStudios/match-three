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

        [Test]
        public void CascadeChance_DefaultsToZero()
        {
            Assert.AreEqual(0, _upgrades.CascadeChance);
        }

        [Test]
        public void CascadeChance_Increases20PerLevel()
        {
            _wallet.AddFunds(10000);
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("cascade_chance", _wallet);
            Assert.AreEqual(20, _upgrades.CascadeChance);
            _upgrades.Purchase("cascade_chance", _wallet);
            Assert.AreEqual(40, _upgrades.CascadeChance);
        }

        [Test]
        public void CascadeChance_IsVisibleWhenScoreBoostPurchased()
        {
            Assert.IsFalse(_upgrades.IsVisible("cascade_chance"));
            _wallet.AddFunds(100);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.IsTrue(_upgrades.IsVisible("cascade_chance"));
        }

        [Test]
        public void CascadeChance_MaxesAt100()
        {
            _wallet.AddFunds(10000);
            _upgrades.Purchase("score_boost", _wallet);
            for (int i = 0; i < 5; i++)
                _upgrades.Purchase("cascade_chance", _wallet);
            Assert.AreEqual(100, _upgrades.CascadeChance);
            Assert.IsTrue(_upgrades.IsMaxed("cascade_chance"));
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

        // Second ring nodes

        [Test]
        public void SecondRingNodes_ExistInTree()
        {
            Assert.IsNotNull(_upgrades.GetNode("quick_swap"));
            Assert.IsNotNull(_upgrades.GetNode("wild_tile"));
            Assert.IsNotNull(_upgrades.GetNode("bigger_match"));
            Assert.IsNotNull(_upgrades.GetNode("wider_match"));
            Assert.IsNotNull(_upgrades.GetNode("chain_bonus"));
            Assert.IsNotNull(_upgrades.GetNode("combo_frenzy"));
            Assert.IsNotNull(_upgrades.GetNode("score_rush"));
        }

        [Test]
        public void SecondRing_VisibleWhenParentPurchased()
        {
            _wallet.AddFunds(10000);
            Assert.IsFalse(_upgrades.IsVisible("quick_swap"));

            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("longer_round", _wallet);
            Assert.IsTrue(_upgrades.IsVisible("quick_swap"));
        }

        [Test]
        public void SecondRing_CascadeChildren_VisibleWhenCascadeChancePurchased()
        {
            _wallet.AddFunds(10000);
            _upgrades.Purchase("score_boost", _wallet);
            Assert.IsFalse(_upgrades.IsVisible("chain_bonus"));

            _upgrades.Purchase("cascade_chance", _wallet);
            Assert.IsTrue(_upgrades.IsVisible("chain_bonus"));
            Assert.IsTrue(_upgrades.IsVisible("combo_frenzy"));
            Assert.IsTrue(_upgrades.IsVisible("score_rush"));
        }

        // Legendary nodes

        [Test]
        public void LegendaryNodes_ExistInTree()
        {
            Assert.IsNotNull(_upgrades.GetNode("time_warp"));
            Assert.IsNotNull(_upgrades.GetNode("tile_storm"));
            Assert.IsTrue(_upgrades.GetNode("time_warp").IsLegendary);
            Assert.IsTrue(_upgrades.GetNode("tile_storm").IsLegendary);
        }

        [Test]
        public void Legendary_VisibleWhenEitherNeighbourPurchased()
        {
            _wallet.AddFunds(10000);
            Assert.IsFalse(_upgrades.IsVisible("time_warp"));

            // Purchase path to combo_frenzy
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("cascade_chance", _wallet);
            _upgrades.Purchase("combo_frenzy", _wallet);

            // Visible with just one neighbour
            Assert.IsTrue(_upgrades.IsVisible("time_warp"));
        }

        [Test]
        public void Legendary_CannotPurchaseWithOnlyOneNeighbour()
        {
            _wallet.AddFunds(10000);
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("cascade_chance", _wallet);
            _upgrades.Purchase("combo_frenzy", _wallet);

            // Visible but can't purchase — needs score_rush too
            Assert.IsTrue(_upgrades.IsVisible("time_warp"));
            Assert.IsFalse(_upgrades.CanPurchase("time_warp"));
            Assert.IsFalse(_upgrades.Purchase("time_warp", _wallet));
        }

        [Test]
        public void Legendary_CanPurchaseWithBothNeighbours()
        {
            _wallet.AddFunds(10000);
            _upgrades.Purchase("score_boost", _wallet);
            _upgrades.Purchase("cascade_chance", _wallet);
            _upgrades.Purchase("combo_frenzy", _wallet);
            _upgrades.Purchase("score_rush", _wallet);

            Assert.IsTrue(_upgrades.CanPurchase("time_warp"));
            Assert.IsTrue(_upgrades.Purchase("time_warp", _wallet));
            Assert.AreEqual(1, _upgrades.GetLevel("time_warp"));
        }

        [Test]
        public void Legendary_HasChallengeText()
        {
            var timeWarp = _upgrades.GetNode("time_warp");
            Assert.IsNotNull(timeWarp.Challenge);
            Assert.IsTrue(timeWarp.Challenge.Length > 0);
        }

        // CanPurchase

        [Test]
        public void CanPurchase_FalseForUnknownNode()
        {
            Assert.IsFalse(_upgrades.CanPurchase("nonexistent"));
        }

        [Test]
        public void CanPurchase_FalseWhenMaxed()
        {
            _wallet.AddFunds(10000);
            for (int i = 0; i < 3; i++)
                _upgrades.Purchase("score_boost", _wallet);
            Assert.IsFalse(_upgrades.CanPurchase("score_boost"));
        }
    }
}
