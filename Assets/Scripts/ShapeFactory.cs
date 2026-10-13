using UnityEngine;

namespace Breakout
{
    /// <summary>
    /// 程序化生成基础图形 Sprite（方块 / 圆），这样仓库里不需要放任何美术素材，
    /// 克隆下来就能看到一个完整的游戏画面。
    /// </summary>
    public static class ShapeFactory
    {
        private const int Size = 64;
        private const float PixelsPerUnit = 64f;

        private static Sprite square;
        private static Sprite circle;

        public static Sprite Square()
        {
            if (square != null) return square;

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            square = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            square.name = "SquareSprite";
            return square;
        }

        public static Sprite Circle()
        {
            if (circle != null) return circle;

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            float r = Size * 0.5f;
            Vector2 center = new Vector2(r, r);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // 边缘做一点点羽化，避免锯齿
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * Mathf.Clamp01(r - d + 0.5f)), 0, 255);
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            circle = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            circle.name = "CircleSprite";
            return circle;
        }
    }
}
