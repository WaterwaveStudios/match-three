using UnityEngine;

namespace MatchThree.Tiles
{
    public static class SpriteGenerator
    {
        private const int Size = 128;
        private const int HalfSize = Size / 2;
        private const int Radius = 56;

        public static Sprite CreateCircle()
        {
            var tex = NewTexture();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(HalfSize, HalfSize));
                    tex.SetPixel(x, y, dist <= Radius ? Color.white : Color.clear);
                }
            }
            return Finalise(tex);
        }

        public static Sprite CreateSquare()
        {
            var tex = NewTexture();
            int margin = 12;
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    bool inside = x >= margin && x < Size - margin && y >= margin && y < Size - margin;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            return Finalise(tex);
        }

        public static Sprite CreateDiamond()
        {
            var tex = NewTexture();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    float dx = Mathf.Abs(x - HalfSize);
                    float dy = Mathf.Abs(y - HalfSize);
                    bool inside = (dx / Radius + dy / Radius) <= 1f;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            return Finalise(tex);
        }

        public static Sprite CreateTriangle()
        {
            var tex = NewTexture();
            int bottom = 10;
            int top = Size - 10;
            int height = top - bottom;
            for (int y = bottom; y < top; y++)
            {
                float progress = (float)(y - bottom) / height;
                float halfWidth = (1f - progress) * (HalfSize - 10);
                int left = (int)(HalfSize - halfWidth);
                int right = (int)(HalfSize + halfWidth);
                for (int x = 0; x < Size; x++)
                {
                    tex.SetPixel(x, y, (x >= left && x <= right) ? Color.white : Color.clear);
                }
            }
            // Fill rows outside triangle
            for (int y = 0; y < bottom; y++)
                for (int x = 0; x < Size; x++)
                    tex.SetPixel(x, y, Color.clear);
            for (int y = top; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    tex.SetPixel(x, y, Color.clear);

            return Finalise(tex);
        }

        public static Sprite CreateHexagon()
        {
            var tex = NewTexture();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    float dx = (x - HalfSize) / (float)Radius;
                    float dy = (y - HalfSize) / (float)Radius;
                    // Hexagon distance
                    float q = Mathf.Abs(dx);
                    float r = Mathf.Abs(dy);
                    bool inside = (q + r * 0.577f) <= 1f && r <= 0.866f;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            return Finalise(tex);
        }

        private static Texture2D NewTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color[Size * Size];
            tex.SetPixels(clear);
            return tex;
        }

        private static Sprite Finalise(Texture2D tex)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
