using UnityEngine;
using MatchThree.Tiles;
using MatchThree.Core;

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
                // Ensure EventSystem exists for UI buttons
                UIHelper.EnsureEventSystem();

                // Ensure camera exists
                var cam = Camera.main;
                if (cam == null)
                {
                    var camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    cam = camGo.AddComponent<Camera>();
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                }
                cam.orthographicSize = 6f;
                cam.backgroundColor = new Color(0.08f, 0.08f, 0.14f);
                cam.transform.position = new Vector3(0, 0, -10f);

                // Create tile prefab
                var prefabGo = new GameObject("TilePrefab");
                prefabGo.SetActive(false);
                prefabGo.AddComponent<SpriteRenderer>();
                prefabGo.AddComponent<Tile>();

                // Generate tile data
                var tileDataSet = new TileData[]
                {
                    CreateTileData(TileType.Circle, new Color(0.9f, 0.25f, 0.3f), SpriteGenerator.CreateCircle()),
                    CreateTileData(TileType.Square, new Color(0.2f, 0.6f, 0.95f), SpriteGenerator.CreateSquare()),
                    CreateTileData(TileType.Diamond, new Color(0.2f, 0.85f, 0.4f), SpriteGenerator.CreateDiamond()),
                    CreateTileData(TileType.Triangle, new Color(0.95f, 0.8f, 0.15f), SpriteGenerator.CreateTriangle()),
                    CreateTileData(TileType.Hexagon, new Color(0.7f, 0.3f, 0.9f), SpriteGenerator.CreateHexagon()),
                };

                // Create GameManager
                var gmGo = new GameObject("GameManager");
                var gm = gmGo.AddComponent<GameManager>();
                gm.Init(prefabGo, tileDataSet);

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
    }
}
