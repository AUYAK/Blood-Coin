using System.Collections.Generic;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Draws every valid battlefield cell as a faint hex outline, plus a brighter highlight
    // over cells the currently acting unit can reach by movement this turn. Builds off
    // CombatManager.GridReady rather than Start(), so it doesn't depend on running after
    // CombatBootstrapper (Unity doesn't guarantee Start() order across GameObjects).
    public class HexGridView : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private Color cellColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color reachableColor = new Color(0.3f, 1f, 0.3f, 0.7f);

        private readonly Dictionary<HexCoord, SpriteRenderer> _reachableViews = new Dictionary<HexCoord, SpriteRenderer>();
        private Sprite _hexSprite;
        private Transform _highlightsRoot;

        private void OnEnable()
        {
            if (combatManager != null)
                combatManager.GridReady += BuildGridCells;
        }

        private void OnDisable()
        {
            if (combatManager != null)
                combatManager.GridReady -= BuildGridCells;
        }

        private void BuildGridCells()
        {
            _hexSprite = HexSpriteFactory.Create(combatManager.HexSize);

            var cellsRoot = new GameObject("Cells").transform;
            cellsRoot.SetParent(transform, false);
            foreach (var cell in combatManager.Grid.ValidCells)
                CreateCellSprite(cell, cellsRoot);

            _highlightsRoot = new GameObject("Highlights").transform;
            _highlightsRoot.SetParent(transform, false);
        }

        private void CreateCellSprite(HexCoord cell, Transform parent)
        {
            var go = new GameObject($"Cell {cell}", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var pos = cell.ToWorldPosition(combatManager.HexSize);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _hexSprite;
            renderer.color = cellColor;
            renderer.sortingOrder = -10;
        }

        private void Update()
        {
            if (_hexSprite == null || combatManager.Grid == null)
                return;

            var unit = combatManager.State == CombatState.UnitActing ? combatManager.CurrentUnit : null;
            var reachable = unit != null
                ? new HashSet<HexCoord>(combatManager.Grid.GetReachableCells(unit.Position, unit.CurrentActionPoints).Keys)
                : new HashSet<HexCoord>();

            foreach (var kvp in _reachableViews)
                kvp.Value.enabled = reachable.Contains(kvp.Key);

            foreach (var cell in reachable)
            {
                if (_reachableViews.TryGetValue(cell, out var renderer))
                {
                    renderer.enabled = true;
                    continue;
                }

                var go = new GameObject($"Reachable {cell}", typeof(SpriteRenderer));
                go.transform.SetParent(_highlightsRoot, false);
                var pos = cell.ToWorldPosition(combatManager.HexSize);
                go.transform.position = new Vector3(pos.x, pos.y, -0.05f);

                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = _hexSprite;
                sr.color = reachableColor;
                sr.sortingOrder = -5;
                _reachableViews[cell] = sr;
            }
        }
    }
}
