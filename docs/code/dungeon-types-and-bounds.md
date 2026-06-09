# Dungeon slice 1 — types & bounds

## Goal

Plain C# data for the dungeon solver + one dumb overlap helper. **No solver yet.** No MonoBehaviour yet.

- One `Bounds Footprint` per room (not floor tiles)
- `AF.Dungeon` asmdef → references `AF.Core` only (Core unused here; keeps dependency direction for later generator)

---

## Files

```
Assets/_Project/Dungeon/
├── AF.Dungeon.asmdef
└── Runtime/
    ├── DungeonTypes.cs
    └── BoundsHelper.cs

Assets/_Project/Tests/EditMode/
├── AF.Tests.EditMode.asmdef
├── BoundsHelperTests.cs
└── RoomTemplateTests.cs
```

---

### `Assets/_Project/Dungeon/AF.Dungeon.asmdef`

```json
{
    "name": "AF.Dungeon",
    "rootNamespace": "AF.Dungeon",
    "references": [
        "AF.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

### `Assets/_Project/Dungeon/Runtime/DungeonTypes.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>Doorway on a room template. Solver marks isConnected when used.</summary>
    public sealed class DoorSocket
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public bool IsConnected;

        public DoorSocket(Vector3 localPosition, Quaternion localRotation)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            IsConnected = false;
        }

        public DoorSocket Clone()
        {
            return new DoorSocket(LocalPosition, LocalRotation)
            {
                IsConnected = IsConnected
            };
        }
    }

    /// <summary>Room blueprint for the solver. No GameObject — adapter maps Id to prefab when spawning.</summary>
    public sealed class RoomTemplate
    {
        public string Id;
        public Vector3 LocalScale = Vector3.one;

        /// <summary>Local-space AABB of the room footprint (from RoomBounds collider).</summary>
        public Bounds Footprint;

        public List<DoorSocket> Entrances = new List<DoorSocket>();
        public List<DoorSocket> Exits = new List<DoorSocket>();

        public RoomTemplate(string id)
        {
            Id = id;
        }

        public RoomTemplate Clone()
        {
            var copy = new RoomTemplate(Id)
            {
                LocalScale = LocalScale,
                Footprint = Footprint
            };

            foreach (DoorSocket e in Entrances)
            {
                copy.Entrances.Add(e.Clone());
            }

            foreach (DoorSocket x in Exits)
            {
                copy.Exits.Add(x.Clone());
            }

            return copy;
        }
    }

    /// <summary>Room after the solver picked position and rotation.</summary>
    public sealed class PlacedRoom
    {
        public RoomTemplate Template;
        public Vector3 Position;
        public Quaternion Rotation;

        public PlacedRoom(RoomTemplate template, Vector3 position, Quaternion rotation)
        {
            Template = template;
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>Category pool passed into the solver (built from RoomCategoryData in slice 4).</summary>
    public sealed class RoomCategoryConfig
    {
        public string Name;
        public List<RoomTemplate> Templates = new List<RoomTemplate>();
        public float SideRoomChance;

        /// <summary>If null or empty, use dungeon default side category.</summary>
        public List<RoomTemplate> SideRoomTemplates;
    }
}
```

---

### `Assets/_Project/Dungeon/Runtime/BoundsHelper.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>Footprint overlap checks. One box per room.</summary>
    public static class BoundsHelper
    {
        const float ShrinkXZ = 0.1f;

        /// <summary>World-axis-aligned bounds for a oriented local footprint.</summary>
        public static Bounds ToWorldBounds(Vector3 position, Quaternion rotation, Vector3 scale, Bounds localFootprint)
        {
            Vector3 scaledCenter = Vector3.Scale(localFootprint.center, scale);
            Vector3 center = position + rotation * scaledCenter;
            Vector3 scaledExtents = Vector3.Scale(localFootprint.extents, scale);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new Vector3(
                            scaledExtents.x * x,
                            scaledExtents.y * y,
                            scaledExtents.z * z);

                        Vector3 world = center + rotation * corner;
                        min = Vector3.Min(min, world);
                        max = Vector3.Max(max, world);
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
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
        /// True if room fits at position/rotation. Appends world footprint to outFootprint when true.
        /// extraOccupied = bounds already reserved this attempt (connector chain).
        /// </summary>
        public static bool CanPlace(
            RoomTemplate template,
            Vector3 position,
            Quaternion rotation,
            IReadOnlyList<Bounds> occupied,
            IReadOnlyList<Bounds> extraOccupied,
            out Bounds worldFootprint)
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
            out Bounds worldFootprint)
        {
            return CanPlace(template, position, rotation, occupied, null, out worldFootprint);
        }

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
```

---

### `Assets/_Project/Tests/EditMode/AF.Tests.EditMode.asmdef`

```json
{
    "name": "AF.Tests.EditMode",
    "rootNamespace": "AF.Tests",
    "references": [
        "AF.Dungeon",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

### `Assets/_Project/Tests/EditMode/BoundsHelperTests.cs`

```csharp
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class BoundsHelperTests
    {
        [Test]
        public void ToWorldBounds_AtOrigin_MatchesLocalFootprint()
        {
            Bounds local = new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f));

            Bounds world = BoundsHelper.ToWorldBounds(Vector3.zero, Quaternion.identity, Vector3.one, local);

            Assert.AreEqual(10f, world.size.x, 0.01f);
            Assert.AreEqual(10f, world.size.z, 0.01f);
        }

        [Test]
        public void OverlapsAny_TouchingEdges_DoesNotOverlap()
        {
            Bounds a = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(10f, 2f, 10f));
            Bounds b = new Bounds(new Vector3(10f, 0f, 0f), new Vector3(10f, 2f, 10f));

            Assert.IsFalse(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void OverlapsAny_Overlapping_ReturnsTrue()
        {
            Bounds a = new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f));
            Bounds b = new Bounds(new Vector3(5f, 0f, 0f), new Vector3(10f, 2f, 10f));

            Assert.IsTrue(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void CanPlace_RejectsOverlap()
        {
            RoomTemplate room = TestRooms.Box("A", 10f);
            var occupied = new System.Collections.Generic.List<Bounds>
            {
                BoundsHelper.ToWorldBounds(room, Vector3.zero, Quaternion.identity)
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(5f, 0f, 0f),
                Quaternion.identity,
                occupied,
                out Bounds footprint);

            Assert.IsFalse(ok);
        }

        [Test]
        public void CanPlace_AllowsAdjacent()
        {
            RoomTemplate room = TestRooms.Box("A", 10f);
            var occupied = new System.Collections.Generic.List<Bounds>
            {
                BoundsHelper.ToWorldBounds(room, Vector3.zero, Quaternion.identity)
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(10f, 0f, 0f),
                Quaternion.identity,
                occupied,
                out Bounds footprint);

            Assert.IsTrue(ok);
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/RoomTemplateTests.cs`

```csharp
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    /// <summary>Shared test room builders. Solver tests will reuse this.</summary>
    public static class TestRooms
    {
        /// <summary>5x5 room. Exit at +Z, entrance at -Z facing back.</summary>
        public static RoomTemplate Box(string id, float size)
        {
            float half = size * 0.5f;
            var room = new RoomTemplate(id)
            {
                Footprint = new Bounds(Vector3.zero, new Vector3(size, 2f, size))
            };

            room.Exits.Add(new DoorSocket(new Vector3(0f, 0f, half), Quaternion.identity));
            room.Entrances.Add(new DoorSocket(new Vector3(0f, 0f, -half), Quaternion.Euler(0f, 180f, 0f)));

            return room;
        }
    }

    public class RoomTemplateTests
    {
        [Test]
        public void Clone_CopiesDoorsAndResetsNotRequired()
        {
            RoomTemplate original = TestRooms.Box("Start", 10f);
            original.Exits[0].IsConnected = true;

            RoomTemplate copy = original.Clone();

            Assert.AreNotSame(original, copy);
            Assert.AreEqual(original.Id, copy.Id);
            Assert.AreEqual(1, copy.Exits.Count);
            Assert.IsTrue(copy.Exits[0].IsConnected);
        }
    }
}
```

---

## Unity setup

1. Create folders under `Assets/_Project/Dungeon/` and `Assets/_Project/Tests/EditMode/`.
2. Add the five files above.
3. Let Unity import. Fix compile errors if test asmdef fails — ensure **Test Framework** package is installed (should be via manifest).
4. **Window → General → Test Runner → Edit Mode → Run All.**

No scene objects. No prefabs yet.

---

## What slice 2 will add

`DungeonLayoutSolver.cs` uses:

- `RoomTemplate` / `PlacedRoom` / `DoorSocket`
- `RoomCategoryConfig`
- `BoundsHelper.CanPlace`
- `TestRooms.Box` in solver tests

---

## Verify

- [ ] `AF.Dungeon` compiles
- [ ] `AF.Tests.EditMode` compiles (Editor only)
- [ ] All 6 Edit Mode tests pass
- [ ] No `GameObject` in `DungeonTypes.cs`
- [ ] `RoomTemplate.Clone()` deep-copies door lists

---

## Next delivery

`docs/code/dungeon-solver.md` — `DungeonLayoutSolver` + placement helpers + solver tests (`roomSize = 5`, side rooms, connectors).
