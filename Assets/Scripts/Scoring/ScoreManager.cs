using System;
using System.Collections.Generic;

namespace MatchThree.Scoring
{
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int ChainMultiplier { get; private set; }

        public event Action<int> OnScoreChanged;

        public void ResetChain()
        {
            ChainMultiplier = 0;
        }

        public void IncrementChain()
        {
            ChainMultiplier++;
        }

        public void AddMatchScore(int matchSize)
        {
            int basePoints;
            if (matchSize <= 3)
                basePoints = 50;
            else if (matchSize == 4)
                basePoints = 150;
            else
                basePoints = 300;

            int multiplier = Math.Max(1, ChainMultiplier);
            int points = basePoints * multiplier;
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
