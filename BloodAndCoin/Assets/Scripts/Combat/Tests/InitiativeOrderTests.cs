using NUnit.Framework;
using UnityEngine;

namespace BloodAndCoin.Combat.Tests
{
    public class InitiativeOrderTests
    {
        private static UnitStatsDefinition MakeStats(int initiative)
        {
            var stats = ScriptableObject.CreateInstance<UnitStatsDefinition>();
            stats.initiative = initiative;
            return stats;
        }

        [Test]
        public void Build_OrdersByDescendingInitiative()
        {
            var slow = new CombatUnit("Slow", MakeStats(5), Team.Player, tieBreakRoll: 0f);
            var fast = new CombatUnit("Fast", MakeStats(20), Team.Enemy, tieBreakRoll: 0f);
            var medium = new CombatUnit("Medium", MakeStats(10), Team.Player, tieBreakRoll: 0f);

            var order = InitiativeOrder.Build(new[] { slow, fast, medium });

            CollectionAssert.AreEqual(new[] { fast, medium, slow }, order);
        }

        [Test]
        public void Build_MixesBothTeamsInOneQueue()
        {
            var playerUnit = new CombatUnit("P", MakeStats(15), Team.Player, tieBreakRoll: 0f);
            var enemyUnit = new CombatUnit("E", MakeStats(20), Team.Enemy, tieBreakRoll: 0f);

            var order = InitiativeOrder.Build(new[] { playerUnit, enemyUnit });

            Assert.AreEqual(enemyUnit, order[0]);
            Assert.AreEqual(playerUnit, order[1]);
        }

        [Test]
        public void Build_TiedInitiative_BrokenByTieBreakRoll()
        {
            var stats = MakeStats(10);
            var lowRoll = new CombatUnit("Low", stats, Team.Player, tieBreakRoll: 0.2f);
            var highRoll = new CombatUnit("High", stats, Team.Enemy, tieBreakRoll: 0.8f);

            var order = InitiativeOrder.Build(new[] { lowRoll, highRoll });

            CollectionAssert.AreEqual(new[] { highRoll, lowRoll }, order);
        }

        [Test]
        public void Build_TieBreakStaysStableAcrossRounds()
        {
            var stats = MakeStats(10);
            var lowRoll = new CombatUnit("Low", stats, Team.Player, tieBreakRoll: 0.2f);
            var highRoll = new CombatUnit("High", stats, Team.Enemy, tieBreakRoll: 0.8f);

            var round1 = InitiativeOrder.Build(new[] { lowRoll, highRoll });
            var round2 = InitiativeOrder.Build(new[] { lowRoll, highRoll });

            CollectionAssert.AreEqual(round1, round2);
        }

        [Test]
        public void Build_ExcludesDeadUnits()
        {
            var alive = new CombatUnit("Alive", MakeStats(10), Team.Player, tieBreakRoll: 0f);
            var dead = new CombatUnit("Dead", MakeStats(20), Team.Enemy, tieBreakRoll: 0f);
            dead.ApplyDamage(dead.Stats.maxHp);

            var order = InitiativeOrder.Build(new[] { alive, dead });

            CollectionAssert.AreEqual(new[] { alive }, order);
        }
    }
}
