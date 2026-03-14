using UnityEngine;

namespace MatchThree.Tiles
{
    [CreateAssetMenu(fileName = "NewTileData", menuName = "MatchThree/TileData")]
    public class TileData : ScriptableObject
    {
        public TileType Type;
        public Color Colour;
        public Sprite Sprite;
    }
}
