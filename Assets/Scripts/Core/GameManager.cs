using System;
using UnityEngine;
using UnityEngine.UI;
using MatchThree.Board;
using MatchThree.Tiles;
using MatchThree.Scoring;

namespace MatchThree.Core
{
    public enum GameState { Menu, Playing, RoundEnd, Shop }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }

        private const float DefaultRoundDuration = 20f;

        private GameObject _tilePrefab;
        private TileData[] _tileDataSet;
        private GameBoard _board;
        private GameObject _menuRoot;
        private GameObject _roundEndRoot;
        private GameObject _shopRoot;
        private GameObject _hudRoot;
        private Text _scoreText;
        private Text _timerText;

        private RoundTimer _roundTimer;
        private WalletManager _wallet;

        public WalletManager Wallet => _wallet;

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

            _wallet = new WalletManager();
            _wallet.Load();
        }

        private void Start()
        {
            ShowMenu();
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    EndRound();
                    return;
                }

                _roundTimer.Tick(Time.deltaTime);
                UpdateTimerDisplay();
            }
        }

        #region State Transitions

        public void ShowMenu()
        {
            State = GameState.Menu;
            DestroyBoard();
            HideRoundEnd();
            HideShop();
            HideHUD();
            CreateMenuUI();
        }

        public void StartRound()
        {
            State = GameState.Playing;
            HideMenu();
            HideShop();

            _roundTimer = new RoundTimer(DefaultRoundDuration);
            _roundTimer.OnExpired += EndRound;

            CreateHUD();
            CreateBoard();
        }

        public void EndRound()
        {
            if (State != GameState.Playing) return;
            State = GameState.RoundEnd;

            int roundScore = _board != null ? _board.ScoreManager.Score : 0;
            _wallet.AddFunds(roundScore);
            _wallet.Save();

            HideHUD();
            CreateRoundEndUI(roundScore);
        }

        public void ShowShop()
        {
            State = GameState.Shop;
            HideRoundEnd();
            DestroyBoard();
            CreateShopUI();
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
            var subtitle = UIHelper.CreateText(canvas.transform, "An incremental puzzle game", 24, new Color(0.6f, 0.6f, 0.7f));
            UIHelper.SetRect(subtitle.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(400, 40));

            // Wallet display
            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 28, new Color(1f, 0.85f, 0.3f));
            UIHelper.SetRect(walletText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 10), new Vector2(400, 40));

            // Play button
            var playBtn = UIHelper.CreateButton(canvas.transform, "Play", 36, StartRound);
            UIHelper.SetRect(playBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -50), new Vector2(240, 60));

            // Quit button
            var quitBtn = UIHelper.CreateButton(canvas.transform, "Quit", 28, () => Application.Quit());
            UIHelper.SetRect(quitBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -130), new Vector2(200, 50));

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

            // Score (top-left)
            _scoreText = UIHelper.CreateText(canvas.transform, "Score: 0", 32, Color.white, TextAnchor.UpperLeft);
            var scoreRect = _scoreText.GetComponent<RectTransform>();
            UIHelper.SetRect(scoreRect,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(300, 60));
            scoreRect.pivot = new Vector2(0, 1);

            // Timer (top-centre)
            _timerText = UIHelper.CreateText(canvas.transform, FormatTime(DefaultRoundDuration), 42, Color.white);
            var timerRect = _timerText.GetComponent<RectTransform>();
            UIHelper.SetRect(timerRect,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(200, 60));
            timerRect.pivot = new Vector2(0.5f, 1);

            // Wallet (top-right)
            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 24, new Color(1f, 0.85f, 0.3f), TextAnchor.UpperRight);
            var walletRect = walletText.GetComponent<RectTransform>();
            UIHelper.SetRect(walletRect,
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(300, 60));
            walletRect.pivot = new Vector2(1, 1);
        }

        private void HideHUD()
        {
            if (_hudRoot != null)
            {
                Destroy(_hudRoot);
                _hudRoot = null;
                _scoreText = null;
                _timerText = null;
            }
        }

        private void UpdateScore(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
        }

        private void UpdateTimerDisplay()
        {
            if (_timerText == null || _roundTimer == null) return;

            _timerText.text = FormatTime(_roundTimer.TimeRemaining);

            // Turn red when low
            if (_roundTimer.TimeRemaining <= 5f)
                _timerText.color = new Color(1f, 0.3f, 0.3f);
            else
                _timerText.color = Color.white;
        }

        private string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{s}s";
        }

        #endregion

        #region Round End UI

        private void CreateRoundEndUI(int roundScore)
        {
            var canvas = UIHelper.CreateCanvas("RoundEndCanvas");
            canvas.sortingOrder = 20;
            _roundEndRoot = canvas.gameObject;

            // Semi-transparent overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvas.transform, false);
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            var overlayRect = overlayImage.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Round Complete title
            var title = UIHelper.CreateText(canvas.transform, "Round Complete!", 56, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 130), new Vector2(500, 80));

            // Round score
            var scoreColour = new Color(1f, 0.85f, 0.3f);
            var roundScoreText = UIHelper.CreateText(canvas.transform, $"Round Score: {roundScore}", 42, scoreColour);
            UIHelper.SetRect(roundScoreText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50), new Vector2(500, 60));

            // Wallet total
            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 36, new Color(0.6f, 0.9f, 0.6f));
            UIHelper.SetRect(walletText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -10), new Vector2(500, 50));

            // Continue to shop button
            var shopBtn = UIHelper.CreateButton(canvas.transform, "Shop", 32, ShowShop);
            UIHelper.SetRect(shopBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -80), new Vector2(240, 55));

            // Main menu button
            var menuBtn = UIHelper.CreateButton(canvas.transform, "Main Menu", 24, ShowMenu);
            UIHelper.SetRect(menuBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -150), new Vector2(200, 45));
        }

        private void HideRoundEnd()
        {
            if (_roundEndRoot != null)
            {
                Destroy(_roundEndRoot);
                _roundEndRoot = null;
            }
        }

        #endregion

        #region Shop UI

        private void CreateShopUI()
        {
            var canvas = UIHelper.CreateCanvas("ShopCanvas");
            _shopRoot = canvas.gameObject;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvas.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.14f);
            var bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Shop title
            var title = UIHelper.CreateText(canvas.transform, "Shop", 56, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 160), new Vector2(400, 80));

            // Wallet display
            var walletColour = new Color(1f, 0.85f, 0.3f);
            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 36, walletColour);
            UIHelper.SetRect(walletText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 80), new Vector2(400, 50));

            // Coming soon placeholder
            var placeholder = UIHelper.CreateText(canvas.transform, "Upgrades coming soon...", 24, new Color(0.5f, 0.5f, 0.6f));
            UIHelper.SetRect(placeholder.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 10), new Vector2(400, 40));

            // Continue button (starts next round)
            var continueBtn = UIHelper.CreateButton(canvas.transform, "Next Round", 32, StartRound);
            UIHelper.SetRect(continueBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(240, 55));

            // Main menu button
            var menuBtn = UIHelper.CreateButton(canvas.transform, "Main Menu", 24, ShowMenu);
            UIHelper.SetRect(menuBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -130), new Vector2(200, 45));
        }

        private void HideShop()
        {
            if (_shopRoot != null)
            {
                Destroy(_shopRoot);
                _shopRoot = null;
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
