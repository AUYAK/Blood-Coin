using UnityEngine;

namespace BloodAndCoin.Combat
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CombatUnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer highlightSprite;

        [Header("Полосы HP/брони")]
        [SerializeField] private float barWidth = 0.9f;
        [SerializeField] private float barHeight = 0.12f;
        [SerializeField] private float barSpacing = 0.14f;
        [SerializeField] private Vector3 barsAnchor = new Vector3(0f, 0.65f, 0f);
        [SerializeField] private Color hpBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color hpFillColor = new Color(0.25f, 0.85f, 0.25f);
        [SerializeField] private Color armorBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color armorFillColor = new Color(0.55f, 0.7f, 0.95f);

        private Transform _hpFill;
        private Transform _armorFill;

        public CombatUnit Unit { get; private set; }

        public void Bind(CombatUnit unit, float hexSize)
        {
            Unit = unit;
            EnsureHighlight();
            EnsureStatusBars();
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

        // Armor bar only exists for units that actually have armor — keeps units without it
        // uncluttered ("ненавязчиво") instead of showing a permanently-empty second bar.
        private void EnsureStatusBars()
        {
            if (_hpFill != null)
                return;

            _hpFill = CreateBar(barsAnchor, hpBackgroundColor, hpFillColor, sortingBase: 1);

            if (Unit.Stats.maxArmor > 0)
                _armorFill = CreateBar(barsAnchor + new Vector3(0f, barSpacing, 0f), armorBackgroundColor, armorFillColor, sortingBase: 3);

            RefreshStatusBars();
        }

        private Transform CreateBar(Vector3 localAnchor, Color backgroundColor, Color fillColor, int sortingBase)
        {
            var root = new GameObject("Bar").transform;
            root.SetParent(transform, false);
            root.localPosition = localAnchor;

            var bgGo = new GameObject("Background", typeof(SpriteRenderer));
            bgGo.transform.SetParent(root, false);
            bgGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sprite = SquareSpriteFactory.GetOrCreate();
            bg.color = backgroundColor;
            bg.sortingOrder = sortingBase;

            var fillGo = new GameObject("Fill", typeof(SpriteRenderer));
            fillGo.transform.SetParent(root, false);
            fillGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            var fill = fillGo.GetComponent<SpriteRenderer>();
            fill.sprite = SquareSpriteFactory.GetOrCreate();
            fill.color = fillColor;
            fill.sortingOrder = sortingBase + 1;

            return fillGo.transform;
        }

        private void Update() => RefreshStatusBars();

        private void RefreshStatusBars()
        {
            if (Unit == null || _hpFill == null)
                return;

            SetBarFraction(_hpFill, barsAnchor, (float)Unit.CurrentHp / Unit.Stats.maxHp);

            if (_armorFill != null)
                SetBarFraction(_armorFill, barsAnchor + new Vector3(0f, barSpacing, 0f),
                    (float)Unit.CurrentArmor / Unit.Stats.maxArmor);
        }

        // Shrinks the fill from full barWidth down to fraction*barWidth while keeping its
        // left edge fixed (instead of shrinking symmetrically from the center-pivoted sprite).
        private void SetBarFraction(Transform fill, Vector3 anchor, float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            float width = barWidth * fraction;
            fill.localScale = new Vector3(width, barHeight, 1f);

            float leftEdge = anchor.x - barWidth / 2f;
            fill.localPosition = new Vector3(leftEdge + width / 2f, anchor.y, anchor.z);
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
