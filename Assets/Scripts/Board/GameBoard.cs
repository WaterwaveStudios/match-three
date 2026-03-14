using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MatchThree.Tiles;
using MatchThree.Matching;
using MatchThree.Scoring;
using MatchThree.UI;
using MatchThree.Core;

namespace MatchThree.Board
{
    public class GameBoard : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int _rows = 4;
        [SerializeField] private int _cols = 4;
        [SerializeField] private float _tileSize = 1.1f;

        [Header("Tile Setup")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private TileData[] _tileDataSet;

        [Header("Animation")]
        [SerializeField] private float _swapDuration = 0.2f;
        [SerializeField] private float _fallDuration = 0.3f;
        [SerializeField] private float _clearDelay = 0.15f;

        private Tile[,] _grid;
        private Tile _selectedTile;
        private bool _isProcessing;
        private ScoreManager _scoreManager;
        private Camera _mainCamera;
        private int _cascadeChance;

        public ScoreManager ScoreManager => _scoreManager;

        private void Awake()
        {
            _scoreManager = new ScoreManager();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _grid = new Tile[_rows, _cols];
            FillBoard();
            CentreCamera();

            if (GameManager.Instance != null)
                GameManager.Instance.SubscribeToBoard(this);
        }

        private void Update()
        {
            if (_isProcessing) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick();
            }
        }

        private void CentreCamera()
        {
            float x = (_cols - 1) * _tileSize / 2f;
            float y = (_rows - 1) * _tileSize / 2f;
            _mainCamera.transform.position = new Vector3(x, y, -10f);
            _mainCamera.orthographicSize = (_rows * _tileSize) / 2f + 1f;
        }

        #region Board Setup

