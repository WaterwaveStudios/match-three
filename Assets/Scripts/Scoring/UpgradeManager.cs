using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchThree.Scoring
{
    public class UpgradeNode
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int[] LevelCosts { get; }
        public string ParentId { get; }
        public string RequiredNeighbourId { get; }
        public string Challenge { get; }

        public int MaxLevel => LevelCosts.Length;
        public bool IsLegendary => RequiredNeighbourId != null;

        public UpgradeNode(string id, string name, string description, int[] levelCosts, string parentId,
            string requiredNeighbourId = null, string challenge = null)
        {
            Id = id;
            Name = name;
            Description = description;
            LevelCosts = levelCosts;
            ParentId = parentId;
            RequiredNeighbourId = requiredNeighbourId;
            Challenge = challenge;
        }
    }

    public class UpgradeManager
    {
        public const string PrefsKey = "MatchThree_Upgrades";

        public static readonly UpgradeNode[] Tree = new[]
        {
            // Root
            new UpgradeNode("score_boost", "Score Boost", "+1 point per tile per level",
                new[] { 10, 20, 40 }, null),

            // First ring
            new UpgradeNode("extra_row", "Extra Row", "+1 board row per level",
                new[] { 15, 30, 60, 120 }, "score_boost"),
            new UpgradeNode("extra_col", "Extra Column", "+1 board column per level",
                new[] { 15, 30, 60, 120 }, "score_boost"),
            new UpgradeNode("longer_round", "Longer Round", "+1s round duration per level",
                new[] { 20, 40, 80 }, "score_boost"),
            new UpgradeNode("cascade_chance", "Cascade Chance", "+20% cascade chance per level",
                new[] { 15, 30, 60, 120, 240 }, "score_boost"),

            // Second ring
            new UpgradeNode("quick_swap", "Quick Swap", "-20% swap speed per level",
                new[] { 25, 50, 100 }, "longer_round"),
            new UpgradeNode("wild_tile", "Wild Tile", "Wild tile spawns rarely",
                new[] { 75 }, "extra_row"),
            new UpgradeNode("bigger_match", "Bigger Match", "+50 bonus for 4+ matches per level",
                new[] { 40, 80 }, "extra_row"),
            new UpgradeNode("wider_match", "Wider Match", "L and T-shaped matches count",
                new[] { 100 }, "extra_col"),
            new UpgradeNode("chain_bonus", "Chain Bonus", "+1 cascade multiplier per level",
                new[] { 30, 60, 120 }, "cascade_chance"),
            new UpgradeNode("combo_frenzy", "Combo Frenzy", "Cascades freeze the timer",
                new[] { 80 }, "cascade_chance"),
            new UpgradeNode("score_rush", "Score Rush", "+1s bonus time per cascade per level",
                new[] { 25, 50, 100 }, "cascade_chance"),

            // Legendary
            new UpgradeNode("time_warp", "Time Warp", "Cascades rewind timer by 2s",
                new[] { 200 }, "combo_frenzy", "score_rush",
                "Score 200 pts in a single cascade chain"),
            new UpgradeNode("tile_storm", "Tile Storm", "Every 5th match shuffles board",
                new[] { 250 }, "bigger_match", "wild_tile",
                "Match 30 tiles in a single round"),
        };

        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public int GetLevel(string id)
        {
            return _levels.TryGetValue(id, out int level) ? level : 0;
        }

        public UpgradeNode GetNode(string id)
        {
            return Array.Find(Tree, n => n.Id == id);
        }

        public int GetCost(string id)
        {
            var node = GetNode(id);
            if (node == null) return -1;
            int level = GetLevel(id);
            if (level >= node.MaxLevel) return -1;
            return node.LevelCosts[level];
        }

        public bool IsVisible(string id)
        {
            var node = GetNode(id);
            if (node == null) return false;
            if (node.ParentId == null) return true;

            if (node.IsLegendary)
            {
                // Legendary: visible if either parent or neighbour purchased
                return GetLevel(node.ParentId) > 0 || GetLevel(node.RequiredNeighbourId) > 0;
            }

            return GetLevel(node.ParentId) > 0;
        }

        public bool IsMaxed(string id)
        {
            var node = GetNode(id);
            if (node == null) return false;
            return GetLevel(id) >= node.MaxLevel;
        }

        public bool CanPurchase(string id)
        {
            var node = GetNode(id);
            if (node == null) return false;
            if (!IsVisible(id)) return false;
            if (IsMaxed(id)) return false;

            if (node.IsLegendary)
            {
                if (GetLevel(node.ParentId) <= 0 || GetLevel(node.RequiredNeighbourId) <= 0)
                    return false;
            }

            return true;
        }

        public bool Purchase(string id, WalletManager wallet)
        {
            if (!CanPurchase(id)) return false;

            int cost = GetCost(id);
            if (!wallet.Spend(cost)) return false;

            _levels[id] = GetLevel(id) + 1;
            return true;
        }

        // Aggregated effects
        public int BonusPerTile => GetLevel("score_boost");
        public int ExtraRows => GetLevel("extra_row");
        public int ExtraCols => GetLevel("extra_col");
        public int BonusRoundTime => GetLevel("longer_round");
        public int CascadeChance => GetLevel("cascade_chance") * 20;

        public void Save()
        {
            var parts = new List<string>();
            foreach (var kvp in _levels)
            {
                if (kvp.Value > 0)
                    parts.Add($"{kvp.Key}:{kvp.Value}");
            }
            PlayerPrefs.SetString(PrefsKey, string.Join(",", parts));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            _levels.Clear();
            var saved = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(saved)) return;

            foreach (var part in saved.Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out int level))
                {
                    _levels[kv[0]] = level;
                }
            }
        }
    }
}
