using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Hit-chance formula is a tunable placeholder (docs/GDD.md раздел 7/10 flags most combat
    // formulas as "ready for playtest tuning", not final) — only the shape (accuracy vs
    // defense, clamped) is meant to survive balancing passes.
    public static class MeleeAttackResolver
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

        public static AttackResult Resolve(CombatUnit attacker, CombatUnit defender, System.Random random)
        {
            int hitChance = Mathf.Clamp(
                BaseHitChance + attacker.Stats.meleeAccuracy - defender.Stats.meleeDefense,
                MinHitChance, MaxHitChance);

            bool hit = random.Next(0, 100) < hitChance;
            if (!hit)
                return new AttackResult(false, 0);

            int damage = attacker.Stats.meleeDamage;
            defender.ApplyDamage(damage);
            return new AttackResult(true, damage);
        }
    }
}
