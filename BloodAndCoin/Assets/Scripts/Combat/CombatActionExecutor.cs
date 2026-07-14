using System.Collections.Generic;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Shared move/attack execution + unit-view registry, used by both PlayerInputController
    // (mouse clicks) and EnemyAIController (AI decisions) so movement/attack/death handling
    // isn't duplicated between them. Also keeps the "active unit" halo in sync with whoever's
    // turn it is, regardless of which side controls them.
    public class CombatActionExecutor : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private float projectileDuration = 0.35f;
        [SerializeField] private float projectileArcHeight = 1.5f;
        [SerializeField] private Color projectileColor = new Color(0.9f, 0.85f, 0.3f);

        private readonly Dictionary<CombatUnit, CombatUnitView> _views = new Dictionary<CombatUnit, CombatUnitView>();
        // System.Random, not UnityEngine.Random — AttackResolver takes System.Random so tests
        // can substitute a fixed-roll subclass; both namespaces define "Random", so the System
        // one must stay fully qualified wherever UnityEngine is also imported.
        private readonly System.Random _random = new System.Random();
        private CombatUnitView _highlightedView;

        private void OnEnable()
        {
            if (combatManager != null)
                combatManager.TurnStarted += OnTurnStarted;
        }

        private void OnDisable()
        {
            if (combatManager != null)
                combatManager.TurnStarted -= OnTurnStarted;
        }

        public void RegisterView(CombatUnit unit, CombatUnitView view) => _views[unit] = view;

        public CombatUnitView GetView(CombatUnit unit) => _views.TryGetValue(unit, out var view) ? view : null;

        private void OnTurnStarted(CombatUnit unit)
        {
            _highlightedView?.SetHighlighted(false);
            _highlightedView = GetView(unit);
            _highlightedView?.SetHighlighted(true);
        }

        public bool TryMove(CombatUnit unit, HexCoord destination)
        {
            var grid = combatManager.Grid;
            var reachable = grid.GetReachableCells(unit.Position, unit.CurrentActionPoints);

            if (!reachable.TryGetValue(destination, out int cost))
            {
                Debug.Log($"{unit.Name}: {destination} недостижима при {unit.CurrentActionPoints} очках действий.");
                return false;
            }

            unit.TrySpendActionPoints(cost);
            grid.MoveUnit(unit, destination);
            GetView(unit)?.SyncPosition(combatManager.HexSize);

            EndTurnIfOutOfActionPoints(unit);
            return true;
        }

        public bool TryAttack(CombatUnit attacker, CombatUnit defender)
        {
            int cost = attacker.AttackActionPointCost;
            if (!attacker.TrySpendActionPoints(cost))
            {
                Debug.Log($"{attacker.Name}: не хватает очков действий для атаки ({attacker.CurrentActionPoints}/{cost}).");
                return false;
            }

            bool ranged = attacker.Stats.isRanged;
            var result = AttackResolver.Resolve(attacker, defender, ranged, _random);
            Debug.Log(result.Hit
                ? $"{attacker.Name} попадает по {defender.Name} на {result.Damage} урона (HP: {defender.CurrentHp})."
                : $"{attacker.Name} промахивается по {defender.Name}.");

            // HP/grid/queue state updates immediately regardless of any travel animation —
            // only the killed unit's on-screen GameObject destruction is deferred to match
            // the arrow's arrival (see SpawnProjectile), so it doesn't vanish mid-flight.
            CombatUnitView defenderView = null;
            if (!defender.IsAlive)
            {
                combatManager.Grid.RemoveUnit(defender);
                defenderView = GetView(defender);
                _views.Remove(defender);
            }

            if (ranged)
                SpawnProjectile(attacker, defender, defenderView);
            else if (defenderView != null)
                Destroy(defenderView.gameObject);

            EndTurnIfOutOfActionPoints(attacker);
            return true;
        }

        private void EndTurnIfOutOfActionPoints(CombatUnit unit)
        {
            if (unit.CurrentActionPoints <= 0)
                combatManager.EndCurrentUnitTurn();
        }

        // Fire-and-forget visual only — the hit/damage/HP above already resolved by the time
        // this plays, so it never blocks or delays the turn. killedDefenderView (if any) is
        // only destroyed once the arrow arrives, not the instant the attack resolves.
        private void SpawnProjectile(CombatUnit attacker, CombatUnit defender, CombatUnitView killedDefenderView)
        {
            var go = new GameObject("Projectile", typeof(SpriteRenderer), typeof(Projectile));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = SquareSpriteFactory.GetOrCreate();
            renderer.color = projectileColor;
            renderer.sortingOrder = 20;
            go.transform.localScale = Vector3.one * 0.35f;

            go.GetComponent<Projectile>().Launch(
                ToWorld(attacker.Position), ToWorld(defender.Position), projectileDuration, projectileArcHeight,
                onArrival: () =>
                {
                    if (killedDefenderView != null)
                        Destroy(killedDefenderView.gameObject);
                });
        }

        private Vector3 ToWorld(HexCoord cell)
        {
            Vector2 pos = cell.ToWorldPosition(combatManager.HexSize);
            return new Vector3(pos.x, pos.y, 0f);
        }
    }
}
