using NUnit.Framework;
using UnityEngine;

namespace BloodAndCoin.Combat.Tests
{
    public class HexCoordTests
    {
        [Test]
        public void Constructor_ThrowsWhenCoordinatesDoNotSumToZero()
        {
            Assert.Throws<System.ArgumentException>(() => new HexCoord(1, 1, 1));
        }

        [Test]
        public void DistanceTo_SameCell_IsZero()
        {
            var a = new HexCoord(2, -3, 1);
            Assert.AreEqual(0, a.DistanceTo(a));
        }

        [Test]
        public void DistanceTo_AdjacentCell_IsOne()
        {
            var origin = HexCoord.Zero;
            foreach (var neighbor in origin.Neighbors())
                Assert.AreEqual(1, origin.DistanceTo(neighbor));
        }

        [Test]
        public void DistanceTo_KnownFarCell_MatchesManualCount()
        {
            var origin = HexCoord.Zero;
            var target = new HexCoord(3, -3, 0);
            Assert.AreEqual(3, origin.DistanceTo(target));
        }

        [Test]
        public void Neighbors_ReturnsSixDistinctCells()
        {
            var origin = HexCoord.Zero;
            var neighbors = new System.Collections.Generic.HashSet<HexCoord>(origin.Neighbors());
            Assert.AreEqual(6, neighbors.Count);
        }

        [Test]
        public void WorldRoundTrip_ReturnsOriginalCoordinate()
        {
            const float hexSize = 1f;
            for (int q = -4; q <= 4; q++)
            {
                for (int r = -4; r <= 4; r++)
                {
                    var original = HexCoord.FromAxial(q, r);
                    var world = original.ToWorldPosition(hexSize);
                    var recovered = HexCoord.FromWorldPosition(world, hexSize);
                    Assert.AreEqual(original, recovered, $"Round-trip failed for axial ({q},{r})");
                }
            }
        }

        [Test]
        public void FromWorldPosition_NearCellCenter_SnapsToThatCell()
        {
            var expected = new HexCoord(1, -1, 0);
            var center = expected.ToWorldPosition(1f);
            var jittered = center + new Vector2(0.1f, -0.05f);

            Assert.AreEqual(expected, HexCoord.FromWorldPosition(jittered, 1f));
        }

        [Test]
        public void ContainsPoint_CellCenter_IsTrue()
        {
            Assert.IsTrue(HexCoord.ContainsPoint(Vector2.zero, 1f));
        }

        [Test]
        public void ContainsPoint_WellInsideNeighborCell_IsFalse()
        {
            var neighborCenter = HexCoord.Zero.Neighbor(0).ToWorldPosition(1f);
            Assert.IsFalse(HexCoord.ContainsPoint(neighborCenter, 1f));
        }

        [Test]
        public void ContainsPoint_AtSmallerScale_ShrinksTheAcceptedRegion()
        {
            // A point near the true edge of the hex should still count at full size but
            // fall outside a shrunk test — this is exactly the trick HexSpriteFactory uses
            // to rasterize a border ring.
            var nearEdge = new Vector2(0f, 0.95f); // just inside the top vertex at hexSize=1
            Assert.IsTrue(HexCoord.ContainsPoint(nearEdge, 1f));
            Assert.IsFalse(HexCoord.ContainsPoint(nearEdge, 0.88f));
        }
    }
}
