using System.Collections.Generic;
using NUnit.Framework;
using MatchThree.Scoring;

namespace MatchThree.Tests
{
    [TestFixture]
    public class ScoreManagerTests
    {
        private ScoreManager _score;

        [SetUp]
        public void SetUp()
        {
            _score = new ScoreManager();
        }

        [Test]
        public void StartsAtZero()
        {
            Assert.AreEqual(0, _score.Score);
            Assert.AreEqual(0, _score.ChainMultiplier);
        }

        [Test]
        public void ThreeMatch_Awards50Points()
        {
            _score.IncrementChain();
            _score.AddMatchScore(3);
            Assert.AreEqual(50, _score.Score);
        }

        [Test]
        public void FourMatch_Awards150Points()
        {
            _score.IncrementChain();
            _score.AddMatchScore(4);
            Assert.AreEqual(150, _score.Score);
        }

        [Test]
        public void FiveMatch_Awards300Points()
        {
            _score.IncrementChain();
            _score.AddMatchScore(5);
            Assert.AreEqual(300, _score.Score);
        }

        [Test]
        public void SixMatch_Awards300Points_SameAsFive()
        {
            _score.IncrementChain();
            _score.AddMatchScore(6);
            Assert.AreEqual(300, _score.Score);
        }

        [Test]
        public void ChainMultiplier_IncreasesScore()
        {
            // Chain 1: 50 points
            _score.IncrementChain();
            _score.AddMatchScore(3);
            Assert.AreEqual(50, _score.Score);

            // Chain 2: 50 * 2 = 100
            _score.IncrementChain();
            _score.AddMatchScore(3);
            Assert.AreEqual(150, _score.Score); // 50 + 100
        }

        [Test]
        public void ChainMultiplier_TripleChain()
        {
            _score.IncrementChain(); // chain = 1
            _score.AddMatchScore(3); // 50 * 1 = 50

            _score.IncrementChain(); // chain = 2
            _score.AddMatchScore(3); // 50 * 2 = 100

            _score.IncrementChain(); // chain = 3
            _score.AddMatchScore(3); // 50 * 3 = 150

            Assert.AreEqual(300, _score.Score); // 50 + 100 + 150
        }

        [Test]
        public void ResetChain_ResetsMultiplier()
        {
            _score.IncrementChain();
            _score.IncrementChain();
            Assert.AreEqual(2, _score.ChainMultiplier);

            _score.ResetChain();
            Assert.AreEqual(0, _score.ChainMultiplier);
        }

        [Test]
        public void ResetChain_DoesNotResetScore()
        {
            _score.IncrementChain();
            _score.AddMatchScore(3);
            _score.ResetChain();

            Assert.AreEqual(50, _score.Score);
        }

        [Test]
        public void AddMatchScore_WithZeroChain_UsesMultiplierOfOne()
        {
            // Chain multiplier is 0, but Math.Max(1, 0) = 1
            _score.AddMatchScore(3);
            Assert.AreEqual(50, _score.Score);
        }

        [Test]
        public void AddMatchScores_MultipleGroups()
        {
            _score.IncrementChain();

            var groups = new List<HashSet<(int row, int col)>>
            {
                new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) },       // 3-match: 50
                new HashSet<(int, int)> { (1, 0), (1, 1), (1, 2), (1, 3) } // 4-match: 150
            };

            _score.AddMatchScores(groups);
            Assert.AreEqual(200, _score.Score); // 50 + 150
        }

        [Test]
        public void OnScoreChanged_FiresOnEveryScoreUpdate()
        {
            int fireCount = 0;
            int lastScore = -1;
            _score.OnScoreChanged += s => { fireCount++; lastScore = s; };

            _score.IncrementChain();
            _score.AddMatchScore(3);

            Assert.AreEqual(1, fireCount);
            Assert.AreEqual(50, lastScore);
        }

        [Test]
        public void OnScoreChanged_FiresForEachGroupInAddMatchScores()
        {
            var scores = new List<int>();
            _score.OnScoreChanged += s => scores.Add(s);

            _score.IncrementChain();
            var groups = new List<HashSet<(int row, int col)>>
            {
                new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) },
                new HashSet<(int, int)> { (1, 0), (1, 1), (1, 2) },
            };

            _score.AddMatchScores(groups);

            Assert.AreEqual(2, scores.Count);
            Assert.AreEqual(50, scores[0]);
            Assert.AreEqual(100, scores[1]);
        }

        [Test]
        public void ScoreAccumulates_AcrossChains()
        {
            // First swap chain
            _score.ResetChain();
            _score.IncrementChain();
            _score.AddMatchScore(3); // 50

            // Second swap chain
            _score.ResetChain();
            _score.IncrementChain();
            _score.AddMatchScore(4); // 150

            Assert.AreEqual(200, _score.Score);
        }
    }
}
