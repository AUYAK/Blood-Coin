using System.Collections.Generic;
using System.Linq;

namespace BloodAndCoin.Combat
{
    // Single shared queue for both sides, recomputed every round from current Initiative —
    // per docs/GDD.md раздел 5, not "our side first, then theirs".
    public static class InitiativeOrder
    {
        public static List<CombatUnit> Build(IEnumerable<CombatUnit> units)
        {
            return units
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.Stats.initiative)
                .ThenByDescending(u => u.TieBreakRoll)
                .ToList();
        }
    }
}
