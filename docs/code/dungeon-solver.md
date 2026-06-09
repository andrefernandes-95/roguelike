# Dungeon slice 2 — layout solver

## Goal

Port Cacildes `DungeonLayoutSolver` into dumb, readable `AF.Dungeon` code:

- **Critical path** `roomSize` rooms (jam default **5**)
- **Side rooms** + **connectors** kept
- **One footprint** per room via `BoundsHelper` (slice 1)
- Two placement helpers instead of 4 copy-pasted loops

**Prerequisite:** slice 1 (`DungeonTypes.cs`, `BoundsHelper.cs`, `TestRooms`) compiles and tests pass.

---

## Files

```
Assets/_Project/Dungeon/
└── DungeonLayoutSolver.cs

Assets/_Project/Tests/EditMode/
└── DungeonLayoutSolverTests.cs
```

---

### `Assets/_Project/Dungeon/DungeonLayoutSolver.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Pure C# dungeon layout. No prefabs — only RoomTemplate data.
    /// </summary>
    public sealed class DungeonLayoutSolver
    {
        readonly List<Bounds> _occupied = new List<Bounds>();
        readonly List<PlacedRoom> _placed = new List<PlacedRoom>();

        public int CurrentActiveSeed { get; private set; }

        public bool TrySolveLayout(
            int roomSize,
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int baseSeed,
            int maxAttempts,
            out List<PlacedRoom> resultRooms,
            out string errorMessage)
        {
            resultRooms = new List<PlacedRoom>();
            errorMessage = "";

            if (layoutSequence == null || layoutSequence.Count == 0)
            {
                errorMessage = "Layout sequence is empty.";
                return false;
            }

            if (roomSize < 2)
            {
                errorMessage = "roomSize must be at least 2.";
                return false;
            }

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                CurrentActiveSeed = baseSeed + (attempt - 1);
                Random.InitState(CurrentActiveSeed);

                _occupied.Clear();
                _placed.Clear();

                if (TryBuild(layoutSequence, defaultSideCategory, connectorCategory, connectorSpawnChance, roomSize, out errorMessage))
                {
                    resultRooms = new List<PlacedRoom>(_placed);
                    return true;
                }
            }

            return false;
        }

        bool TryBuild(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int roomSize,
            out string errorMessage)
        {
            errorMessage = "";

            if (!PlaceStartRoom(layoutSequence, roomSize, out errorMessage))
            {
                return false;
            }

            if (!PlaceMainPath(layoutSequence, connectorCategory, connectorSpawnChance, roomSize, out errorMessage))
            {
                return false;
            }

            PlaceSideRooms(layoutSequence, defaultSideCategory, connectorCategory, connectorSpawnChance);
            return true;
        }

        bool PlaceStartRoom(List<RoomCategoryConfig> layoutSequence, int roomSize, out string errorMessage)
        {
            errorMessage = "";

            RoomCategoryConfig startCategory = GetCategoryForIndex(0, roomSize, layoutSequence);
            if (startCategory == null || startCategory.Templates.Count == 0)
            {
                errorMessage = "Start category is missing or has no templates.";
                return false;
            }

            RoomTemplate startPrefab = startCategory.Templates[Random.Range(0, startCategory.Templates.Count)];
            RoomTemplate startRoom = startPrefab.Clone();

            if (!BoundsHelper.CanPlace(startRoom, Vector3.zero, Quaternion.identity, _occupied, out Bounds footprint))
            {
                errorMessage = "Start room is invalid.";
                return false;
            }

            _occupied.Add(footprint);
            _placed.Add(new PlacedRoom(startRoom, Vector3.zero, Quaternion.identity));
            return true;
        }

        bool PlaceMainPath(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int roomSize,
            out string errorMessage)
        {
            errorMessage = "";
            PlacedRoom currentRoom = _placed[0];
            bool pathCompleted = false;

            for (int step = 1; step < roomSize; step++)
            {
                bool isBossStep = step == roomSize - 1;
                RoomCategoryConfig category = isBossStep
                    ? layoutSequence[layoutSequence.Count - 1]
                    : GetCategoryForIndex(step, roomSize, layoutSequence);

                if (category == null || category.Templates.Count == 0)
                {
                    errorMessage = $"Category for step {step} is missing or empty.";
                    return false;
                }

                if (!TryPlaceNextOnPath(currentRoom, category, connectorCategory, connectorSpawnChance, out PlacedRoom newRoom))
                {
                    errorMessage = $"Failed to place room at step {step} (roomSize={roomSize}, placed={_placed.Count}).";
                    return false;
                }

                currentRoom = newRoom;

                if (isBossStep)
                {
                    pathCompleted = true;
                }
            }

            if (!pathCompleted)
            {
                errorMessage = "Critical path was not completed.";
                return false;
            }

            return true;
        }

        bool TryPlaceNextOnPath(
            PlacedRoom currentRoom,
            RoomCategoryConfig category,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            out PlacedRoom newCurrentRoom)
        {
            newCurrentRoom = currentRoom;

            List<RoomTemplate> prefabs = new List<RoomTemplate>(category.Templates);
            Shuffle(prefabs);

            foreach (RoomTemplate prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                List<DoorSocket> exits = GetFreeExits(currentRoom.Template);
                if (exits.Count == 0)
                {
                    break;
                }

                Shuffle(exits);

                foreach (DoorSocket exit in exits)
                {
                    bool wantConnector = connectorCategory != null
                        && connectorCategory.Templates.Count > 0
                        && Random.value < connectorSpawnChance;

                    if (wantConnector && TryAttachWithConnector(currentRoom, exit, prefab, connectorCategory, out PlacedRoom afterConnector))
                    {
                        newCurrentRoom = afterConnector;
                        return true;
                    }

                    if (TryAttachDirect(currentRoom, exit, prefab, out PlacedRoom placed))
                    {
                        newCurrentRoom = placed;
                        return true;
                    }
                }
            }

            return false;
        }

        void PlaceSideRooms(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance)
        {
            List<PlacedRoom> snapshot = new List<PlacedRoom>(_placed);

            foreach (PlacedRoom room in snapshot)
            {
                RoomCategoryConfig sourceCategory = FindCategoryForRoom(room, layoutSequence, defaultSideCategory);
                float sideChance = sourceCategory != null ? sourceCategory.SideRoomChance : 0f;
                List<RoomTemplate> sidePrefabs = GetSideTemplates(sourceCategory, defaultSideCategory);

                if (sidePrefabs == null || sidePrefabs.Count == 0)
                {
                    continue;
                }

                List<DoorSocket> exits = GetFreeExits(room.Template);
                foreach (DoorSocket exit in exits)
                {
                    if (Random.value >= sideChance)
                    {
                        continue;
                    }

                    TryPlaceSideBranch(room, exit, sidePrefabs, connectorCategory, connectorSpawnChance);
                }
            }
        }

        void TryPlaceSideBranch(
            PlacedRoom fromRoom,
            DoorSocket exit,
            List<RoomTemplate> sidePrefabs,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance)
        {
            List<RoomTemplate> shuffled = new List<RoomTemplate>(sidePrefabs);
            Shuffle(shuffled);

            foreach (RoomTemplate prefab in shuffled)
            {
                if (prefab == null)
                {
                    continue;
                }

                bool wantConnector = connectorCategory != null
                    && connectorCategory.Templates.Count > 0
                    && Random.value < connectorSpawnChance;

                if (wantConnector && TryAttachWithConnector(fromRoom, exit, prefab, connectorCategory, out _))
                {
                    return;
                }

                if (TryAttachDirect(fromRoom, exit, prefab, out _))
                {
                    return;
                }
            }
        }

        bool TryAttachDirect(PlacedRoom fromRoom, DoorSocket exit, RoomTemplate prefab, out PlacedRoom newRoom)
        {
            newRoom = null;

            RoomTemplate room = prefab.Clone();
            DoorSocket entrance = GetFirstFreeEntrance(room);
            if (entrance == null)
            {
                return false;
            }

            AlignRooms(fromRoom, exit, room, entrance, out Vector3 pos, out Quaternion rot);

            if (!BoundsHelper.CanPlace(room, pos, rot, _occupied, out Bounds footprint))
            {
                return false;
            }

            exit.IsConnected = true;
            entrance.IsConnected = true;
            _occupied.Add(footprint);
            newRoom = new PlacedRoom(room, pos, rot);
            _placed.Add(newRoom);
            return true;
        }

        bool TryAttachWithConnector(
            PlacedRoom fromRoom,
            DoorSocket exit,
            RoomTemplate prefab,
            RoomCategoryConfig connectorCategory,
            out PlacedRoom newRoom)
        {
            newRoom = null;

            List<RoomTemplate> connectors = new List<RoomTemplate>(connectorCategory.Templates);
            Shuffle(connectors);

            foreach (RoomTemplate connPrefab in connectors)
            {
                if (connPrefab == null)
                {
                    continue;
                }

                RoomTemplate connector = connPrefab.Clone();
                DoorSocket connEntrance = GetFirstFreeEntrance(connector);
                if (connEntrance == null)
                {
                    continue;
                }

                AlignRooms(fromRoom, exit, connector, connEntrance, out Vector3 connPos, out Quaternion connRot);

                if (!BoundsHelper.CanPlace(connector, connPos, connRot, _occupied, out Bounds connFootprint))
                {
                    continue;
                }

                PlacedRoom placedConnector = new PlacedRoom(connector, connPos, connRot);
                List<DoorSocket> connExits = GetFreeExits(connector);

                foreach (DoorSocket connExit in connExits)
                {
                    RoomTemplate room = prefab.Clone();
                    DoorSocket entrance = GetFirstFreeEntrance(room);
                    if (entrance == null)
                    {
                        continue;
                    }

                    AlignRooms(placedConnector, connExit, room, entrance, out Vector3 roomPos, out Quaternion roomRot);

                    if (!BoundsHelper.CanPlace(room, roomPos, roomRot, _occupied, new[] { connFootprint }, out Bounds roomFootprint))
                    {
                        continue;
                    }

                    exit.IsConnected = true;
                    connEntrance.IsConnected = true;
                    connExit.IsConnected = true;
                    entrance.IsConnected = true;

                    _occupied.Add(connFootprint);
                    _occupied.Add(roomFootprint);

                    _placed.Add(placedConnector);
                    newRoom = new PlacedRoom(room, roomPos, roomRot);
                    _placed.Add(newRoom);
                    return true;
                }
            }

            return false;
        }

        // --- Category pick (ported from Cacildes — do not "improve") ---

        static RoomCategoryConfig GetCategoryForIndex(int index, int totalRooms, List<RoomCategoryConfig> layoutSequence)
        {
            int count = layoutSequence.Count;
            if (index == 0)
            {
                return layoutSequence[0];
            }

            if (index == totalRooms - 1)
            {
                return layoutSequence[count - 1];
            }

            if (count <= 2)
            {
                return layoutSequence[Mathf.Clamp(index, 0, count - 1)];
            }

            if (totalRooms <= 3)
            {
                return layoutSequence[count / 2];
            }

            float t = (float)(index - 1) / (totalRooms - 3);
            int midCount = count - 2;
            int categoryIndex = 1 + Mathf.Clamp(Mathf.RoundToInt(t * (midCount - 1)), 0, midCount - 1);
            return layoutSequence[categoryIndex];
        }

        static RoomCategoryConfig FindCategoryForRoom(
            PlacedRoom placed,
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory)
        {
            foreach (RoomCategoryConfig category in layoutSequence)
            {
                if (category == null)
                {
                    continue;
                }

                foreach (RoomTemplate template in category.Templates)
                {
                    if (template != null && template.Id == placed.Template.Id)
                    {
                        return category;
                    }
                }
            }

            if (defaultSideCategory != null)
            {
                foreach (RoomTemplate template in defaultSideCategory.Templates)
                {
                    if (template != null && template.Id == placed.Template.Id)
                    {
                        return defaultSideCategory;
                    }
                }
            }

            return null;
        }

        static List<RoomTemplate> GetSideTemplates(RoomCategoryConfig sourceCategory, RoomCategoryConfig defaultSideCategory)
        {
            if (sourceCategory != null
                && sourceCategory.SideRoomTemplates != null
                && sourceCategory.SideRoomTemplates.Count > 0)
            {
                return sourceCategory.SideRoomTemplates;
            }

            if (defaultSideCategory != null)
            {
                return defaultSideCategory.Templates;
            }

            return null;
        }

        // --- Door helpers ---

        static List<DoorSocket> GetFreeExits(RoomTemplate template)
        {
            List<DoorSocket> list = new List<DoorSocket>();
            foreach (DoorSocket exit in template.Exits)
            {
                if (!exit.IsConnected)
                {
                    list.Add(exit);
                }
            }

            return list;
        }

        static DoorSocket GetFirstFreeEntrance(RoomTemplate template)
        {
            foreach (DoorSocket entrance in template.Entrances)
            {
                if (!entrance.IsConnected)
                {
                    return entrance;
                }
            }

            return null;
        }

        // --- Alignment (ported from Cacildes AlignLogicalRooms) ---

        static void AlignRooms(
            PlacedRoom roomA,
            DoorSocket exit,
            RoomTemplate roomB,
            DoorSocket entrance,
            out Vector3 targetPos,
            out Quaternion targetRot)
        {
            Vector3 scaledExit = Vector3.Scale(exit.LocalPosition, roomA.Template.LocalScale);
            Vector3 exitWorldPos = roomA.Position + roomA.Rotation * scaledExit;
            Quaternion exitWorldRot = roomA.Rotation * exit.LocalRotation;

            targetRot = exitWorldRot * Quaternion.Inverse(entrance.LocalRotation);

            Vector3 scaledEntrance = Vector3.Scale(entrance.LocalPosition, roomB.LocalScale);
            Vector3 rotatedEntrance = targetRot * scaledEntrance;
            Vector3 exitForward = exitWorldRot * Vector3.forward;
            Vector3 separation = exitForward * 0.000005f;

            targetPos = exitWorldPos - rotatedEntrance + separation;
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/DungeonLayoutSolverTests.cs`

Uses `TestRooms` from `RoomTemplateTests.cs` (slice 1).

```csharp
using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class DungeonLayoutSolverTests
    {
        static List<RoomCategoryConfig> CreateSequence(RoomTemplate start, RoomTemplate mid, RoomTemplate end)
        {
            var startCat = new RoomCategoryConfig { Name = "Start" };
            startCat.Templates.Add(start);

            var midCat = new RoomCategoryConfig { Name = "Mid" };
            midCat.Templates.Add(mid);

            var endCat = new RoomCategoryConfig { Name = "End" };
            endCat.Templates.Add(end);

            return new List<RoomCategoryConfig> { startCat, midCat, endCat };
        }

        [Test]
        public void TrySolveLayout_StartRoom_AtOrigin()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f));

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                roomSize: 3,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 10,
                maxAttempts: 1,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(Vector3.zero, result[0].Position);
            Assert.AreEqual(Quaternion.identity, result[0].Rotation);
        }

        [Test]
        public void TrySolveLayout_CriticalPath_CountEqualsRoomSize()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f));

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                roomSize: 5,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 42,
                maxAttempts: 10,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual("Start", result[0].Template.Id);
            Assert.AreEqual("End", result[4].Template.Id);
        }

        [Test]
        public void TrySolveLayout_SameSeed_SameLayout()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f));

            var solverA = new DungeonLayoutSolver();
            solverA.TrySolveLayout(3, sequence, null, null, 0f, 99, 1, out List<PlacedRoom> a, out _);

            var solverB = new DungeonLayoutSolver();
            solverB.TrySolveLayout(3, sequence, null, null, 0f, 99, 1, out List<PlacedRoom> b, out _);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Rotation, b[i].Rotation);
            }
        }

        [Test]
        public void TrySolveLayout_OverlappingDoors_Fails()
        {
            // Doors at center → rooms overlap on step 1
            RoomTemplate Bad(string id)
            {
                var room = new RoomTemplate(id)
                {
                    Footprint = new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f))
                };
                room.Exits.Add(new DoorSocket(Vector3.zero, Quaternion.identity));
                room.Entrances.Add(new DoorSocket(Vector3.zero, Quaternion.identity));
                return room;
            }

            var sequence = CreateSequence(Bad("Start"), Bad("Mid"), Bad("End"));

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                3, sequence, null, null, 0f, 123, 1,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsFalse(ok);
            Assert.IsEmpty(result);
            Assert.IsTrue(error.Contains("Failed to place room at step 1"));
        }

        [Test]
        public void TrySolveLayout_SideRoomChanceOne_AddsExtraRoom()
        {
            RoomTemplate side = TestRooms.Box("Side", 10f);

            var midCat = new RoomCategoryConfig { Name = "Mid", SideRoomChance = 1f };
            midCat.Templates.Add(TestRooms.Box("Mid", 10f));
            midCat.SideRoomTemplates = new List<RoomTemplate> { side };

            var startCat = new RoomCategoryConfig { Name = "Start" };
            startCat.Templates.Add(TestRooms.Box("Start", 10f));

            var endCat = new RoomCategoryConfig { Name = "End" };
            endCat.Templates.Add(TestRooms.Box("End", 10f));

            var sequence = new List<RoomCategoryConfig> { startCat, midCat, endCat };

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                3, sequence, null, null, 0f, 7, 20,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.Greater(result.Count, 3, "Side room should add at least one extra room.");
        }

        [Test]
        public void TrySolveLayout_ConnectorChanceOne_AddsConnector()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f));

            var connectorCat = new RoomCategoryConfig { Name = "Connector" };
            connectorCat.Templates.Add(TestRooms.Box("Connector", 8f));

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                3, sequence, null, connectorCat, connectorSpawnChance: 1f, baseSeed: 55, maxAttempts: 30,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.Greater(result.Count, 3, "Connector should add at least one extra room.");
        }
    }
}
```

---

## How to read the solver (beginner map)

```
TrySolveLayout
  └── retry loop (seed + attempt)
        └── TryBuild
              ├── PlaceStartRoom          → room 0 at origin
              ├── PlaceMainPath           → roomSize - 1 steps
              │     └── TryPlaceNextOnPath
              │           ├── TryAttachWithConnector (optional)
              │           └── TryAttachDirect
              └── PlaceSideRooms          → roll sideRoomChance on free exits
```

---

## Unity setup

1. Add `DungeonLayoutSolver.cs` next to your existing `DungeonTypes.cs`.
2. Add `DungeonLayoutSolverTests.cs` under Tests.
3. **Test Runner → Edit Mode → Run All** (slice 1 + slice 2 tests).

No scene changes.

---

## Verify

- [ ] Compiles
- [ ] **12 tests pass** (6 bounds + 6 solver) — side/connector tests may need more `maxAttempts` on unlucky seeds; bump to 50 if flaky
- [ ] `TrySolveLayout` with `roomSize: 5`, seed `42`, no side/connectors → exactly 5 rooms
- [ ] Start room always at origin

---

## Next delivery

`docs/code/dungeon-prefab-authoring.md` — `RoomPrefabData`, `DoorEntrance`, `DoorExit`, `RoomCategoryData` SO.
