using UnityEngine;
using UnityEngine.Serialization;

namespace BloodAndCoin.Combat
{
    // Minimal stat subset for the first combat slice (GDD раздел 5). Cover/morale/stamina/
    // armor-by-zone are intentionally out of scope until the turn/initiative core is
    // validated — see docs/GDD.md.
    [CreateAssetMenu(fileName = "UnitStats", menuName = "Blood and Coin/Combat/Unit Stats Definition")]
    public class UnitStatsDefinition : ScriptableObject
    {
        [Header("Идентификация")]
        public string unitName = "Unit";

        [Header("Статы")]
        [Min(1)] public int maxHp = 20;
        // Per-battle pool that absorbs damage before HP (see CombatUnit.ApplyDamage) — resets
        // at battle setup, not a persistent gear-durability mechanic (that was deliberately cut,
        // see docs/specializations.md on removing the Engineer profession).
        [Min(0)] public int maxArmor = 0;
        [Min(0)] public int initiative = 10;
        [Range(0, 100)] public int meleeAccuracy = 60;
        [Range(0, 100)] public int meleeDefense = 20;
        [Min(0)] public int meleeDamage = 5;
        // Every unit can be shot at regardless of whether it shoots itself — GDD раздел 5
        // lists ranged defense as a stat every fighter has, not just archers.
        [Range(0, 100)] public int rangedDefense = 10;

        [Header("Очки действий")]
        [Min(1)] public int actionPoints = 4;
        [FormerlySerializedAs("attackActionPointCost")]
        [Min(1)] public int meleeActionPointCost = 2;

        [Header("Дальний бой (лучники/арбалетчики)")]
        public bool isRanged = false;
        [Range(0, 100)] public int rangedAccuracy = 60;
        [Min(0)] public int rangedDamage = 4;
        [Min(2)] public int rangedRange = 3;
        [Min(1)] public int rangedActionPointCost = 2;
    }
}
