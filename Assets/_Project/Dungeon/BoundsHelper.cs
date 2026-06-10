using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Floor-tile overlap checks (Cacildes-style). Uses yaw-only rotation for the floor plan.
    /// </summary>
    public static class BoundsHelper
    {
        const float ShrinkXZ = 0.1f;

        public static Bounds ToWorldBounds(Vector3 position, Quaternion rotation, Vector3 scale, Bounds localTile)
        {
            Quaternion yaw = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            Vector3 scaledCenter = Vector3.Scale(localTile.center, scale);
            Vector3 center = position + yaw * scaledCenter;
            Vector3 scaledExtents = Vector3.Scale(localTile.extents, scale);

            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new(scaledExtents.x * x, scaledExtents.y * y, scaledExtents.z * z);
                        Vector3 world = center + yaw * corner;
                        min = Vector3.Min(min, world);
                        max = Vector3.Max(max, world);
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static List<Bounds> ToWorldTiles(RoomTemplate template, Vector3 position, Quaternion rotation)
        {
            var worldTiles = new List<Bounds>(template.FloorTiles.Count);
            for (int i = 0; i < template.FloorTiles.Count; i++)
            {
                worldTiles.Add(ToWorldBounds(position, rotation, template.LocalScale, template.FloorTiles[i]));
            }

            return worldTiles;
        }

        public static bool CanPlace(
            RoomTemplate template,
            Vector3 position,
            Quaternion rotation,
            IReadOnlyList<Bounds> occupied,
            IReadOnlyList<Bounds> extraOccupied,
            out List<Bounds> worldTiles)
        {
            worldTiles = ToWorldTiles(template, position, rotation);
            if (worldTiles.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < worldTiles.Count; i++)
            {
                Bounds shrunkNew = ShrinkForTest(worldTiles[i]);

                if (OverlapsAny(shrunkNew, occupied))
                {
                    return false;
                }

                if (extraOccupied != null && extraOccupied.Count > 0 && OverlapsAny(shrunkNew, extraOccupied))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CanPlace(
            RoomTemplate template,
            Vector3 position,
            Quaternion rotation,
            IReadOnlyList<Bounds> occupied,
            out List<Bounds> worldTiles)
        {
            return CanPlace(template, position, rotation, occupied, null, out worldTiles);
        }

        public static bool OverlapsAny(Bounds candidate, IReadOnlyList<Bounds> occupied)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                if (candidate.Intersects(ShrinkForTest(occupied[i])))
                {
                    return true;
                }
            }

            return false;
        }

        static Bounds ShrinkForTest(Bounds bounds)
        {
            Vector3 size = bounds.size;
            size.x = Mathf.Max(0.01f, size.x - ShrinkXZ);
            size.z = Mathf.Max(0.01f, size.z - ShrinkXZ);
            size.y = Mathf.Max(0.5f, size.y);

            return new Bounds(bounds.center, size);
        }
    }
}
