using UnityEngine;

namespace MatchThree.Tiles
{
    public class Tile : MonoBehaviour
    {
        public TileType Type { get; private set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsHinted { get; private set; }

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
            IsHinted = false;
            transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
        }

        public void SetHinted(bool hinted)
        {
            IsHinted = hinted;
            if (!hinted)
                transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (IsHinted)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
                transform.localScale = Vector3.one * pulse;
            }
        }
    }
}
