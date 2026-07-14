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
        public int CurrentArmor { get; private set; }
        public int CurrentActionPoints { get; private set; }
        public HexCoord Position { get; set; }

        public bool IsAlive => CurrentHp > 0;

        // Melee is always range 1; a ranged unit's actual reach/cost come from its stats.
        public int AttackRange => Stats.isRanged ? Stats.rangedRange : 1;
        public int AttackActionPointCost => Stats.isRanged ? Stats.rangedActionPointCost : Stats.meleeActionPointCost;

        public CombatUnit(string name, UnitStatsDefinition stats, Team team, float tieBreakRoll)
        {
            Name = name;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Team = team;
            TieBreakRoll = tieBreakRoll;
            CurrentHp = stats.maxHp;
            CurrentArmor = stats.maxArmor;
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

        // Armor absorbs point-for-point before HP takes any overflow.
        public void ApplyDamage(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int absorbedByArmor = Math.Min(CurrentArmor, amount);
            CurrentArmor -= absorbedByArmor;

            int remaining = amount - absorbedByArmor;
            CurrentHp = Math.Max(0, CurrentHp - remaining);
        }
    }
}
