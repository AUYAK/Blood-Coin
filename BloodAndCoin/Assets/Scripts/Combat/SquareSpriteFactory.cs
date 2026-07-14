using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Shared 1x1 white square sprite for runtime-generated unit views and highlights — no
    // imported art needed to make the prototype playable.
    public static class SquareSpriteFactory
    {
        private static Sprite _cached;

        public static Sprite GetOrCreate()
        {
            if (_cached != null)
                return _cached;

            var texture = Texture2D.whiteTexture;
            _cached = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            return _cached;
        }
    }
}