        private void FillBoard()
        {
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    SpawnTileAt(r, c, false);
                }
            }

            // Ensure no matches on initial board
            while (MatchFinder.HasAnyMatch(_grid, _rows, _cols))
            {
                ClearBoard();
                for (int r = 0; r < _rows; r++)
                {
                    for (int c = 0; c < _cols; c++)
                    {
                        SpawnTileAt(r, c, false);
                    }
                }
            }
        }

        private void ClearBoard()
        {
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (_grid[r, c] != null)
                    {
                        Destroy(_grid[r, c].gameObject);
                        _grid[r, c] = null;
                    }
                }
            }
        }

        private Tile SpawnTileAt(int row, int col, bool aboveBoard, bool allowCascade = false)
        {
            TileData data;
            if (allowCascade)
                data = _tileDataSet[Random.Range(0, _tileDataSet.Length)];
            else
                data = GetSafeTileData(row, col);

            Vector3 pos = GridToWorld(row, col);
            if (aboveBoard)
            {
                pos.y = GridToWorld(_rows, col).y + col * 0.1f;
            }

            GameObject go = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
            go.SetActive(true);
            go.name = $"Tile_{row}_{col}";
            Tile tile = go.GetComponent<Tile>();
            tile.Init(data, row, col);
            _grid[row, col] = tile;
            return tile;
        }

        private TileData GetSafeTileData(int row, int col)
        {
            var available = new List<TileData>(_tileDataSet);

            // Check left pair
            if (col >= 2 && SameTypeAt(row, col - 1, row, col - 2))
                available.RemoveAll(d => d.Type == _grid[row, col - 1].Type);

            // Check right pair
            if (col + 2 < _cols && SameTypeAt(row, col + 1, row, col + 2))
                available.RemoveAll(d => d.Type == _grid[row, col + 1].Type);

            // Check left-right sandwich
            if (col >= 1 && col + 1 < _cols && SameTypeAt(row, col - 1, row, col + 1))
                available.RemoveAll(d => d.Type == _grid[row, col - 1].Type);

            // Check below pair
            if (row >= 2 && SameTypeAt(row - 1, col, row - 2, col))
                available.RemoveAll(d => d.Type == _grid[row - 1, col].Type);

            // Check above pair
            if (row + 2 < _rows && SameTypeAt(row + 1, col, row + 2, col))
                available.RemoveAll(d => d.Type == _grid[row + 1, col].Type);

            // Check below-above sandwich
            if (row >= 1 && row + 1 < _rows && SameTypeAt(row - 1, col, row + 1, col))
                available.RemoveAll(d => d.Type == _grid[row - 1, col].Type);

            if (available.Count == 0)
                available = new List<TileData>(_tileDataSet);

            return available[Random.Range(0, available.Count)];
        }

        private bool SameTypeAt(int r1, int c1, int r2, int c2)
        {
            return _grid[r1, c1] != null && _grid[r2, c2] != null
                && _grid[r1, c1].Type == _grid[r2, c2].Type;
        }

        #endregion

        #region Input

        private void HandleClick()
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            int col = Mathf.RoundToInt(worldPos.x / _tileSize);
            int row = Mathf.RoundToInt(worldPos.y / _tileSize);

            if (row < 0 || row >= _rows || col < 0 || col >= _cols) return;

            Tile clicked = _grid[row, col];
            if (clicked == null) return;

            if (_selectedTile == null)
            {
                _selectedTile = clicked;
                _selectedTile.SetSelected(true);
            }
            else if (_selectedTile == clicked)
            {
                _selectedTile.SetSelected(false);
                _selectedTile = null;
            }
            else if (AreAdjacent(_selectedTile, clicked))
            {
                _selectedTile.SetSelected(false);
                StartCoroutine(TrySwap(_selectedTile, clicked));
                _selectedTile = null;
            }
            else
            {
                _selectedTile.SetSelected(false);
                _selectedTile = clicked;
                _selectedTile.SetSelected(true);
            }
        }

        private bool AreAdjacent(Tile a, Tile b)
        {
            int dr = Mathf.Abs(a.Row - b.Row);
            int dc = Mathf.Abs(a.Col - b.Col);
            return (dr + dc) == 1;
        }

        #endregion

        #region Swap

        private IEnumerator TrySwap(Tile a, Tile b)
        {
            _isProcessing = true;

            yield return StartCoroutine(AnimateSwap(a, b));
            SwapInGrid(a, b);

            if (MatchFinder.HasAnyMatch(_grid, _rows, _cols))
            {
                yield return StartCoroutine(ProcessMatches());
            }
            else
            {
                // Invalid swap — swap back
                yield return StartCoroutine(AnimateSwap(a, b));
                SwapInGrid(a, b);
            }

            // Check for deadlock after board settles
            if (!MatchFinder.HasAnyValidMove(_grid, _rows, _cols))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.EndRound("Deadlocked!");
            }

            _isProcessing = false;
        }

        private void SwapInGrid(Tile a, Tile b)
        {
            _grid[a.Row, a.Col] = b;
            _grid[b.Row, b.Col] = a;

            int tempRow = a.Row;
            int tempCol = a.Col;
            a.Row = b.Row;
            a.Col = b.Col;
            b.Row = tempRow;
            b.Col = tempCol;
        }

        private IEnumerator AnimateSwap(Tile a, Tile b)
        {
            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;
            float elapsed = 0f;

            while (elapsed < _swapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _swapDuration;
                a.transform.position = Vector3.Lerp(posA, posB, t);
                b.transform.position = Vector3.Lerp(posB, posA, t);
                yield return null;
            }

            a.transform.position = posB;
            b.transform.position = posA;
        }

        #endregion

        #region Match Processing

        private IEnumerator ProcessMatches()
        {
            _scoreManager.ResetChain();

            while (true)
            {
                var matchGroups = MatchFinder.FindAllMatches(_grid, _rows, _cols);
                if (matchGroups.Count == 0) break;

                _scoreManager.IncrementChain();

                // Score each group and spawn popups
                var allMatched = new HashSet<(int row, int col)>();
                foreach (var group in matchGroups)
                {
                    int points = _scoreManager.CalculatePoints(group.Count);
                    _scoreManager.AddMatchScore(group.Count);
                    SpawnScorePopup(group, points);

                    foreach (var pos in group)
                    {
                        allMatched.Add(pos);
                    }
                }

                // Destroy matched tiles
                foreach (var (row, col) in allMatched)
                {
                    if (_grid[row, col] != null)
                    {
                        Destroy(_grid[row, col].gameObject);
                        _grid[row, col] = null;
                    }
                }

                yield return new WaitForSeconds(_clearDelay);

                // Collapse tiles down
                yield return StartCoroutine(CollapseTiles());

                // Refill empty spaces
                yield return StartCoroutine(RefillBoard());
            }
        }

        private IEnumerator CollapseTiles()
        {
            bool moved = false;

            for (int c = 0; c < _cols; c++)
            {
                int writeRow = 0;
                for (int r = 0; r < _rows; r++)
                {
                    if (_grid[r, c] != null)
                    {
                        if (r != writeRow)
                        {
                            _grid[writeRow, c] = _grid[r, c];
                            _grid[r, c] = null;
                            _grid[writeRow, c].Row = writeRow;
                            moved = true;
                        }
                        writeRow++;
                    }
                }
            }

            if (moved)
            {
                yield return StartCoroutine(AnimateAllToPosition());
            }
        }

        private IEnumerator RefillBoard()
        {
            var newTiles = new List<Tile>();

            for (int c = 0; c < _cols; c++)
            {
                for (int r = 0; r < _rows; r++)
                {
                    if (_grid[r, c] == null)
                    {
                        bool allowCascade = _cascadeChance > 0 && Random.Range(0, 100) < _cascadeChance;
                        Tile tile = SpawnTileAt(r, c, true, allowCascade);
                        newTiles.Add(tile);
                    }
                }
            }

            if (newTiles.Count > 0)
            {
                yield return StartCoroutine(AnimateAllToPosition());
            }
        }

        private IEnumerator AnimateAllToPosition()
        {
            var moves = new List<(Transform tf, Vector3 start, Vector3 end)>();

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (_grid[r, c] != null)
                    {
                        Vector3 target = GridToWorld(r, c);
                        if (Vector3.Distance(_grid[r, c].transform.position, target) > 0.01f)
                        {
                            moves.Add((_grid[r, c].transform, _grid[r, c].transform.position, target));
                        }
                    }
                }
            }

            if (moves.Count == 0) yield break;

            float elapsed = 0f;
            float duration = _fallDuration * 3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                float eased = 1f - (1f - t) * (1f - t);

                foreach (var (tf, start, end) in moves)
                {
                    if (tf != null)
                        tf.position = Vector3.Lerp(start, end, eased);
                }
                yield return null;
            }

            foreach (var (tf, _, end) in moves)
            {
                if (tf != null)
                    tf.position = end;
            }
        }

        #endregion

        #region Helpers

        private Vector3 GridToWorld(int row, int col)
        {
            return new Vector3(col * _tileSize, row * _tileSize, 0f);
        }

        private void SpawnScorePopup(HashSet<(int row, int col)> group, int points)
        {
            // Find centre of the match group
            float sumX = 0f, sumY = 0f;
            foreach (var (row, col) in group)
            {
                var world = GridToWorld(row, col);
                sumX += world.x;
                sumY += world.y;
            }
            var centre = new Vector3(sumX / group.Count, sumY / group.Count, -1f);

            var go = new GameObject("ScorePopup");
            go.transform.position = centre;
            var popup = go.AddComponent<ScorePopup>();
            popup.Init(points);
        }

        #endregion
    }
}
