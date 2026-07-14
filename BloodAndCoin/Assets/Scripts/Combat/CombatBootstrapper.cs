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
    // right after wiring CombatManager/PlayerInputController/CombatBootstrapper into a scene
    // — no prefab or ScriptableObject assets have to exist yet.
    public class CombatBootstrapper : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private PlayerInputController inputController;
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

            var view = CreateView(spawn.unitName, team);
            view.Bind(unit, combatManager.HexSize);
            inputController.RegisterView(unit, view);

            return unit;
        }

        private CombatUnitView CreateView(string unitName, Team team)
        {
            GameObject go = unitViewPrefab != null ? Instantiate(unitViewPrefab) : CreateDefaultViewObject(team);
            go.name = unitName;

            var view = go.GetComponent<CombatUnitView>();
            if (view == null)
                view = go.AddComponent<CombatUnitView>();

            return view;
        }

        private static GameObject CreateDefaultViewObject(Team team)
        {
            var go = new GameObject("Unit", typeof(SpriteRenderer), typeof(CombatUnitView));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = SquareSpriteFactory.GetOrCreate();
            renderer.color = team == Team.Player ? new Color(0.2f, 0.4f, 0.9f) : new Color(0.8f, 0.2f, 0.2f);
            return go;
        }
    }
}
