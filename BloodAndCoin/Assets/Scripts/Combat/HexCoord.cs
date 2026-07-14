using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Cube coordinates (x+y+z=0). World-position math assumes a pointy-top layout and is
    // independent of Unity's Tilemap cell-coordinate scheme on purpose: the Tilemap in the
    // scene is decorative background art only, not the source of truth for grid logic.
    [Serializable]
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public HexCoord(int x, int y, int z)
        {
            if (x + y + z != 0)
                throw new ArgumentException($"Cube coordinates must sum to zero: ({x}, {y}, {z})");
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly HexCoord Zero = new HexCoord(0, 0, 0);

        public static HexCoord FromAxial(int q, int r) => new HexCoord(q, -q - r, r);

        public int Q => X;
        public int R => Z;

        private static readonly HexCoord[] Directions =
        {
            new HexCoord(1, -1, 0), new HexCoord(1, 0, -1), new HexCoord(0, 1, -1),
            new HexCoord(-1, 1, 0), new HexCoord(-1, 0, 1), new HexCoord(0, -1, 1),
        };

        public HexCoord Neighbor(int direction) => this + Directions[((direction % 6) + 6) % 6];

        public IEnumerable<HexCoord> Neighbors()
        {
            for (int i = 0; i < 6; i++)
                yield return Neighbor(i);
        }

        public int DistanceTo(HexCoord other) =>
            (Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z)) / 2;

        public static HexCoord operator +(HexCoord a, HexCoord b) =>
            new HexCoord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static HexCoord operator -(HexCoord a, HexCoord b) =>
            new HexCoord(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);

        public bool Equals(HexCoord other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);

        public override int GetHashCode()
        {
            // Manual combine instead of System.HashCode — avoids depending on the project's
            // Api Compatibility Level supporting netstandard2.1.
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                hash = hash * 31 + Z;
                return hash;
            }
        }

        public override string ToString() => $"Hex({X}, {Y}, {Z})";

        public Vector2 ToWorldPosition(float hexSize)
        {
            float worldX = hexSize * (Mathf.Sqrt(3f) * Q + Mathf.Sqrt(3f) / 2f * R);
            float worldY = hexSize * (3f / 2f * R);
            return new Vector2(worldX, worldY);
        }

        public static HexCoord FromWorldPosition(Vector2 worldPosition, float hexSize)
        {
            float q = (Mathf.Sqrt(3f) / 3f * worldPosition.x - 1f / 3f * worldPosition.y) / hexSize;
            float r = (2f / 3f * worldPosition.y) / hexSize;
            return RoundAxial(q, r);
        }

        // True if localPosition (relative to this cell's own center) falls inside a hex of
        // the given size centered at the origin. Reuses FromWorldPosition so HexSpriteFactory
        // can rasterize a shape that can never drift from what click-to-move treats as
        // "inside this hex" — including the shrunk-size trick it uses to draw a border ring.
        public static bool ContainsPoint(Vector2 localPosition, float hexSize) =>
            FromWorldPosition(localPosition, hexSize) == Zero;

        private static HexCoord RoundAxial(float q, float r)
        {
            float x = q;
            float z = r;
            float y = -x - z;

            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);

            float xDiff = Mathf.Abs(rx - x);
            float yDiff = Mathf.Abs(ry - y);
            float zDiff = Mathf.Abs(rz - z);

            if (xDiff > yDiff && xDiff > zDiff)
                rx = -ry - rz;
            else if (yDiff > zDiff)
                ry = -rx - rz;
            else
                rz = -rx - ry;

            return new HexCoord(rx, ry, rz);
        }
    }
}
