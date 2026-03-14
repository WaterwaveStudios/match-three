using UnityEngine;

namespace MatchThree.Tiles
{
    public class Tile : MonoBehaviour
    {
        public TileType Type { get; private set; }
        public int Row { get; set; }
        public int Col { get; set; }

        private SpriteRenderer _spriteRenderer;

        public void Init(TileData data, int row, int col)
        {
            Type = data.Type;
            Row = row;
            Col = col;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = data.Sprite;
            _spriteRenderer.color = data.Colour;
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        }
    }
}
