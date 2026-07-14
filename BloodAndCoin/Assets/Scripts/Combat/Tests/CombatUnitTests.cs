using NUnit.Framework;
using UnityEngine;

namespace BloodAndCoin.Combat.Tests
{
    public class CombatUnitTests
    {
        [Test]
        public void AttackRange_MeleeUnit_IsAlwaysOne()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.isRanged = false;
            stats.rangedRange = 5; // should be ignored while isRanged is false
            var unit = new CombatUnit("Melee", stats, Team.Player, tieBreakRoll: 0f);

            Assert.AreEqual(1, unit.AttackRange);
        }

        [Test]
        public void AttackRange_RangedUnit_UsesRangedRangeStat()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.isRanged = true;
            stats.rangedRange = 4;
            var unit = new CombatUnit("Archer", stats, Team.Player, tieBreakRoll: 0f);

            Assert.AreEqual(4, unit.AttackRange);
        }

        [Test]
        public void AttackActionPointCost_SwitchesBetweenMeleeAndRangedCost()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.meleeActionPointCost = 2;
            stats.rangedActionPointCost = 3;

            stats.isRanged = false;
            var melee = new CombatUnit("Melee", stats, Team.Player, tieBreakRoll: 0f);
            Assert.AreEqual(2, melee.AttackActionPointCost);

            stats.isRanged = true;
            var ranged = new CombatUnit("Archer", stats, Team.Player, tieBreakRoll: 0f);
            Assert.AreEqual(3, ranged.AttackActionPointCost);
        }

        [Test]
        public void ApplyDamage_ArmorAbsorbsBeforeHp()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.maxHp = 20;
            stats.maxArmor = 5;
            var unit = new CombatUnit("Armored", stats, Team.Player, tieBreakRoll: 0f);

            unit.ApplyDamage(3);

            Assert.AreEqual(2, unit.CurrentArmor);
            Assert.AreEqual(20, unit.CurrentHp, "Damage fully absorbed by armor shouldn't touch HP.");
        }

        [Test]
        public void ApplyDamage_OverflowsToHpOnceArmorIsDepleted()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.maxHp = 20;
            stats.maxArmor = 5;
            var unit = new CombatUnit("Armored", stats, Team.Player, tieBreakRoll: 0f);

            unit.ApplyDamage(8); // 5 absorbed by armor, 3 spills over to HP

            Assert.AreEqual(0, unit.CurrentArmor);
            Assert.AreEqual(17, unit.CurrentHp);
        }

        [Test]
        public void ApplyDamage_NoArmor_GoesStraightToHp()
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.maxHp = 20;
            stats.maxArmor = 0;
            var unit = new CombatUnit("Unarmored", stats, Team.Player, tieBreakRoll: 0f);

            unit.ApplyDamage(6);

            Assert.AreEqual(0, unit.CurrentArmor);
            Assert.AreEqual(14, unit.CurrentHp);
        }
    }
}
