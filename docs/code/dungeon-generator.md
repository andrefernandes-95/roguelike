# Dungeon slice 4 — generator

## Goal

Thin runtime adapter: read run seed → solve layout → spawn room prefabs → close dead ends → move player to start spawn.

**Prerequisite:** slice 1–3 compile; prefab + category assets exist.

**Out of scope (jam v1):** visibility culling, enemy/loot spawn, NavMesh baking.

---

## Files

```
Assets/_Project/Dungeon/
├── DungeonCatalogBuilder.cs
└── DungeonGenerator.cs

Assets/_Project/Tests/EditMode/
└── DungeonCatalogBuilderTests.cs
```

**Asmdef:** `AF.Dungeon` already references `AF.Core` (for `RunCoordinator`). No change needed.

---

### `Assets/_Project/Dungeon/DungeonCatalogBuilder.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Converts authoring assets into solver input and prefab lookup for spawning.
    /// </summary>
    public static class DungeonCatalogBuilder
    {
        public static List<RoomCategoryConfig> ToLayoutSequence(IEnumerable<RoomCategoryData> sequence)
        {
            var configs = new List<RoomCategoryConfig>();
            if (sequence == null)
            {
                return configs;
            }

            foreach (RoomCategoryData category in sequence)
            {
                if (category != null)
                {
                    configs.Add(category.ToConfig());
                }
            }

            return configs;
        }

        public static Dictionary<string, GameObject> BuildPrefabLookup(
            IEnumerable<RoomCategoryData> layoutSequence,
            RoomCategoryData sideCategory,
            RoomCategoryData connectorCategory)
        {
            var lookup = new Dictionary<string, GameObject>();

            void Register(RoomCategoryData category)
            {
                if (category == null)
                {
                    return;
                }

                foreach (GameObject prefab in category.prefabs)
                {
                    if (prefab != null)
                    {
                        lookup[prefab.name] = prefab;
                    }
                }

                Register(category.sideRoomCategory);
            }

            if (layoutSequence != null)
            {
                foreach (RoomCategoryData category in layoutSequence)
                {
                    Register(category);
                }
            }

            Register(sideCategory);
            Register(connectorCategory);

            return lookup;
        }
    }
}
```

---

### `Assets/_Project/Dungeon/DungeonGenerator.cs`

```csharp
using System.Collections.Generic;
using AF.Core;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Runtime adapter: seed → solver → instantiate room prefabs.
    /// </summary>
    public sealed class DungeonGenerator : MonoBehaviour
    {
        const string PlayerSpawnChildName = "PlayerSpawn";

        [SerializeField] List<RoomCategoryData> layoutSequence = new();
        [SerializeField] int roomSize = 5;
        [SerializeField] RoomCategoryData defaultSideRoomCategory;
        [SerializeField] RoomCategoryData connectorCategory;
        [SerializeField] [Range(0f, 1f)] float connectorSpawnChance = 0.3f;
        [SerializeField] GameObject deadEndPrefab;
        [SerializeField] Transform player;
        [SerializeField] int maxSolveAttempts = 999;

        readonly List<GameObject> spawnedRooms = new();

        public int CurrentActiveSeed { get; private set; }

        void Start()
        {
            if (RunCoordinator.Instance == null || RunCoordinator.Instance.State != RunState.FloorActive)
            {
                return;
            }

            Generate();
        }

        public void Generate()
        {
            if (layoutSequence == null || layoutSequence.Count == 0)
            {
                Debug.LogError("[DungeonGenerator] Layout sequence is empty.");
                return;
            }

            if (roomSize < 2)
            {
                Debug.LogError("[DungeonGenerator] roomSize must be at least 2.");
                return;
            }

            RunCoordinator coordinator = RunCoordinator.Instance;
            if (coordinator == null)
            {
                Debug.LogError("[DungeonGenerator] RunCoordinator not found.");
                return;
            }

            int baseSeed = coordinator.Session.Seed;
            List<RoomCategoryConfig> configs = DungeonCatalogBuilder.ToLayoutSequence(layoutSequence);
            RoomCategoryConfig sideConfig = defaultSideRoomCategory != null
                ? defaultSideRoomCategory.ToConfig()
                : null;
            RoomCategoryConfig connectorConfig = connectorCategory != null
                ? connectorCategory.ToConfig()
                : null;

            var solver = new DungeonLayoutSolver();
            bool solved = solver.TrySolveLayout(
                roomSize,
                configs,
                sideConfig,
                connectorConfig,
                connectorSpawnChance,
                baseSeed,
                maxSolveAttempts,
                out List<PlacedRoom> resultRooms,
                out string errorMessage);

            if (!solved)
            {
                Debug.LogError($"[DungeonGenerator] Layout failed: {errorMessage}");
                ClearDungeon();
                return;
            }

            CurrentActiveSeed = solver.CurrentActiveSeed;
            Dictionary<string, GameObject> prefabLookup = DungeonCatalogBuilder.BuildPrefabLookup(
                layoutSequence,
                defaultSideRoomCategory,
                connectorCategory);

            ClearDungeon();

            for (int i = 0; i < resultRooms.Count; i++)
            {
                PlacedRoom placed = resultRooms[i];
                if (!prefabLookup.TryGetValue(placed.Template.Id, out GameObject prefab))
                {
                    Debug.LogError($"[DungeonGenerator] No prefab for template '{placed.Template.Id}'.");
                    ClearDungeon();
                    return;
                }

                GameObject roomObject = Instantiate(prefab, placed.Position, placed.Rotation, transform);
                roomObject.name = i == 0 ? $"{placed.Template.Id}_Start" : $"{placed.Template.Id}_{i}";
                roomObject.SetActive(true);

                SyncDoorConnections(roomObject, placed.Template);
                spawnedRooms.Add(roomObject);
            }

            CloseUnusedConnections();
            PositionPlayerAtStart(spawnedRooms.Count > 0 ? spawnedRooms[0] : null);

            Debug.Log($"[DungeonGenerator] Generated {spawnedRooms.Count} rooms (seed {CurrentActiveSeed}).");
        }

        public void ClearDungeon()
        {
            spawnedRooms.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        static void SyncDoorConnections(GameObject roomObject, RoomTemplate template)
        {
            DoorEntrance[] entrances = roomObject.GetComponentsInChildren<DoorEntrance>(true);
            for (int i = 0; i < entrances.Length && i < template.Entrances.Count; i++)
            {
                entrances[i].IsConnected = template.Entrances[i].IsConnected;
            }

            DoorExit[] exits = roomObject.GetComponentsInChildren<DoorExit>(true);
            for (int i = 0; i < exits.Length && i < template.Exits.Count; i++)
            {
                exits[i].IsConnected = template.Exits[i].IsConnected;
            }
        }

        void CloseUnusedConnections()
        {
            if (deadEndPrefab == null)
            {
                return;
            }

            foreach (GameObject roomObject in spawnedRooms)
            {
                if (roomObject == null)
                {
                    continue;
                }

                DoorExit[] exits = roomObject.GetComponentsInChildren<DoorExit>(true);
                foreach (DoorExit exit in exits)
                {
                    if (exit != null && !exit.IsConnected)
                    {
                        GameObject blocker = Instantiate(
                            deadEndPrefab,
                            exit.transform.position,
                            exit.transform.rotation,
                            transform);
                        blocker.name = $"DeadEnd_{roomObject.name}_Exit";
                        exit.IsConnected = true;
                    }
                }

                DoorEntrance[] entrances = roomObject.GetComponentsInChildren<DoorEntrance>(true);
                foreach (DoorEntrance entrance in entrances)
                {
                    if (entrance != null && !entrance.IsConnected)
                    {
                        Quaternion rotation = entrance.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                        GameObject blocker = Instantiate(
                            deadEndPrefab,
                            entrance.transform.position,
                            rotation,
                            transform);
                        blocker.name = $"DeadEnd_{roomObject.name}_Entrance";
                        entrance.IsConnected = true;
                    }
                }
            }
        }

        void PositionPlayerAtStart(GameObject startRoom)
        {
            if (startRoom == null)
            {
                return;
            }

            if (player == null)
            {
                Debug.LogWarning("[DungeonGenerator] Player transform not assigned.");
                return;
            }

            Transform spawn = startRoom.transform.Find(PlayerSpawnChildName);
            if (spawn == null)
            {
                Debug.LogWarning("[DungeonGenerator] Start room has no PlayerSpawn child.");
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.SetPositionAndRotation(spawn.position, spawn.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/DungeonCatalogBuilderTests.cs`

```csharp
using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class DungeonCatalogBuilderTests
    {
        readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        GameObject CreateRoomPrefab(string name)
        {
            float half = 5f;
            var root = new GameObject(name);
            createdObjects.Add(root);
            root.AddComponent<RoomPrefabData>();

            var boundsObject = new GameObject("RoomBounds");
            boundsObject.transform.SetParent(root.transform, false);
            var box = boundsObject.AddComponent<BoxCollider>();
            box.size = new Vector3(10f, 2f, 10f);

            var exitObject = new GameObject("DoorExit");
            exitObject.transform.SetParent(root.transform, false);
            exitObject.transform.localPosition = new Vector3(0f, 0f, half);
            exitObject.AddComponent<DoorExit>();

            var entranceObject = new GameObject("DoorEntrance");
            entranceObject.transform.SetParent(root.transform, false);
            entranceObject.transform.localPosition = new Vector3(0f, 0f, -half);
            entranceObject.AddComponent<DoorEntrance>();

            return root;
        }

        RoomCategoryData CreateCategory(string name, string prefabName)
        {
            var category = ScriptableObject.CreateInstance<RoomCategoryData>();
            createdObjects.Add(category);
            category.categoryName = name;
            category.prefabs.Add(CreateRoomPrefab(prefabName));
            return category;
        }

        [Test]
        public void BuildPrefabLookup_MapsTemplateIdToPrefab()
        {
            RoomCategoryData start = CreateCategory("Start", "Room_Start");
            RoomCategoryData mid = CreateCategory("Mid", "Room_Mid");
            RoomCategoryData end = CreateCategory("End", "Room_End");

            var sequence = new List<RoomCategoryData> { start, mid, end };
            Dictionary<string, GameObject> lookup = DungeonCatalogBuilder.BuildPrefabLookup(sequence, null, null);

            Assert.AreEqual(3, lookup.Count);
            Assert.AreEqual(start.prefabs[0], lookup["Room_Start"]);
            Assert.AreEqual(mid.prefabs[0], lookup["Room_Mid"]);
            Assert.AreEqual(end.prefabs[0], lookup["Room_End"]);
        }

        [Test]
        public void ToLayoutSequence_BuildsConfigsInOrder()
        {
            RoomCategoryData start = CreateCategory("Start", "Room_Start");
            RoomCategoryData mid = CreateCategory("Mid", "Room_Mid");
            RoomCategoryData end = CreateCategory("End", "Room_End");

            List<RoomCategoryConfig> configs = DungeonCatalogBuilder.ToLayoutSequence(
                new List<RoomCategoryData> { start, mid, end });

            Assert.AreEqual(3, configs.Count);
            Assert.AreEqual("Start", configs[0].Name);
            Assert.AreEqual("Mid", configs[1].Name);
            Assert.AreEqual("End", configs[2].Name);
        }
    }
}
```

---

## Unity setup

### Graybox scene

1. Empty GameObject **`Dungeon`** → add **`DungeonGenerator`**
2. Wire Inspector:
   - **Layout Sequence:** `Cat_Start`, `Cat_Mid`, `Cat_End` (3 entries)
   - **Room Size:** `5`
   - **Default Side Room Category:** `Cat_Side`
   - **Connector Category:** `Cat_Connector`
   - **Connector Spawn Chance:** e.g. `0.3`
   - **Dead End Prefab:** simple wall/cube with collider
   - **Player:** drag Player transform from scene
3. Disable or remove static **Ground** once rooms generate (optional)
4. Remove duplicate **RunCoordinator** from Graybox if present — TitleScreen DDOL instance is the one that matters

### RunCoordinator (TitleScreen scene)

- `dungeonScene` = `Graybox` (should already be set)

### Dead-end prefab

Simple cube/wall prefab with collider. Spawned at unused door transforms.

---

## Play flow

**TitleScreen → New Run** → loads Graybox → `DungeonGenerator.Start()` runs when `RunCoordinator.State == FloorActive` → procedural layout from `RunSession.Seed` → player teleports to `PlayerSpawn` on start room.

---

## Checklist

- [ ] Compiles with zero errors
- [ ] Test Runner → Edit Mode → all dungeon tests pass
- [ ] Play: New Run spawns 5+ rooms (connectors/side rooms may add extras)
- [ ] Player appears on start room `PlayerSpawn`
- [ ] Unused doors get dead-end blockers
- [ ] Console shows `Generated N rooms (seed …)`
