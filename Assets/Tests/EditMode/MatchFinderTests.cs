using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MatchThree.Tiles;
using MatchThree.Matching;

namespace MatchThree.Tests
{
    [TestFixture]
    public class MatchFinderTests
    {
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
        }

        private Tile MakeTile(TileType type, int row, int col)
        {
            var go = new GameObject($"Tile_{row}_{col}");
            go.AddComponent<SpriteRenderer>();
            var tile = go.AddComponent<Tile>();

            var data = ScriptableObject.CreateInstance<TileData>();
            data.Type = type;
            data.Colour = Color.white;
            tile.Init(data, row, col);

            _created.Add(go);
            return tile;
        }

        private Tile[,] BuildGrid(TileType[,] layout)
        {
            int rows = layout.GetLength(0);
            int cols = layout.GetLength(1);
            var grid = new Tile[rows, cols];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    grid[r, c] = MakeTile(layout[r, c], r, c);

            return grid;
        }

        [Test]
        public void NoMatch_WhenNoThreeInARow()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Square, TileType.Circle, TileType.Square },
                { TileType.Square, TileType.Circle, TileType.Square, TileType.Circle },
                { TileType.Circle, TileType.Square, TileType.Circle, TileType.Square },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 3, 4);

            Assert.AreEqual(0, matches.Count);
        }

        [Test]
        public void FindsHorizontalMatchOfThree()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Circle, TileType.Square },
                { TileType.Square, TileType.Diamond, TileType.Square, TileType.Diamond },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 2, 4);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(3, matches[0].Count);
            Assert.IsTrue(matches[0].Contains((0, 0)));
            Assert.IsTrue(matches[0].Contains((0, 1)));
            Assert.IsTrue(matches[0].Contains((0, 2)));
        }

        [Test]
        public void FindsVerticalMatchOfThree()
        {
            var layout = new TileType[,]
            {
                { TileType.Diamond, TileType.Square },
                { TileType.Diamond, TileType.Circle },
                { TileType.Diamond, TileType.Square },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 3, 2);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(3, matches[0].Count);
            Assert.IsTrue(matches[0].Contains((0, 0)));
            Assert.IsTrue(matches[0].Contains((1, 0)));
            Assert.IsTrue(matches[0].Contains((2, 0)));
        }

        [Test]
        public void FindsMatchOfFour()
        {
            var layout = new TileType[,]
            {
                { TileType.Triangle, TileType.Triangle, TileType.Triangle, TileType.Triangle },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 1, 4);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(4, matches[0].Count);
        }

        [Test]
        public void FindsMatchOfFive()
        {
            var layout = new TileType[,]
            {
                { TileType.Hexagon, TileType.Hexagon, TileType.Hexagon, TileType.Hexagon, TileType.Hexagon },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 1, 5);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(5, matches[0].Count);
        }

        [Test]
        public void FindsMultipleMatches()
        {
            // Row 0: three circles, Row 1: three squares
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Circle },
                { TileType.Square, TileType.Square, TileType.Square },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 2, 3);

            Assert.AreEqual(2, matches.Count);
        }

        [Test]
        public void FindsCrossMatch_BothDirections()
        {
            // Horizontal match on row 1, vertical match on col 1
            var layout = new TileType[,]
            {
                { TileType.Square,   TileType.Circle,  TileType.Diamond },
                { TileType.Circle,   TileType.Circle,  TileType.Circle  },
                { TileType.Diamond,  TileType.Circle,  TileType.Square  },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 3, 3);

            // Should find horizontal (row 1) and vertical (col 1)
            Assert.AreEqual(2, matches.Count);

            var allPositions = new HashSet<(int, int)>();
            foreach (var group in matches)
                foreach (var pos in group)
                    allPositions.Add(pos);

            // Centre tile (1,1) should appear in both
            Assert.IsTrue(allPositions.Contains((1, 1)));
            Assert.AreEqual(5, allPositions.Count); // 3 + 3 - 1 shared
        }

        [Test]
        public void HasAnyMatch_ReturnsTrueWhenMatchExists()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Circle },
            };

            var grid = BuildGrid(layout);
            Assert.IsTrue(MatchFinder.HasAnyMatch(grid, 1, 3));
        }

        [Test]
        public void HasAnyMatch_ReturnsFalseWhenNoMatch()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Square, TileType.Circle },
            };

            var grid = BuildGrid(layout);
            Assert.IsFalse(MatchFinder.HasAnyMatch(grid, 1, 3));
        }

        [Test]
        public void HandlesNullCellsWithoutError()
        {
            var grid = new Tile[3, 3];
            // All nulls
            var matches = MatchFinder.FindAllMatches(grid, 3, 3);
            Assert.AreEqual(0, matches.Count);
        }

        [Test]
        public void HandlesNullGaps()
        {
            var grid = new Tile[1, 5];
            grid[0, 0] = MakeTile(TileType.Circle, 0, 0);
            grid[0, 1] = MakeTile(TileType.Circle, 0, 1);
            // grid[0, 2] = null (gap)
            grid[0, 3] = MakeTile(TileType.Circle, 0, 3);
            grid[0, 4] = MakeTile(TileType.Circle, 0, 4);

            var matches = MatchFinder.FindAllMatches(grid, 1, 5);
            Assert.AreEqual(0, matches.Count);
        }

        [Test]
        public void MatchAtEndOfRow()
        {
            var layout = new TileType[,]
            {
                { TileType.Square, TileType.Circle, TileType.Circle, TileType.Circle },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 1, 4);

            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches[0].Contains((0, 1)));
            Assert.IsTrue(matches[0].Contains((0, 2)));
            Assert.IsTrue(matches[0].Contains((0, 3)));
        }

        [Test]
        public void MatchAtEndOfColumn()
        {
            var layout = new TileType[,]
            {
                { TileType.Square },
                { TileType.Circle },
                { TileType.Circle },
                { TileType.Circle },
            };

            var grid = BuildGrid(layout);
            var matches = MatchFinder.FindAllMatches(grid, 4, 1);

            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches[0].Contains((1, 0)));
            Assert.IsTrue(matches[0].Contains((2, 0)));
            Assert.IsTrue(matches[0].Contains((3, 0)));
        }

        // HasAnyValidMove tests

        [Test]
        public void HasAnyValidMove_ReturnsTrueWhenMoveExists()
        {
            // Swapping (0,2)S with (1,2)C completes row 0: C,C,C
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Square },
                { TileType.Circle, TileType.Square, TileType.Circle },
                { TileType.Square, TileType.Circle, TileType.Square },
            };

            var grid = BuildGrid(layout);
            Assert.IsTrue(MatchFinder.HasAnyValidMove(grid, 3, 3));
        }

        [Test]
        public void HasAnyValidMove_ReturnsFalseWhenDeadlocked()
        {
            // Rotating 3-type pattern — no swap creates a 3-match
            var layout = new TileType[,]
            {
                { TileType.Circle,  TileType.Square,  TileType.Diamond, TileType.Circle  },
                { TileType.Square,  TileType.Diamond, TileType.Circle,  TileType.Square  },
                { TileType.Diamond, TileType.Circle,  TileType.Square,  TileType.Diamond },
                { TileType.Circle,  TileType.Square,  TileType.Diamond, TileType.Circle  },
            };

            var grid = BuildGrid(layout);
            Assert.IsFalse(MatchFinder.HasAnyValidMove(grid, 4, 4));
        }

        [Test]
        public void HasAnyValidMove_HandlesNullCells()
        {
            var grid = new Tile[3, 3];
            // All nulls — no valid moves
            Assert.IsFalse(MatchFinder.HasAnyValidMove(grid, 3, 3));
        }

        [Test]
        public void HasAnyValidMove_DoesNotMutateGrid()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Square },
                { TileType.Circle, TileType.Square, TileType.Circle },
                { TileType.Square, TileType.Circle, TileType.Square },
            };

            var grid = BuildGrid(layout);

            // Record original positions
            var original = new TileType[3, 3];
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    original[r, c] = grid[r, c].Type;

            MatchFinder.HasAnyValidMove(grid, 3, 3);

            // Grid should be unchanged
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    Assert.AreEqual(original[r, c], grid[r, c].Type);
        }

        // FindFirstValidMove tests

        [Test]
        public void FindFirstValidMove_ReturnsMovePair()
        {
            // Swapping (0,2)S with (1,2)C completes row 0: C,C,C
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Square },
                { TileType.Circle, TileType.Square, TileType.Circle },
                { TileType.Square, TileType.Circle, TileType.Square },
            };

            var grid = BuildGrid(layout);
            var move = MatchFinder.FindFirstValidMove(grid, 3, 3);

            Assert.IsNotNull(move);
            var (r1, c1, r2, c2) = move.Value;
            // Verify it's a real adjacent swap
            Assert.AreEqual(1, Mathf.Abs(r1 - r2) + Mathf.Abs(c1 - c2));
        }

        [Test]
        public void FindFirstValidMove_ReturnsNullWhenDeadlocked()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle,  TileType.Square,  TileType.Diamond, TileType.Circle  },
                { TileType.Square,  TileType.Diamond, TileType.Circle,  TileType.Square  },
                { TileType.Diamond, TileType.Circle,  TileType.Square,  TileType.Diamond },
                { TileType.Circle,  TileType.Square,  TileType.Diamond, TileType.Circle  },
            };

            var grid = BuildGrid(layout);
            Assert.IsNull(MatchFinder.FindFirstValidMove(grid, 4, 4));
        }

        [Test]
        public void FindFirstValidMove_ReturnedSwapProducesMatch()
        {
            var layout = new TileType[,]
            {
                { TileType.Circle, TileType.Circle, TileType.Square },
                { TileType.Circle, TileType.Square, TileType.Circle },
                { TileType.Square, TileType.Circle, TileType.Square },
            };

            var grid = BuildGrid(layout);
            var move = MatchFinder.FindFirstValidMove(grid, 3, 3);
            Assert.IsNotNull(move);

            // Perform the swap and verify it creates a match
            var (r1, c1, r2, c2) = move.Value;
            var temp = grid[r1, c1];
            grid[r1, c1] = grid[r2, c2];
            grid[r2, c2] = temp;

            Assert.IsTrue(MatchFinder.HasAnyMatch(grid, 3, 3));
        }
    }
}
