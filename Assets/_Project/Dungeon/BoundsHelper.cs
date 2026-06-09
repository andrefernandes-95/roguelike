using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Footprint overlap checks.
    /// One box per room.
    /// </summary>
    public static class BoundsHelper
    {
        const float ShrinkXZ = 0.1f;

        public static Bounds ToWorldBounds(Vector3 position, Quaternion rotation, Vector3 scale, Bounds localFootprint)
        {
            Vector3 scaledCenter = Vector3.Scale(localFootprint.center, scale);

            // First rotate the center, then transalte it
            Vector3 center = position + rotation * scaledCenter;
            Vector3 scaledExtents = Vector3.Scale(localFootprint.extents, scale);

            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new(scaledExtents.x * x, scaledExtents.y * y, scaledExtents.z * z);

                        Vector3 world = center + rotation * corner;
                        min = Vector3.Min(min, world);
                        max = Vector3.Max(max, world);
                    }
                }
            }

            return new Bounds(
                // Get the center
                (min + max) / 2,
                //Get the size
                max - min);
        }

        public static Bounds ToWorldBounds(PlacedRoom room)
        {
            return ToWorldBounds(room.Position, room.Rotation, room.Template.LocalScale, room.Template.Footprint);
        }

        public static Bounds ToWorldBounds(RoomTemplate template, Vector3 position, Quaternion rotation)
        {
            return ToWorldBounds(position, rotation, template.LocalScale, template.Footprint);
        }

        public static bool OverlapsAny(Bounds candidate, IReadOnlyList<Bounds> occupied)
        {
            Bounds shrunk = ShrinkForTest(candidate);
            for (int i = 0; i < occupied.Count; i++)
            {
                if (shrunk.Intersects(ShrinkForTest(occupied[i])))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True if room fits at position / rotation.
        /// Appends world footprint to outFootprint when true.
        /// extraOccupied = bounds already reserved this attempt (connector chain)
        /// </summary>
        public static bool CanPlace(
            RoomTemplate template,
            Vector3 position,
            Quaternion rotation,
            IReadOnlyList<Bounds> occupied,
            IReadOnlyList<Bounds> extraOccupied,
            out Bounds worldFootprint
        )
        {
            worldFootprint = ToWorldBounds(template, position, rotation);

            if (OverlapsAny(worldFootprint, occupied))
            {
                return false;
            }

            if (extraOccupied != null && extraOccupied.Count > 0 && OverlapsAny(worldFootprint, extraOccupied))
            {
                return false;
            }

            return true;
        }

        public static bool CanPlace(
            RoomTemplate template,
            Vector3 position,
            Quaternion rotation,
            IReadOnlyList<Bounds> occupied,
            out Bounds worldFootprint
        )
        {
            return CanPlace(template, position, rotation, occupied, null, out worldFootprint);
        }

        /// <summary>
        /// Slightly reduces room bounds so overlap checks are more forgiving and don’t reject valid placements.
        /// </summary>
        static Bounds ShrinkForTest(Bounds bounds)
        {
            Vector3 size = bounds.size;
            size.x = Mathf.Max(0.01f, size.x - ShrinkXZ);
            size.z = Mathf.Max(0.01f, size.z - ShrinkXZ);
            size.y = Mathf.Max(1f, size.y);

            return new Bounds(bounds.center, size);
        }
    }
}
