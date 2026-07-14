using System;
using System.Collections.Generic;

namespace BloodAndCoin.Combat
{
    // Pure logic grid: cell occupancy + reachability. Deliberately has no dependency on
    // Unity's Tilemap — see HexCoord for why the visual grid and the logic grid are separate.
    public class BattlefieldGrid
    {
        private readonly HashSet<HexCoord> _validCells;
        private readonly Dictionary<HexCoord, CombatUnit> _occupants = new Dictionary<HexCoord, CombatUnit>();

        public BattlefieldGrid(IEnumerable<HexCoord> validCells)
        {
            _validCells = new HashSet<HexCoord>(validCells);
        }

        public static BattlefieldGrid CreateHexagonal(int radius)
        {
            var cells = new List<HexCoord>();
            for (int x = -radius; x <= radius; x++)
            {
                int yMin = Math.Max(-radius, -x - radius);
                int yMax = Math.Min(radius, -x + radius);
                for (int y = yMin; y <= yMax; y++)
                {
                    int z = -x - y;
                    cells.Add(new HexCoord(x, y, z));
                }
            }

            return new BattlefieldGrid(cells);
        }

        public IReadOnlyCollection<HexCoord> ValidCells => _validCells;

        public bool IsValidCell(HexCoord cell) => _validCells.Contains(cell);

        public bool IsOccupied(HexCoord cell) => _occupants.ContainsKey(cell);

        public CombatUnit GetOccupant(HexCoord cell) =>
            _occupants.TryGetValue(cell, out var unit) ? unit : null;

        public void PlaceUnit(CombatUnit unit, HexCoord cell)
        {
            if (!IsValidCell(cell))
                throw new ArgumentException($"{cell} is outside the battlefield.");
            if (IsOccupied(cell))
                throw new InvalidOperationException($"{cell} is already occupied.");

            _occupants[cell] = unit;
            unit.Position = cell;
        }

        public void MoveUnit(CombatUnit unit, HexCoord destination)
        {
            if (!IsValidCell(destination))
                throw new ArgumentException($"{destination} is outside the battlefield.");
            if (IsOccupied(destination))
                throw new InvalidOperationException($"{destination} is already occupied.");

            _occupants.Remove(unit.Position);
            _occupants[destination] = unit;
            unit.Position = destination;
        }

        public void RemoveUnit(CombatUnit unit) => _occupants.Remove(unit.Position);

        // Uniform-cost BFS: one action point per hex step. Returns cost-to-reach for every
        // reachable, unoccupied cell within the unit's remaining action points (origin excluded).
        public IReadOnlyDictionary<HexCoord, int> GetReachableCells(HexCoord origin, int movementPoints)
        {
            var costSoFar = new Dictionary<HexCoord, int> { [origin] = 0 };
            var frontier = new Queue<HexCoord>();
            frontier.Enqueue(origin);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int currentCost = costSoFar[current];
                if (currentCost >= movementPoints)
                    continue;

                foreach (var next in current.Neighbors())
                {
                    if (!IsValidCell(next) || IsOccupied(next))
                        continue;

                    int newCost = currentCost + 1;
                    if (costSoFar.ContainsKey(next) && costSoFar[next] <= newCost)
                        continue;

                    costSoFar[next] = newCost;
                    frontier.Enqueue(next);
                }
            }

            costSoFar.Remove(origin);
            return costSoFar;
        }
    }
}
