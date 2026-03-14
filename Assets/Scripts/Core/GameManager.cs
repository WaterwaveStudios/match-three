using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        private const int BaseRows = 4;
        private const int BaseCols = 4;

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
        private UpgradeManager _upgradeManager;

        // Tooltip state
        private GameObject _tooltipRoot;
        private Text _tooltipText;
        private RectTransform _tooltipRect;
        private RectTransform _shopCanvasRect;
        private bool _tooltipActive;

        // Zoom state
        private RectTransform _mapContainer;
        private float _zoomLevel = 1f;
        private const float MinZoom = 0.4f;
        private const float MaxZoom = 1.5f;

        public WalletManager Wallet => _wallet;
        public UpgradeManager Upgrades => _upgradeManager;

        // Spatial node positions for the upgrade graph
        private static readonly Dictionary<string, Vector2> NodePositions = new Dictionary<string, Vector2>
        {
            // Root
            { "score_boost",    new Vector2(0, 0) },
            // First ring
            { "extra_row",      new Vector2(160, 0) },
            { "extra_col",      new Vector2(-160, 0) },
            { "longer_round",   new Vector2(0, 140) },
            { "cascade_chance", new Vector2(0, -140) },
            // Second ring
            { "quick_swap",     new Vector2(0, 280) },
            { "wild_tile",      new Vector2(320, 60) },
            { "bigger_match",   new Vector2(240, -140) },
            { "wider_match",    new Vector2(-320, 60) },
            { "chain_bonus",    new Vector2(-130, -280) },
            { "combo_frenzy",   new Vector2(0, -280) },
            { "score_rush",     new Vector2(130, -280) },
            // Legendary
            { "time_warp",      new Vector2(65, -400) },
            { "tile_storm",     new Vector2(320, -60) },
        };

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

            _upgradeManager = new UpgradeManager();
            _upgradeManager.Load();
        }

        private void Start()
        {
            ShowMenu();
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    EndRound();
                    return;
                }

                _roundTimer.Tick(Time.deltaTime);
                UpdateTimerDisplay();
            }

            if (State == GameState.Shop)
            {
                // Tooltip follows cursor
                if (_tooltipActive && _tooltipRect != null && _shopCanvasRect != null && Mouse.current != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _shopCanvasRect, Mouse.current.position.ReadValue(), null, out localPoint);
                    _tooltipRect.anchoredPosition = localPoint + new Vector2(0, 50);
                }

                if (Mouse.current != null && _mapContainer != null)
                {
                    // Scroll to zoom
                    float scroll = Mouse.current.scroll.ReadValue().y;
                    if (scroll != 0)
                    {
                        _zoomLevel = Mathf.Clamp(_zoomLevel + scroll * 0.01f, MinZoom, MaxZoom);
                        _mapContainer.localScale = Vector3.one * _zoomLevel;
                    }

                    // Left click drag to pan
                    if (Mouse.current.leftButton.isPressed)
                    {
                        Vector2 delta = Mouse.current.delta.ReadValue();
                        if (delta.sqrMagnitude > 0)
                        {
                            _mapContainer.anchoredPosition += delta;
                        }
                    }
                }
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

            float roundDuration = DefaultRoundDuration + _upgradeManager.BonusRoundTime;
            _roundTimer = new RoundTimer(roundDuration);
            _roundTimer.OnExpired += () => EndRound();

            CreateHUD();
            CreateBoard();
        }

        public void EndRound(string reason = null)
        {
            if (State != GameState.Playing) return;
            State = GameState.RoundEnd;

            int roundScore = _board != null ? _board.ScoreManager.Score : 0;
            _wallet.AddFunds(roundScore);
            _wallet.Save();

            HideHUD();
            CreateRoundEndUI(roundScore, reason);
        }

        public void ShowShop()
        {
            State = GameState.Shop;
            HideRoundEnd();
            DestroyBoard();
            _zoomLevel = 1f;
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
            SetField(_board, "_rows", BaseRows + _upgradeManager.ExtraRows);
            SetField(_board, "_cols", BaseCols + _upgradeManager.ExtraCols);
            SetField(_board, "_cascadeChance", _upgradeManager.CascadeChance);
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
            var title = UIHelper.CreateText(canvas.transform, "Deadlocked", 72, Color.white);
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

            float roundDuration = DefaultRoundDuration + _upgradeManager.BonusRoundTime;

            // Timer (top-centre)
            _timerText = UIHelper.CreateText(canvas.transform, FormatTime(roundDuration), 42, Color.white);
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

        private void CreateRoundEndUI(int roundScore, string reason = null)
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

            string titleText = reason ?? "Round Complete!";
            var title = UIHelper.CreateText(canvas.transform, titleText, 56, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 130), new Vector2(500, 80));

            var scoreColour = new Color(1f, 0.85f, 0.3f);
            var roundScoreText = UIHelper.CreateText(canvas.transform, $"Round Score: {roundScore}", 42, scoreColour);
            UIHelper.SetRect(roundScoreText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50), new Vector2(500, 60));

            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 36, new Color(0.6f, 0.9f, 0.6f));
            UIHelper.SetRect(walletText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -10), new Vector2(500, 50));

            var shopBtn = UIHelper.CreateButton(canvas.transform, "Shop", 32, ShowShop);
            UIHelper.SetRect(shopBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -80), new Vector2(240, 55));

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
            _shopCanvasRect = canvas.GetComponent<RectTransform>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvas.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.14f);
            var bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fixed header: title
            var title = UIHelper.CreateText(canvas.transform, "Upgrades", 48, Color.white);
            UIHelper.SetRect(title.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(400, 60));

            // Fixed header: wallet
            var walletColour = new Color(1f, 0.85f, 0.3f);
            var walletText = UIHelper.CreateText(canvas.transform, $"Wallet: {_wallet.Balance}", 28, walletColour);
            UIHelper.SetRect(walletText.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -70), new Vector2(400, 40));

            // Zoomable map container (centered on screen)
            var container = new GameObject("MapContainer");
            container.transform.SetParent(canvas.transform, false);
            _mapContainer = container.AddComponent<RectTransform>();
            _mapContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContainer.sizeDelta = Vector2.zero;
            _mapContainer.anchoredPosition = new Vector2(0, 20);
            _mapContainer.localScale = Vector3.one * _zoomLevel;

            // Draw connecting lines (behind nodes)
            foreach (var node in UpgradeManager.Tree)
            {
                if (!_upgradeManager.IsVisible(node.Id)) continue;
                if (node.ParentId != null && NodePositions.ContainsKey(node.ParentId))
                {
                    CreateTreeLine(_mapContainer, NodePositions[node.ParentId], NodePositions[node.Id]);
                }
                if (node.RequiredNeighbourId != null && NodePositions.ContainsKey(node.RequiredNeighbourId))
                {
                    CreateTreeLine(_mapContainer, NodePositions[node.RequiredNeighbourId], NodePositions[node.Id]);
                }
            }

            // Draw upgrade nodes
            foreach (var node in UpgradeManager.Tree)
            {
                if (_upgradeManager.IsVisible(node.Id))
                {
                    CreateUpgradeNode(_mapContainer, node);
                }
            }

            // Tooltip (fixed, on canvas not container — doesn't zoom)
            CreateTooltip(canvas.transform);

            // Fixed footer: buttons
            var continueBtn = UIHelper.CreateButton(canvas.transform, "Next Round", 32, StartRound);
            UIHelper.SetRect(continueBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 80), new Vector2(240, 55));

            var menuBtn = UIHelper.CreateButton(canvas.transform, "Main Menu", 24, ShowMenu);
            UIHelper.SetRect(menuBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(200, 45));
        }

        private void CreateUpgradeNode(RectTransform parent, UpgradeNode node)
        {
            var pos = NodePositions[node.Id];
            int level = _upgradeManager.GetLevel(node.Id);
            bool maxed = _upgradeManager.IsMaxed(node.Id);
            int cost = _upgradeManager.GetCost(node.Id);
            bool canPurchase = _upgradeManager.CanPurchase(node.Id);
            bool canAfford = cost > 0 && _wallet.CanAfford(cost);

            // Build label
            string label;
            if (node.IsLegendary && !canPurchase && level == 0)
                label = $"{node.Name}\n???";
            else if (maxed)
                label = $"{node.Name}\nMAX";
            else if (level > 0)
                label = $"{node.Name}\nLv {level}/{node.MaxLevel}";
            else
                label = node.Name;

            var btn = UIHelper.CreateButton(parent, label, 16, () =>
            {
                if (_upgradeManager.Purchase(node.Id, _wallet))
                {
                    _upgradeManager.Save();
                    _wallet.Save();
                    _tooltipActive = false;
                    float savedZoom = _zoomLevel;
                    HideShop();
                    _zoomLevel = savedZoom;
                    CreateShopUI();
                }
            });

            var nodeSize = node.IsLegendary ? new Vector2(150, 55) : new Vector2(130, 50);
            UIHelper.SetRect(btn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, nodeSize);

            // Color by state
            var btnImage = btn.GetComponent<Image>();
            Color nodeColor;
            if (maxed)
                nodeColor = new Color(0.5f, 0.4f, 0.15f); // gold
            else if (node.IsLegendary && !canPurchase)
                nodeColor = new Color(0.3f, 0.15f, 0.35f); // purple (locked legendary)
            else if (level > 0)
                nodeColor = new Color(0.2f, 0.45f, 0.2f); // green
            else if (canPurchase && canAfford)
                nodeColor = new Color(0.2f, 0.3f, 0.5f); // blue
            else
                nodeColor = new Color(0.25f, 0.25f, 0.3f); // dark
            btnImage.color = nodeColor;

            if (!canPurchase || !canAfford || maxed)
            {
                btn.interactable = false;
                var colours = btn.colors;
                colours.disabledColor = nodeColor;
                btn.colors = colours;
            }

            // Hover tooltip
            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            string tooltipContent = BuildTooltipText(node, level, maxed, cost, canPurchase);

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((_) => ShowTooltip(tooltipContent));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        private string BuildTooltipText(UpgradeNode node, int level, bool maxed, int cost, bool canPurchase)
        {
            string text = $"{node.Name}\n{node.Description}\n";

            if (maxed)
                text += $"Level: {level}/{node.MaxLevel} (MAX)";
            else if (level > 0)
                text += $"Level: {level}/{node.MaxLevel}\nNext: {cost} pts";
            else
                text += $"Cost: {cost} pts";

            if (node.IsLegendary && !canPurchase && level == 0)
                text += $"\n\nChallenge: {node.Challenge}";

            return text;
        }

        private void CreateTooltip(Transform parent)
        {
            _tooltipRoot = new GameObject("Tooltip");
            _tooltipRoot.transform.SetParent(parent, false);

            var bgImage = _tooltipRoot.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bgImage.raycastTarget = false;

            _tooltipRect = bgImage.GetComponent<RectTransform>();
            _tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltipRect.sizeDelta = new Vector2(240, 90);
            _tooltipRect.pivot = new Vector2(0.5f, 0);

            _tooltipText = UIHelper.CreateText(_tooltipRoot.transform, "", 14, Color.white);
            _tooltipText.alignment = TextAnchor.MiddleCenter;
            var textRect = _tooltipText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);

            _tooltipRoot.SetActive(false);
            _tooltipActive = false;
        }

        private void ShowTooltip(string text)
        {
            if (_tooltipRoot == null) return;
            _tooltipText.text = text;

            int lineCount = text.Split('\n').Length;
            _tooltipRect.sizeDelta = new Vector2(240, 16 + lineCount * 18);

            _tooltipRoot.SetActive(true);
            _tooltipActive = true;
        }

        private void HideTooltip()
        {
            if (_tooltipRoot == null) return;
            _tooltipRoot.SetActive(false);
            _tooltipActive = false;
        }

        private void CreateTreeLine(RectTransform parent, Vector2 from, Vector2 to)
        {
            var line = new GameObject("TreeLine");
            line.transform.SetParent(parent, false);

            var img = line.AddComponent<Image>();
            img.color = new Color(0.35f, 0.35f, 0.45f);
            img.raycastTarget = false;

            var rect = img.GetComponent<RectTransform>();
            Vector2 dir = to - from;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(distance, 3f);
            rect.anchoredPosition = (from + to) / 2f;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void HideShop()
        {
            if (_shopRoot != null)
            {
                Destroy(_shopRoot);
                _shopRoot = null;
                _tooltipRoot = null;
                _tooltipText = null;
                _tooltipRect = null;
                _shopCanvasRect = null;
                _mapContainer = null;
                _tooltipActive = false;
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

        internal void SubscribeToBoard(GameBoard board)
        {
            board.ScoreManager.BonusPerTile = _upgradeManager.BonusPerTile;
            board.ScoreManager.OnScoreChanged += UpdateScore;
        }

        #endregion
    }
}
