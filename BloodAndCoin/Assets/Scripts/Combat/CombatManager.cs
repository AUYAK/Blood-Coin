using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    public enum CombatState
    {
        Setup,
        RoundStart,
        UnitActing,
        Victory,
    }

    // Setup -> RoundStart -> UnitActing (loop) -> RoundStart | Victory.
    // Queue is rebuilt every round from current Initiative (docs/GDD.md раздел 5): buffs/
    // debuffs can reshuffle it later, the tie-break roll assigned at Setup never changes.
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] private int battlefieldRadius = 5;
        [SerializeField, Min(0.01f)] private float hexSize = 1f;

        public event Action<CombatUnit> TurnStarted;
        public event Action RoundStarted;
        public event Action<Team> BattleEnded;
        // Fired once Grid exists and units are placed — lets HexGridView build its cells
        // without depending on MonoBehaviour Start() ordering against CombatBootstrapper.
        public event Action GridReady;

        public CombatState State { get; private set; } = CombatState.Setup;
        public BattlefieldGrid Grid { get; private set; }
        public float HexSize => hexSize;
        public IReadOnlyList<CombatUnit> InitiativeQueueSnapshot => _initiativeQueue;

        private readonly List<CombatUnit> _allUnits = new List<CombatUnit>();
        private List<CombatUnit> _initiativeQueue = new List<CombatUnit>();
        private int _queueIndex;

        public CombatUnit CurrentUnit =>
            _queueIndex >= 0 && _queueIndex < _initiativeQueue.Count ? _initiativeQueue[_queueIndex] : null;

        // Units must already have their intended starting HexCoord set on Position — this
        // places each one onto the freshly created grid before the first round begins.
        public void SetupBattle(IEnumerable<CombatUnit> units)
        {
            Grid = BattlefieldGrid.CreateHexagonal(battlefieldRadius);
            _allUnits.Clear();
            _allUnits.AddRange(units);

            foreach (var unit in _allUnits)
                Grid.PlaceUnit(unit, unit.Position);

            GridReady?.Invoke();
            StartRound();
        }

        public void EndCurrentUnitTurn()
        {
            _queueIndex++;
            AdvanceToNextLivingUnit();
        }

        private void StartRound()
        {
            foreach (var unit in _allUnits.Where(u => u.IsAlive))
                unit.ResetActionPoints();

            _initiativeQueue = InitiativeOrder.Build(_allUnits);
            _queueIndex = 0;
            State = CombatState.RoundStart;
            RoundStarted?.Invoke();
            AdvanceToNextLivingUnit();
        }

        private void AdvanceToNextLivingUnit()
        {
            while (_queueIndex < _initiativeQueue.Count && !_initiativeQueue[_queueIndex].IsAlive)
                _queueIndex++;

            if (CheckVictory())
                return;

            if (_queueIndex >= _initiativeQueue.Count)
            {
                StartRound();
                return;
            }

            State = CombatState.UnitActing;
            TurnStarted?.Invoke(CurrentUnit);
        }

        private bool CheckVictory()
        {
            bool playersAlive = _allUnits.Any(u => u.Team == Team.Player && u.IsAlive);
            bool enemiesAlive = _allUnits.Any(u => u.Team == Team.Enemy && u.IsAlive);

            if (playersAlive && enemiesAlive)
                return false;

            State = CombatState.Victory;
            BattleEnded?.Invoke(playersAlive ? Team.Player : Team.Enemy);
            return true;
        }
    }
}
