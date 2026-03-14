using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MatchThree.Tiles;

namespace MatchThree.Board
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Create tile prefab
            var prefabGo = new GameObject("TilePrefab");
            prefabGo.SetActive(false);
            prefabGo.AddComponent<SpriteRenderer>();
            prefabGo.AddComponent<Tile>();

            // Generate tile data for each type
            var tileDataSet = new TileData[5];

            tileDataSet[0] = CreateTileData(TileType.Circle, new Color(0.9f, 0.25f, 0.3f), SpriteGenerator.CreateCircle());     // Red
            tileDataSet[1] = CreateTileData(TileType.Square, new Color(0.2f, 0.6f, 0.95f), SpriteGenerator.CreateSquare());     // Blue
            tileDataSet[2] = CreateTileData(TileType.Diamond, new Color(0.2f, 0.85f, 0.4f), SpriteGenerator.CreateDiamond());   // Green
            tileDataSet[3] = CreateTileData(TileType.Triangle, new Color(0.95f, 0.8f, 0.15f), SpriteGenerator.CreateTriangle()); // Yellow
            tileDataSet[4] = CreateTileData(TileType.Hexagon, new Color(0.7f, 0.3f, 0.9f), SpriteGenerator.CreateHexagon());    // Purple

            // Create board
            var boardGo = new GameObject("GameBoard");
            var board = boardGo.AddComponent<GameBoard>();

            // Use reflection to set serialized fields since we're doing runtime setup
            SetPrivateField(board, "_tilePrefab", prefabGo);
            SetPrivateField(board, "_tileDataSet", tileDataSet);

            // Set up camera background
            Camera.main.backgroundColor = new Color(0.12f, 0.12f, 0.18f);

            // Create UI
            CreateScoreUI(board);
        }

        private static TileData CreateTileData(TileType type, Color colour, Sprite sprite)
        {
            var data = ScriptableObject.CreateInstance<TileData>();
            data.Type = type;
            data.Colour = colour;
            data.Sprite = sprite;
            return data;
        }

        private static void CreateScoreUI(GameBoard board)
        {
            // Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Score text
            var textGo = new GameObject("ScoreText");
            textGo.transform.SetParent(canvasGo.transform, false);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "Score: 0";
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(300, 60);

            // Score display component
            var display = canvasGo.AddComponent<UI.ScoreDisplay>();
            SetPrivateField(display, "_board", board);
            SetPrivateField(display, "_scoreText", text);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogError($"Could not find field {fieldName} on {target.GetType().Name}");
        }
    }
}
