using UnityEngine;

namespace BloodAndCoin.Combat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CombatUnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer highlightSprite;

        public CombatUnit Unit { get; private set; }

        public void Bind(CombatUnit unit, float hexSize)
        {
            Unit = unit;
            EnsureHighlight();
            SyncPosition(hexSize);
        }

        // If no prefab-authored highlightSprite was assigned, generate a simple halo behind
        // the unit at runtime — keeps auto-generated units (CombatBootstrapper's fallback,
        // no prefab) visibly highlightable without requiring Editor setup.
        private void EnsureHighlight()
        {
            if (highlightSprite != null)
                return;

            var go = new GameObject("Highlight", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 1.4f;

            var ownRenderer = GetComponent<SpriteRenderer>();
            var haloRenderer = go.GetComponent<SpriteRenderer>();
            haloRenderer.sprite = SquareSpriteFactory.GetOrCreate();
            haloRenderer.color = new Color(1f, 0.95f, 0.2f, 0.9f);
            haloRenderer.sortingOrder = ownRenderer.sortingOrder - 1;
            haloRenderer.enabled = false;

            highlightSprite = haloRenderer;
        }

        public void SyncPosition(float hexSize)
        {
            if (Unit == null)
                return;

            Vector2 worldPos = Unit.Position.ToWorldPosition(hexSize);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightSprite != null)
                highlightSprite.enabled = highlighted;
        }
    }
}
