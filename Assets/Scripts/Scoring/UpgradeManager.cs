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

        public int MaxLevel => LevelCosts.Length;

        public UpgradeNode(string id, string name, string description, int[] levelCosts, string parentId)
        {
            Id = id;
            Name = name;
            Description = description;
            LevelCosts = levelCosts;
            ParentId = parentId;
        }
    }

    public class UpgradeManager
    {
        public const string PrefsKey = "MatchThree_Upgrades";

        public static readonly UpgradeNode[] Tree = new[]
        {
            new UpgradeNode("score_boost", "Score Boost", "+1 point per tile per level",
                new[] { 10, 20, 40 }, null),
            new UpgradeNode("extra_row", "Extra Row", "+1 board row per level",
                new[] { 15, 30, 60, 120 }, "score_boost"),
            new UpgradeNode("extra_col", "Extra Column", "+1 board column per level",
                new[] { 15, 30, 60, 120 }, "score_boost"),
            new UpgradeNode("longer_round", "Longer Round", "+1s round duration per level",
                new[] { 20, 40, 80 }, "score_boost"),
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
            return GetLevel(node.ParentId) > 0;
        }

        public bool IsMaxed(string id)
        {
            var node = GetNode(id);
            if (node == null) return false;
            return GetLevel(id) >= node.MaxLevel;
        }

        public bool Purchase(string id, WalletManager wallet)
        {
            var node = GetNode(id);
            if (node == null) return false;
            if (!IsVisible(id)) return false;
            if (IsMaxed(id)) return false;

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
