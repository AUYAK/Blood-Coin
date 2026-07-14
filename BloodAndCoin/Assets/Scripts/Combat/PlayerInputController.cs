using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BloodAndCoin.Combat
{
    // Polls Mouse.current directly instead of an Input Actions asset: hand-editing
    // InputSystem_Actions.inputactions outside the Editor risks producing an asset the
    // Editor can't reimport cleanly. Swap to an action map later if that friction matters.
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private Camera worldCamera;

        private readonly Dictionary<CombatUnit, CombatUnitView> _views = new Dictionary<CombatUnit, CombatUnitView>();
        // System.Random, not UnityEngine.Random — MeleeAttackResolver takes System.Random so
        // tests can substitute a fixed-roll subclass; both namespaces define "Random", so the
        // System one must stay fully qualified wherever UnityEngine is also imported.
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

        private void OnTurnStarted(CombatUnit unit)
        {
            _highlightedView?.SetHighlighted(false);
            _highlightedView = _views.TryGetValue(unit, out var view) ? view : null;
            _highlightedView?.SetHighlighted(true);
        }

        private void Update()
        {
            if (combatManager == null || combatManager.State != CombatState.UnitActing)
                return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                HandleClick();
        }

        private void HandleClick()
        {
            var actingUnit = combatManager.CurrentUnit;
            if (actingUnit == null)
                return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = worldCamera.ScreenToWorldPoint(screenPos);
            var clicked = HexCoord.FromWorldPosition(worldPos, combatManager.HexSize);

            var grid = combatManager.Grid;
            if (!grid.IsValidCell(clicked))
                return;

            var occupant = grid.GetOccupant(clicked);

            if (occupant != null && occupant != actingUnit)
            {
                if (occupant.Team != actingUnit.Team && clicked.DistanceTo(actingUnit.Position) == 1)
                    TryAttack(actingUnit, occupant);
                else
                    Debug.Log($"{actingUnit.Name}: цель недоступна для атаки ({occupant.Name}).");
                return;
            }

            if (occupant == null)
                TryMove(actingUnit, clicked);
        }

        private void TryMove(CombatUnit unit, HexCoord destination)
        {
            var grid = combatManager.Grid;
            var reachable = grid.GetReachableCells(unit.Position, unit.CurrentActionPoints);

            if (!reachable.TryGetValue(destination, out int cost))
            {
                Debug.Log($"{unit.Name}: {destination} недостижима при {unit.CurrentActionPoints} очках действий.");
                return;
            }

            unit.TrySpendActionPoints(cost);
            grid.MoveUnit(unit, destination);
            if (_views.TryGetValue(unit, out var view))
                view.SyncPosition(combatManager.HexSize);

            EndTurnIfOutOfActionPoints(unit);
        }

        private void TryAttack(CombatUnit attacker, CombatUnit defender)
        {
            int cost = attacker.Stats.attackActionPointCost;
            if (!attacker.TrySpendActionPoints(cost))
            {
                Debug.Log($"{attacker.Name}: не хватает очков действий для атаки ({attacker.CurrentActionPoints}/{cost}).");
                return;
            }

            var result = MeleeAttackResolver.Resolve(attacker, defender, _random);
            Debug.Log(result.Hit
                ? $"{attacker.Name} попадает по {defender.Name} на {result.Damage} урона (HP: {defender.CurrentHp})."
                : $"{attacker.Name} промахивается по {defender.Name}.");

            if (!defender.IsAlive)
            {
                combatManager.Grid.RemoveUnit(defender);
                if (_views.TryGetValue(defender, out var defenderView))
                {
                    Destroy(defenderView.gameObject);
                    _views.Remove(defender);
                }
            }

            EndTurnIfOutOfActionPoints(attacker);
        }

        private void EndTurnIfOutOfActionPoints(CombatUnit unit)
        {
            if (unit.CurrentActionPoints <= 0)
                combatManager.EndCurrentUnitTurn();
        }

        // Hooked up to the debug UI's "End Turn" button so a unit with leftover action
        // points doesn't have to be spent down to zero just to pass to the next unit.
        public void EndTurnManually() => combatManager?.EndCurrentUnitTurn();
    }
}
