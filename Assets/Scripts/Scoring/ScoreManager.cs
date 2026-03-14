using System;
using System.Collections.Generic;

namespace MatchThree.Scoring
{
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int ChainMultiplier { get; private set; }
        public int BonusPerTile { get; set; }

        public event Action<int> OnScoreChanged;

        public void ResetChain()
        {
            ChainMultiplier = 0;
        }

        public void IncrementChain()
        {
            ChainMultiplier++;
        }

        public int CalculatePoints(int matchSize)
        {
            return matchSize * (1 + BonusPerTile) * Math.Max(1, ChainMultiplier);
        }

        public void AddMatchScore(int matchSize)
        {
            int points = CalculatePoints(matchSize);
            Score += points;
            OnScoreChanged?.Invoke(Score);
        }

        public void AddMatchScores(List<HashSet<(int row, int col)>> matchGroups)
        {
            foreach (var group in matchGroups)
            {
                AddMatchScore(group.Count);
            }
        }
    }
}
