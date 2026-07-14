using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Hit-chance formula is a tunable placeholder (docs/GDD.md раздел 7/10 flags most combat
    // formulas as "ready for playtest tuning", not final) — only the shape (accuracy vs
    // defense, clamped) is meant to survive balancing passes. One resolver for both melee and
    // ranged: same shape, just reads the matching pair of stats.
    public static class AttackResolver
    {
        private const int MinHitChance = 5;
        private const int MaxHitChance = 95;
        private const int BaseHitChance = 50;

        public readonly struct AttackResult
        {
            public readonly bool Hit;
            public readonly int Damage;

            public AttackResult(bool hit, int damage)
            {
                Hit = hit;
                Damage = damage;
            }
        }

        // No side effects, no random roll — safe to call from UI (e.g. a target-info panel)
        // purely to display the odds before the player commits to an attack.
        public static int CalculateHitChance(CombatUnit attacker, CombatUnit defender, bool ranged)
        {
            int accuracy = ranged ? attacker.Stats.rangedAccuracy : attacker.Stats.meleeAccuracy;
            int defense = ranged ? defender.Stats.rangedDefense : defender.Stats.meleeDefense;
            return Mathf.Clamp(BaseHitChance + accuracy - defense, MinHitChance, MaxHitChance);
        }

        public static AttackResult Resolve(CombatUnit attacker, CombatUnit defender, bool ranged, System.Random random)
        {
            int hitChance = CalculateHitChance(attacker, defender, ranged);
            int damage = ranged ? attacker.Stats.rangedDamage : attacker.Stats.meleeDamage;

            bool hit = random.Next(0, 100) < hitChance;
            if (!hit)
                return new AttackResult(false, 0);

            defender.ApplyDamage(damage);
            return new AttackResult(true, damage);
        }
    }
}
