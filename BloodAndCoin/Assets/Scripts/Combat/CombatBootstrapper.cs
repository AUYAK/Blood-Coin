using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    [Serializable]
    public struct UnitSpawnData
    {
        public string unitName;
        public UnitStatsDefinition stats;
        public int axialQ;
        public int axialR;
    }

    // Spawns a battle from inspector-configured squads. If unitViewPrefab is left empty, a
    // bare colored-square GameObject is generated at runtime, so the prototype is playable
    // right after wiring CombatManager/CombatActionExecutor/CombatBootstrapper into a scene
    // — no prefab or ScriptableObject assets have to exist yet.
    public class CombatBootstrapper : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private CombatActionExecutor actions;
        [SerializeField] private GameObject unitViewPrefab;
        [SerializeField] private List<UnitSpawnData> playerSquad = new List<UnitSpawnData>();
        [SerializeField] private List<UnitSpawnData> enemySquad = new List<UnitSpawnData>();

        private void Start()
        {
            var units = new List<CombatUnit>();

            foreach (var spawn in playerSquad)
                units.Add(SpawnUnit(spawn, Team.Player));
            foreach (var spawn in enemySquad)
                units.Add(SpawnUnit(spawn, Team.Enemy));

            combatManager.SetupBattle(units);
        }

        private CombatUnit SpawnUnit(UnitSpawnData spawn, Team team)
        {
            if (spawn.stats == null)
                throw new InvalidOperationException($"{spawn.unitName}: UnitStatsDefinition не назначен в CombatBootstrapper.");

            var unit = new CombatUnit(spawn.unitName, spawn.stats, team, UnityEngine.Random.value)
            {
                Position = HexCoord.FromAxial(spawn.axialQ, spawn.axialR),
            };

            var view = CreateView(spawn.unitName, team, spawn.stats.isRanged);
            view.Bind(unit, combatManager.HexSize);
            actions.RegisterView(unit, view);

            return unit;
        }

        private CombatUnitView CreateView(string unitName, Team team, bool isRanged)
        {
            GameObject go = unitViewPrefab != null ? Instantiate(unitViewPrefab) : CreateDefaultViewObject(team, isRanged);
            go.name = unitName;

            var view = go.GetComponent<CombatUnitView>();
            if (view == null)
                view = go.AddComponent<CombatUnitView>();

            return view;
        }

        // Ranged units get a shifted tint (still recognizably the same team) so they're
        // distinguishable on the battlefield without needing real sprites yet.
        private static GameObject CreateDefaultViewObject(Team team, bool isRanged)
        {
            var go = new GameObject("Unit", typeof(SpriteRenderer), typeof(CombatUnitView));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = SquareSpriteFactory.GetOrCreate();

            if (team == Team.Player)
                renderer.color = isRanged ? new Color(0.2f, 0.8f, 0.7f) : new Color(0.2f, 0.4f, 0.9f);
            else
                renderer.color = isRanged ? new Color(0.9f, 0.6f, 0.15f) : new Color(0.8f, 0.2f, 0.2f);

            return go;
        }
    }
}
