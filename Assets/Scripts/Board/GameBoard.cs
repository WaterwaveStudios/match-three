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

        // Idle hint
        private float _idleTime;
        private Tile _hintTileA;
        private Tile _hintTileB;
        private const float HintDelay = 5f;

        // Drag state
        private Tile _dragTile;
        private Tile _dragTargetTile;
        private Vector3 _dragStartWorldPos;
        private bool _isDragging;

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
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ClearHint();
                _idleTime = 0f;
                HandleDragStart();
            }
            else if (Mouse.current.leftButton.isPressed && _dragTile != null)
            {
                HandleDragUpdate();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && _dragTile != null)
            {
                HandleDragEnd();
            }
            else
            {
                // Idle hint
                _idleTime += Time.deltaTime;
                if (_idleTime >= HintDelay && _hintTileA == null)
                    ShowHint();
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

        private Tile SpawnTileAt(int row, int col, bool aboveBoard, bool allowCascade = false, int spawnIndex = 0)
        {
            TileData data;
            if (allowCascade)
                data = _tileDataSet[Random.Range(0, _tileDataSet.Length)];
            else
                data = GetSafeTileData(row, col);

            Vector3 pos = GridToWorld(row, col);
            if (aboveBoard)
            {
                pos.y = GridToWorld(_rows, col).y + spawnIndex * _tileSize;
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

        private void HandleDragStart()
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            int col = Mathf.RoundToInt(worldPos.x / _tileSize);
            int row = Mathf.RoundToInt(worldPos.y / _tileSize);

            if (row < 0 || row >= _rows || col < 0 || col >= _cols) return;

            Tile clicked = _grid[row, col];
            if (clicked == null) return;

            _dragTile = clicked;
            _dragStartWorldPos = GridToWorld(row, col);
            _dragTargetTile = null;
            _isDragging = false;
        }

        private void HandleDragUpdate()
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 delta = worldPos - (Vector2)_dragStartWorldPos;

            if (!_isDragging)
            {
                if (delta.magnitude < _tileSize * 0.15f) return;
                _isDragging = true;

                // Clear selection — we're dragging now
                if (_selectedTile != null)
                {
                    _selectedTile.SetSelected(false);
                    _selectedTile = null;
                }

                // Bring dragged tile to front
                var pos = _dragTile.transform.position;
                pos.z = -0.5f;
                _dragTile.transform.position = pos;
            }

            // Lock to primary axis
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            int targetRow = _dragTile.Row;
            int targetCol = _dragTile.Col;

            if (absX > absY)
            {
                float clampedX = Mathf.Clamp(delta.x, -_tileSize, _tileSize);
                _dragTile.transform.position = _dragStartWorldPos + new Vector3(clampedX, 0, -0.5f);
                targetCol += delta.x > 0 ? 1 : -1;
            }
            else
            {
                float clampedY = Mathf.Clamp(delta.y, -_tileSize, _tileSize);
                _dragTile.transform.position = _dragStartWorldPos + new Vector3(0, clampedY, -0.5f);
                targetRow += delta.y > 0 ? 1 : -1;
            }

            // Reset old target if direction changed
            if (_dragTargetTile != null)
            {
                Tile newTarget = (targetRow >= 0 && targetRow < _rows && targetCol >= 0 && targetCol < _cols)
                    ? _grid[targetRow, targetCol] : null;
                if (_dragTargetTile != newTarget)
                {
                    _dragTargetTile.transform.position = GridToWorld(_dragTargetTile.Row, _dragTargetTile.Col);
                    _dragTargetTile = null;
                }
            }

            // Slide target tile toward dragged tile's original position
            if (targetRow >= 0 && targetRow < _rows && targetCol >= 0 && targetCol < _cols)
            {
                _dragTargetTile = _grid[targetRow, targetCol];
                if (_dragTargetTile != null)
                {
                    float t = Mathf.Clamp01(Mathf.Max(absX, absY) / _tileSize);
                    Vector3 targetOrigPos = GridToWorld(_dragTargetTile.Row, _dragTargetTile.Col);
                    _dragTargetTile.transform.position = Vector3.Lerp(targetOrigPos, _dragStartWorldPos, t);
                }
            }
        }

        private void HandleDragEnd()
        {
            if (!_isDragging)
            {
                // Short click — use click-to-select fallback
                HandleTileClick(_dragTile);
                _dragTile = null;
                return;
            }

            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 delta = worldPos - (Vector2)_dragStartWorldPos;
            bool draggedFarEnough = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) > _tileSize * 0.4f;

            if (draggedFarEnough && _dragTargetTile != null)
            {
                // Snap to final positions
                _dragTile.transform.position = GridToWorld(_dragTargetTile.Row, _dragTargetTile.Col);
                _dragTargetTile.transform.position = _dragStartWorldPos;

                if (_selectedTile != null)
                {
                    _selectedTile.SetSelected(false);
                    _selectedTile = null;
                }

                StartCoroutine(TrySwapFromDrag(_dragTile, _dragTargetTile));
            }
            else
            {
                // Snap back
                _dragTile.transform.position = _dragStartWorldPos;
                if (_dragTargetTile != null)
                    _dragTargetTile.transform.position = GridToWorld(_dragTargetTile.Row, _dragTargetTile.Col);
            }

            _dragTile = null;
            _dragTargetTile = null;
            _isDragging = false;
        }

        private void HandleTileClick(Tile clicked)
        {
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

        private IEnumerator TrySwapFromDrag(Tile a, Tile b)
        {
            _isProcessing = true;
            ClearHint();
            _idleTime = 0f;

            // Tiles are already visually swapped from drag
            SwapInGrid(a, b);

            if (MatchFinder.HasAnyMatch(_grid, _rows, _cols))
            {
                yield return StartCoroutine(ProcessMatches());
            }
            else
            {
                // Invalid swap — animate back
                yield return StartCoroutine(AnimateSwap(a, b));
                SwapInGrid(a, b);
            }

            if (!MatchFinder.HasAnyValidMove(_grid, _rows, _cols))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.EndRound("Deadlocked!");
            }

            _isProcessing = false;
        }

        private IEnumerator TrySwap(Tile a, Tile b)
        {
            _isProcessing = true;
            ClearHint();
            _idleTime = 0f;

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

                // Collapse and refill simultaneously
                yield return StartCoroutine(CollapseAndRefill());
            }
        }

        private IEnumerator CollapseAndRefill()
        {
            // Step 1: Collapse grid positions (no animation yet)
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
                        }
                        writeRow++;
                    }
                }
            }

            // Step 2: Spawn new tiles above the board
            for (int c = 0; c < _cols; c++)
            {
                int spawnIndex = 0;
                for (int r = 0; r < _rows; r++)
                {
                    if (_grid[r, c] == null)
                    {
                        bool allowCascade = _cascadeChance > 0 && Random.Range(0, 100) < _cascadeChance;
                        SpawnTileAt(r, c, true, allowCascade, spawnIndex);
                        spawnIndex++;
                    }
                }
            }

            // Step 3: Animate everything to position at once
            yield return StartCoroutine(AnimateAllToPosition());
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

        private void ShowHint()
        {
            var move = MatchFinder.FindFirstValidMove(_grid, _rows, _cols);
            if (move == null) return;

            var (r1, c1, r2, c2) = move.Value;
            _hintTileA = _grid[r1, c1];
            _hintTileB = _grid[r2, c2];

            if (_hintTileA != null) _hintTileA.SetHinted(true);
            if (_hintTileB != null) _hintTileB.SetHinted(true);
        }

        private void ClearHint()
        {
            if (_hintTileA != null) _hintTileA.SetHinted(false);
            if (_hintTileB != null) _hintTileB.SetHinted(false);
            _hintTileA = null;
            _hintTileB = null;
        }

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
