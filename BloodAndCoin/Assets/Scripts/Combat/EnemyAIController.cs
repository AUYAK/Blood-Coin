using System.Collections;
using System.Linq;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Simple PvE opponent: on its turn, walks toward the nearest living player unit and
    // attacks once adjacent. No pathfinding beyond "pick the reachable cell that gets
    // closest" and no target prioritization beyond distance — enough to make the prototype
    // playable solo instead of hand-driving both sides.
    public class EnemyAIController : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private CombatActionExecutor actions;
        [SerializeField] private float actionDelay = 0.4f;

        // Backstop against an unforeseen loop (e.g. a future rule change that stops AP from
        // ever reaching zero) — a real turn should end long before this via TryMove/TryAttack.
        private const int MaxStepsPerTurn = 20;

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

        private void OnTurnStarted(CombatUnit unit)
        {
            if (unit.Team == Team.Enemy)
                StartCoroutine(RunTurn(unit));
        }

        private IEnumerator RunTurn(CombatUnit unit)
        {
            for (int step = 0; step < MaxStepsPerTurn; step++)
            {
                yield return new WaitForSeconds(actionDelay);

                if (combatManager.CurrentUnit != unit || unit.CurrentActionPoints <= 0)
                    yield break;

                var target = combatManager.InitiativeQueueSnapshot
                    .Where(u => u.Team == Team.Player && u.IsAlive)
                    .OrderBy(u => unit.Position.DistanceTo(u.Position))
                    .FirstOrDefault();

                if (target == null)
                {
                    combatManager.EndCurrentUnitTurn();
                    yield break;
                }

                if (unit.Position.DistanceTo(target.Position) <= unit.AttackRange)
                {
                    // Check affordability ourselves rather than calling TryAttack blind: the
                    // AI already knows its own CurrentActionPoints, so there's no reason to
                    // attempt an attack we can already tell will be refused (that "attempt and
                    // react to failure" pattern is what caused the hang — retrying the same
                    // doomed attack every step until MaxStepsPerTurn).
                    if (unit.CurrentActionPoints < unit.AttackActionPointCost)
                    {
                        combatManager.EndCurrentUnitTurn();
                        yield break;
                    }

                    actions.TryAttack(unit, target);
                    continue;
                }

                if (!TryStepToward(unit, target))
                {
                    combatManager.EndCurrentUnitTurn();
                    yield break;
                }
            }

            if (combatManager.CurrentUnit == unit)
                combatManager.EndCurrentUnitTurn();
        }

        private bool TryStepToward(CombatUnit unit, CombatUnit target)
        {
            var reachable = combatManager.Grid.GetReachableCells(unit.Position, unit.CurrentActionPoints);
            if (reachable.Count == 0)
                return false;

            int bestDistance = unit.Position.DistanceTo(target.Position);
            HexCoord? best = null;

            foreach (var cell in reachable.Keys)
            {
                int distance = cell.DistanceTo(target.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cell;
                }
            }

            return best.HasValue && actions.TryMove(unit, best.Value);
        }
    }
}
