using UnityEngine;
using UnityEngine.UI;
using MatchThree.Tiles;

namespace MatchThree.Board
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Debug.Log("[MatchThree] Bootstrap starting...");

            try
            {
                // Create tile prefab
                var prefabGo = new GameObject("TilePrefab");
                prefabGo.SetActive(false);
                prefabGo.AddComponent<SpriteRenderer>();
                prefabGo.AddComponent<Tile>();

                // Generate tile data for each type
                var tileDataSet = new TileData[5];
                tileDataSet[0] = CreateTileData(TileType.Circle, new Color(0.9f, 0.25f, 0.3f), SpriteGenerator.CreateCircle());
                tileDataSet[1] = CreateTileData(TileType.Square, new Color(0.2f, 0.6f, 0.95f), SpriteGenerator.CreateSquare());
                tileDataSet[2] = CreateTileData(TileType.Diamond, new Color(0.2f, 0.85f, 0.4f), SpriteGenerator.CreateDiamond());
                tileDataSet[3] = CreateTileData(TileType.Triangle, new Color(0.95f, 0.8f, 0.15f), SpriteGenerator.CreateTriangle());
                tileDataSet[4] = CreateTileData(TileType.Hexagon, new Color(0.7f, 0.3f, 0.9f), SpriteGenerator.CreateHexagon());

                Debug.Log("[MatchThree] Tile data created");

                // Create board
                var boardGo = new GameObject("GameBoard");
                var board = boardGo.AddComponent<GameBoard>();

                SetPrivateField(board, "_tilePrefab", prefabGo);
                SetPrivateField(board, "_tileDataSet", tileDataSet);

                // Set up camera background
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
                }
                else
                {
                    Debug.LogWarning("[MatchThree] No MainCamera found — creating one");
                    var camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    cam = camGo.AddComponent<Camera>();
                    cam.orthographic = true;
                    cam.orthographicSize = 6f;
                    cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.transform.position = new Vector3(0, 0, -10f);
                }

                // Create UI
                CreateScoreUI(board);

                Debug.Log("[MatchThree] Bootstrap complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MatchThree] Bootstrap failed: {e}");
            }
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

            // Score text — using Unity UI Text to avoid TMP essential resources dependency
            var textGo = new GameObject("ScoreText");
            textGo.transform.SetParent(canvasGo.transform, false);

            var text = textGo.AddComponent<Text>();
            text.text = "Score: 0";
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 32);

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
                Debug.LogError($"[MatchThree] Could not find field {fieldName} on {target.GetType().Name}");
        }
    }
}
