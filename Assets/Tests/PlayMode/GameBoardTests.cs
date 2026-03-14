using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MatchThree.Board;
using MatchThree.Tiles;

namespace MatchThree.Tests
{
    [TestFixture]
    public class GameBoardTests
    {
        private GameObject _prefab;
        private TileData[] _tileDataSet;
        private GameBoard _board;

        [SetUp]
        public void SetUp()
        {
            // Create tile prefab
            _prefab = new GameObject("TilePrefab");
            _prefab.SetActive(false);
            _prefab.AddComponent<SpriteRenderer>();
            _prefab.AddComponent<Tile>();

            // Create tile data
            _tileDataSet = new TileData[5];
            _tileDataSet[0] = MakeTileData(TileType.Circle, Color.red);
            _tileDataSet[1] = MakeTileData(TileType.Square, Color.blue);
            _tileDataSet[2] = MakeTileData(TileType.Diamond, Color.green);
            _tileDataSet[3] = MakeTileData(TileType.Triangle, Color.yellow);
            _tileDataSet[4] = MakeTileData(TileType.Hexagon, Color.magenta);
        }

        [TearDown]
        public void TearDown()
        {
            if (_board != null)
                Object.Destroy(_board.gameObject);
            if (_prefab != null)
                Object.Destroy(_prefab);
        }

        private GameBoard CreateBoard(int rows = 8, int cols = 8)
        {
            var go = new GameObject("TestBoard");
            var board = go.AddComponent<GameBoard>();

            SetField(board, "_tilePrefab", _prefab);
            SetField(board, "_tileDataSet", _tileDataSet);
            SetField(board, "_rows", rows);
            SetField(board, "_cols", cols);

            _board = board;
            return board;
        }

        private TileData MakeTileData(TileType type, Color colour)
        {
            var data = ScriptableObject.CreateInstance<TileData>();
            data.Type = type;
            data.Colour = colour;
            // No sprite needed for logic tests
            return data;
        }

        private void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(target, value);
        }

        private Tile[,] GetGrid(GameBoard board)
        {
            var field = typeof(GameBoard).GetField("_grid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Tile[,])field.GetValue(board);
        }

        [UnityTest]
        public IEnumerator Board_SpawnsTiles_AfterStart()
        {
            var board = CreateBoard();
            yield return null; // Wait for Start()

            var grid = GetGrid(board);
            Assert.IsNotNull(grid);

            int tileCount = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (grid[r, c] != null)
                        tileCount++;

            Assert.AreEqual(64, tileCount, "Board should have 64 tiles");
        }

        [UnityTest]
        public IEnumerator Board_HasNoInitialMatches()
        {
            var board = CreateBoard();
            yield return null;

            var grid = GetGrid(board);
            var matches = Matching.MatchFinder.FindAllMatches(grid, 8, 8);
            Assert.AreEqual(0, matches.Count, "Initial board should have no matches");
        }

        [UnityTest]
        public IEnumerator Board_AllTilesAreActive()
        {
            var board = CreateBoard();
            yield return null;

            var grid = GetGrid(board);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Assert.IsTrue(grid[r, c].gameObject.activeSelf,
                        $"Tile at ({r},{c}) should be active");
                }
            }
        }

        [UnityTest]
        public IEnumerator Board_TilesHaveCorrectPositions()
        {
            var board = CreateBoard();
            yield return null;

            var grid = GetGrid(board);
            float tileSize = 1.1f;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var expected = new Vector3(c * tileSize, r * tileSize, 0f);
                    var actual = grid[r, c].transform.position;
                    Assert.AreEqual(expected.x, actual.x, 0.01f, $"Tile ({r},{c}) X position");
                    Assert.AreEqual(expected.y, actual.y, 0.01f, $"Tile ({r},{c}) Y position");
                }
            }
        }

        [UnityTest]
        public IEnumerator Board_ScoreManagerExists()
        {
            var board = CreateBoard();
            yield return null;

            Assert.IsNotNull(board.ScoreManager);
            Assert.AreEqual(0, board.ScoreManager.Score);
        }

        [UnityTest]
        public IEnumerator Board_AllFiveTileTypesUsed()
        {
            var board = CreateBoard();
            yield return null;

            var grid = GetGrid(board);
            var typesFound = new HashSet<TileType>();

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    typesFound.Add(grid[r, c].Type);

            Assert.AreEqual(5, typesFound.Count, "All 5 tile types should appear on an 8x8 board");
        }

        [UnityTest]
        public IEnumerator SmallBoard_SpawnsCorrectCount()
        {
            var board = CreateBoard(4, 4);
            yield return null;

            var grid = GetGrid(board);
            int count = 0;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    if (grid[r, c] != null)
                        count++;

            Assert.AreEqual(16, count);
        }
    }
}
