using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Minimal stat subset for the first combat slice (GDD раздел 5). Ranged/cover/morale/
    // stamina/armor-by-zone are intentionally out of scope until the turn/initiative core
    // is validated — see docs/GDD.md.
    [CreateAssetMenu(fileName = "UnitStats", menuName = "Blood and Coin/Combat/Unit Stats Definition")]
    public class UnitStatsDefinition : ScriptableObject
    {
        [Header("Идентификация")]
        public string unitName = "Unit";

        [Header("Статы")]
        [Min(1)] public int maxHp = 20;
        [Min(0)] public int initiative = 10;
        [Range(0, 100)] public int meleeAccuracy = 60;
        [Range(0, 100)] public int meleeDefense = 20;
        [Min(0)] public int meleeDamage = 5;

        [Header("Очки действий")]
        [Min(1)] public int actionPoints = 4;
        [Min(1)] public int attackActionPointCost = 2;
    }
}
