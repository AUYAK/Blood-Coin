using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BloodAndCoin.Combat
{
    // Polls Pointer.current directly instead of an Input Actions asset: hand-editing
    // InputSystem_Actions.inputactions outside the Editor risks producing an asset the
    // Editor can't reimport cleanly. Swap to an action map later if that friction matters.
    // Pointer (not Mouse) specifically so this also works on touch — the project targets
    // Android/iOS, and Mouse.current is always null on a touch-only device.
    // Only acts during the player's own team's turns — enemy turns are driven by
    // EnemyAIController, through the same CombatActionExecutor.
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private CombatActionExecutor actions;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TargetInfoPanel targetInfoPanel;

        // First click on an in-range enemy shows targetInfoPanel; a second click on the same
        // enemy confirms the attack. Only gates the player's own clicks — EnemyAIController
        // calls CombatActionExecutor.TryAttack directly and always attacks immediately, so
        // there's no "window disco" on the AI's turns.
        private CombatUnit _pendingAttacker;
        private CombatUnit _pendingTarget;

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

        private void OnTurnStarted(CombatUnit unit) => ClearPendingAttack();

        private void Update()
        {
            if (combatManager == null || combatManager.State != CombatState.UnitActing)
                return;

            var actingUnit = combatManager.CurrentUnit;
            if (actingUnit == null || actingUnit.Team != Team.Player)
                return;

            if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
                return;

            // Don't also treat a tap/click on UI (e.g. the End Turn button) as a battlefield
            // click — only handles a single active pointer, which is all this game needs.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleClick(actingUnit);
        }

        private void HandleClick(CombatUnit actingUnit)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Vector3 worldPos = worldCamera.ScreenToWorldPoint(screenPos);
            var clicked = HexCoord.FromWorldPosition(worldPos, combatManager.HexSize);

            var grid = combatManager.Grid;
            if (!grid.IsValidCell(clicked))
                return;

            var occupant = grid.GetOccupant(clicked);

            if (occupant != null && occupant != actingUnit)
            {
                bool inRange = occupant.Team != actingUnit.Team && clicked.DistanceTo(actingUnit.Position) <= actingUnit.AttackRange;
                if (!inRange)
                {
                    Debug.Log($"{actingUnit.Name}: цель недоступна для атаки ({occupant.Name}).");
                    ClearPendingAttack();
                    return;
                }

                bool isConfirmingSameTarget = _pendingAttacker == actingUnit && _pendingTarget == occupant;
                if (isConfirmingSameTarget)
                {
                    ClearPendingAttack();
                    actions.TryAttack(actingUnit, occupant);
                }
                else
                {
                    _pendingAttacker = actingUnit;
                    _pendingTarget = occupant;
                    Vector2 targetWorldPos = occupant.Position.ToWorldPosition(combatManager.HexSize);
                    targetInfoPanel?.Show(actingUnit, occupant, new Vector3(targetWorldPos.x, targetWorldPos.y, 0f), worldCamera);
                }
                return;
            }

            ClearPendingAttack();
            if (occupant == null)
                actions.TryMove(actingUnit, clicked);
        }

        private void ClearPendingAttack()
        {
            _pendingAttacker = null;
            _pendingTarget = null;
            targetInfoPanel?.Hide();
        }

        // Hooked up to the debug UI's "End Turn" button so a unit with leftover action
        // points doesn't have to be spent down to zero just to pass to the next unit. Guarded
        // to the player's own turn — otherwise spam-clicking it could skip the AI's turn,
        // which the player has no other way to influence.
        public void EndTurnManually()
        {
            if (combatManager == null)
                return;

            var current = combatManager.CurrentUnit;
            if (current == null || current.Team != Team.Player)
                return;

            combatManager.EndCurrentUnitTurn();
        }
    }
}
