using System;
using UnityEngine;
using UnityEngine.UI;
using MatchThree.Board;
using MatchThree.Tiles;

namespace MatchThree.Core
{
    public enum GameState { Menu, Playing, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }

        private GameObject _tilePrefab;
        private TileData[] _tileDataSet;
        private GameBoard _board;
        private GameObject _menuRoot;
        private GameObject _gameOverRoot;
        private GameObject _hudRoot;
        private Text _scoreText;

        public void Init(GameObject tilePrefab, TileData[] tileDataSet)
        {
            _tilePrefab = tilePrefab;
            _tileDataSet = tileDataSet;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ShowMenu();
        }

        private void Update()
        {
            if (State == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                EndGame();
            }
        }

        #region State Transitions

        public void ShowMenu()
        {
            State = GameState.Menu;
            DestroyBoard();
            HideGameOver();
            HideHUD();
            CreateMenuUI();
        }

        public void StartGame()
        {
            State = GameState.Playing;
            HideMenu();
            CreateHUD();
            CreateBoard();
        }

        public void EndGame()
        {
            State = GameState.GameOver;
            int finalScore = _board != null ? _board.ScoreManager.Score : 0;
            HideHUD();
            CreateGameOverUI(finalScore);
        }

        public void RestartGame()
        {
            HideGameOver();
            DestroyBoard();
            State = GameState.Playing;
            CreateHUD();
            CreateBoard();
        }

        #endregion

        #region Board

        private void CreateBoard()
        {
            var boardGo = new GameObject("GameBoard");
            _board = boardGo.AddComponent<GameBoard>();

            SetField(_board, "_tilePrefab", _tilePrefab);
            SetField(_board, "_tileDataSet", _tileDataSet);
        }

        private void DestroyBoard()
        {
            if (_board != null)
            {
                Destroy(_board.gameObject);
                _board = null;
            }
        }

        #endregion

        #region Menu UI

        private void CreateMenuUI()
        {
            var canvas = UIHelper.CreateCanvas("MenuCanvas");
            _menuRoot = canvas.gameObject;

            // Darken background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvas.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.14f);
            var bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Title
            var title = UIHelper.CreateText(canvas.transform, "Match Three", 72, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(600, 100));

            // Subtitle
            var subtitle = UIHelper.CreateText(canvas.transform, "A classic puzzle game", 24, new Color(0.6f, 0.6f, 0.7f));
            UIHelper.SetRect(subtitle.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(400, 40));

            // Play button
            var playBtn = UIHelper.CreateButton(canvas.transform, "Play", 36, StartGame);
            UIHelper.SetRect(playBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -30), new Vector2(240, 60));

            // Quit button
            var quitBtn = UIHelper.CreateButton(canvas.transform, "Quit", 28, () => Application.Quit());
            UIHelper.SetRect(quitBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -110), new Vector2(200, 50));

            #if UNITY_EDITOR
            quitBtn.onClick.AddListener(() => UnityEditor.EditorApplication.isPlaying = false);
            #endif
        }

        private void HideMenu()
        {
            if (_menuRoot != null)
            {
                Destroy(_menuRoot);
                _menuRoot = null;
            }
        }

        #endregion

        #region HUD

        private void CreateHUD()
        {
            var canvas = UIHelper.CreateCanvas("HUDCanvas");
            _hudRoot = canvas.gameObject;

            _scoreText = UIHelper.CreateText(canvas.transform, "Score: 0", 32, Color.white, TextAnchor.UpperLeft);
            var rect = _scoreText.GetComponent<RectTransform>();
            UIHelper.SetRect(rect,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(300, 60));
            _scoreText.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        }

        private void HideHUD()
        {
            if (_hudRoot != null)
            {
                Destroy(_hudRoot);
                _hudRoot = null;
                _scoreText = null;
            }
        }

        private void UpdateScore(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
        }

        #endregion

        #region Game Over UI

        private void CreateGameOverUI(int finalScore)
        {
            var canvas = UIHelper.CreateCanvas("GameOverCanvas");
            canvas.sortingOrder = 20;
            _gameOverRoot = canvas.gameObject;

            // Semi-transparent overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvas.transform, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            var overlayRect = overlayImage.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Game Over title
            var title = UIHelper.CreateText(canvas.transform, "Game Over", 64, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(500, 80));

            // Final score
            var scoreColour = new Color(1f, 0.85f, 0.3f);
            var score = UIHelper.CreateText(canvas.transform, $"Final Score: {finalScore}", 42, scoreColour);
            UIHelper.SetRect(score.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 40), new Vector2(500, 60));

            // Restart button
            var restartBtn = UIHelper.CreateButton(canvas.transform, "Play Again", 32, RestartGame);
            UIHelper.SetRect(restartBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -50), new Vector2(240, 55));

            // Menu button
            var menuBtn = UIHelper.CreateButton(canvas.transform, "Main Menu", 28, ShowMenu);
            UIHelper.SetRect(menuBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -120), new Vector2(200, 50));
        }

        private void HideGameOver()
        {
            if (_gameOverRoot != null)
            {
                Destroy(_gameOverRoot);
                _gameOverRoot = null;
            }
        }

        #endregion

        #region Helpers

        private void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }

        // Called by board via ScoreManager event
        internal void SubscribeToBoard(GameBoard board)
        {
            board.ScoreManager.OnScoreChanged += UpdateScore;
        }

        #endregion
    }
}
