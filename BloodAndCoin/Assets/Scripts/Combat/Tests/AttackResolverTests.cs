using NUnit.Framework;
using UnityEngine;

namespace BloodAndCoin.Combat.Tests
{
    public class AttackResolverTests
    {
        // System.Random.Next(int,int) is virtual specifically so tests can force a fixed roll
        // instead of asserting on flaky randomized outcomes.
        private class FixedRandom : System.Random
        {
            private readonly int _fixedRoll;
            public FixedRandom(int fixedRoll) => _fixedRoll = fixedRoll;
            public override int Next(int minValue, int maxValue) => _fixedRoll;
        }

        private static CombatUnit MakeUnit(
            int meleeAccuracy = 0, int meleeDefense = 0, int meleeDamage = 5,
            int rangedAccuracy = 0, int rangedDefense = 0, int rangedDamage = 5)
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.meleeAccuracy = meleeAccuracy;
            stats.meleeDefense = meleeDefense;
            stats.meleeDamage = meleeDamage;
            stats.rangedAccuracy = rangedAccuracy;
            stats.rangedDefense = rangedDefense;
            stats.rangedDamage = rangedDamage;
            return new CombatUnit("Unit", stats, Team.Player, tieBreakRoll: 0f);
        }

        [Test]
        public void CalculateHitChance_HasNoSideEffects()
        {
            var attacker = MakeUnit(meleeAccuracy: 50);
            var defender = MakeUnit(meleeDefense: 10);
            int hpBefore = defender.CurrentHp;

            int chance = AttackResolver.CalculateHitChance(attacker, defender, ranged: false);

            Assert.AreEqual(90, chance); // 50 base + 50 accuracy - 10 defense, clamped
            Assert.AreEqual(hpBefore, defender.CurrentHp, "Calculating the odds must not deal damage.");
        }

        [Test]
        public void HitChance_ClampsToFloor_WhenDefenseFarExceedsAccuracy()
        {
            var attacker = MakeUnit(meleeAccuracy: 0);
            var defender = MakeUnit(meleeDefense: 100);

            var justUnderFloor = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(4));
            Assert.IsTrue(justUnderFloor.Hit, "5% floor should still allow a roll of 4 to hit.");

            var atFloor = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(5));
            Assert.IsFalse(atFloor.Hit, "Chance should be clamped to exactly the 5% floor, not lower.");
        }

        [Test]
        public void HitChance_ClampsToCeiling_WhenAccuracyFarExceedsDefense()
        {
            var attacker = MakeUnit(meleeAccuracy: 100);
            var defender = MakeUnit(meleeDefense: 0);

            var justUnderCeiling = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(94));
            Assert.IsTrue(justUnderCeiling.Hit, "95% ceiling should still allow a roll of 94 to hit.");

            var atCeiling = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(95));
            Assert.IsFalse(atCeiling.Hit, "Chance should be clamped to exactly the 95% ceiling, not higher.");
        }

        [Test]
        public void Hit_AppliesMeleeDamageToDefender()
        {
            var attacker = MakeUnit(meleeAccuracy: 50, meleeDamage: 7);
            var defender = MakeUnit(meleeDefense: 0);
            int hpBefore = defender.CurrentHp;

            var result = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(0));

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

            var result = AttackResolver.Resolve(attacker, defender, ranged: false, new FixedRandom(50));

            Assert.IsFalse(result.Hit);
            Assert.AreEqual(hpBefore, defender.CurrentHp);
        }

        [Test]
        public void Ranged_UsesRangedStats_NotMeleeStats()
        {
            // High melee accuracy/damage but zero ranged — a ranged attack should miss and
            // deal no damage despite melee stats looking favorable, proving it reads the
            // ranged pair, not the melee one.
            var attacker = MakeUnit(meleeAccuracy: 100, meleeDamage: 99, rangedAccuracy: 0, rangedDamage: 0);
            var defender = MakeUnit(meleeDefense: 0, rangedDefense: 100);
            int hpBefore = defender.CurrentHp;

            var result = AttackResolver.Resolve(attacker, defender, ranged: true, new FixedRandom(10));

            Assert.IsFalse(result.Hit);
            Assert.AreEqual(hpBefore, defender.CurrentHp);
        }

        [Test]
        public void Ranged_Hit_AppliesRangedDamage()
        {
            var attacker = MakeUnit(rangedAccuracy: 50, rangedDamage: 4);
            var defender = MakeUnit(rangedDefense: 0);
            int hpBefore = defender.CurrentHp;

            var result = AttackResolver.Resolve(attacker, defender, ranged: true, new FixedRandom(0));

            Assert.IsTrue(result.Hit);
            Assert.AreEqual(4, result.Damage);
            Assert.AreEqual(hpBefore - 4, defender.CurrentHp);
        }
    }
}
