using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Procedurally rasterizes a single hex cell into a Sprite — no art assets, no Tilemap.
    // The fill test reuses HexCoord.ContainsPoint (the same math click-to-move uses), so the
    // drawn shape can never drift from what the grid logic treats as "inside this hex". The
    // border ring is drawn by testing containment a second time against a shrunk hex size.
    public static class HexSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        private const float BorderShrink = 0.88f;
        private const byte BorderAlpha = 255;
        private const byte InteriorAlpha = 90;

        public static Sprite Create(float hexSize)
        {
            int width = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(3f) * hexSize * PixelsPerUnit));
            int height = Mathf.Max(2, Mathf.CeilToInt(2f * hexSize * PixelsPerUnit));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[width * height];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    float localX = (px + 0.5f - width / 2f) / PixelsPerUnit;
                    float localY = (py + 0.5f - height / 2f) / PixelsPerUnit;
                    var local = new Vector2(localX, localY);

                    byte alpha;
                    if (!HexCoord.ContainsPoint(local, hexSize))
                        alpha = 0;
                    else if (!HexCoord.ContainsPoint(local, hexSize * BorderShrink))
                        alpha = BorderAlpha;
                    else
                        alpha = InteriorAlpha;

                    pixels[py * width + px] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }
    }
}
