using System;

namespace BloodAndCoin.Combat
{
    public enum Team
    {
        Player,
        Enemy,
    }

    // Plain C# state, no MonoBehaviour — keeps combat logic testable in EditMode without a
    // scene. CombatUnitView is the MonoBehaviour bridge to the GameObject on screen.
    public class CombatUnit
    {
        public string Name { get; }
        public UnitStatsDefinition Stats { get; }
        public Team Team { get; }

        // Rolled once per battle at setup and never recomputed — see docs/GDD.md раздел 5:
        // tie-break must stay fixed for the whole fight so the queue doesn't reshuffle on ties.
        public float TieBreakRoll { get; }

        public int CurrentHp { get; private set; }
        public int CurrentActionPoints { get; private set; }
        public HexCoord Position { get; set; }

        public bool IsAlive => CurrentHp > 0;

        public CombatUnit(string name, UnitStatsDefinition stats, Team team, float tieBreakRoll)
        {
            Name = name;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Team = team;
            TieBreakRoll = tieBreakRoll;
            CurrentHp = stats.maxHp;
            CurrentActionPoints = stats.actionPoints;
        }

        public void ResetActionPoints() => CurrentActionPoints = Stats.actionPoints;

        public bool TrySpendActionPoints(int amount)
        {
            if (amount > CurrentActionPoints)
                return false;

            CurrentActionPoints -= amount;
            return true;
        }

        public void ApplyDamage(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            CurrentHp = Math.Max(0, CurrentHp - amount);
        }
    }
}
