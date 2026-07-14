using NUnit.Framework;
using UnityEngine;

namespace BloodAndCoin.Combat.Tests
{
    public class MeleeAttackResolverTests
    {
        // System.Random.Next(int,int) is virtual specifically so tests can force a fixed roll
        // instead of asserting on flaky randomized outcomes.
        private class FixedRandom : System.Random
        {
            private readonly int _fixedRoll;
            public FixedRandom(int fixedRoll) => _fixedRoll = fixedRoll;
            public override int Next(int minValue, int maxValue) => _fixedRoll;
        }

        private static CombatUnit MakeUnit(int meleeAccuracy = 0, int meleeDefense = 0, int meleeDamage = 5)
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.meleeAccuracy = meleeAccuracy;
            stats.meleeDefense = meleeDefense;
            stats.meleeDamage = meleeDamage;
            return new CombatUnit("Unit", stats, Team.Player, tieBreakRoll: 0f);
        }

        [Test]
        public void HitChance_ClampsToFloor_WhenDefenseFarExceedsAccuracy()
        {
            var attacker = MakeUnit(meleeAccuracy: 0);
            var defender = MakeUnit(meleeDefense: 100);

            var justUnderFloor = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(4));
            Assert.IsTrue(justUnderFloor.Hit, "5% floor should still allow a roll of 4 to hit.");

            var atFloor = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(5));
            Assert.IsFalse(atFloor.Hit, "Chance should be clamped to exactly the 5% floor, not lower.");
        }

        [Test]
        public void HitChance_ClampsToCeiling_WhenAccuracyFarExceedsDefense()
        {
            var attacker = MakeUnit(meleeAccuracy: 100);
            var defender = MakeUnit(meleeDefense: 0);

            var justUnderCeiling = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(94));
            Assert.IsTrue(justUnderCeiling.Hit, "95% ceiling should still allow a roll of 94 to hit.");

            var atCeiling = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(95));
            Assert.IsFalse(atCeiling.Hit, "Chance should be clamped to exactly the 95% ceiling, not higher.");
        }

        [Test]
        public void Hit_AppliesMeleeDamageToDefender()
        {
            var attacker = MakeUnit(meleeAccuracy: 50, meleeDamage: 7);
            var defender = MakeUnit(meleeDefense: 0);
            int hpBefore = defender.CurrentHp;

            var result = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(0));

            Assert.IsTrue(result.Hit);
            Assert.AreEqual(7, result.Damage);
            Assert.AreEqual(hpBefore - 7, defender.CurrentHp);
        }

        [Test]
        public void Miss_DoesNotApplyDamage()
        {
            var attacker = MakeUnit(meleeAccuracy: 0, meleeDamage: 7);
            var defender = MakeUnit(meleeDefense: 100);
            int hpBefore = defender.CurrentHp;

            var result = MeleeAttackResolver.Resolve(attacker, defender, new FixedRandom(50));

            Assert.IsFalse(result.Hit);
            Assert.AreEqual(hpBefore, defender.CurrentHp);
        }
    }
}
