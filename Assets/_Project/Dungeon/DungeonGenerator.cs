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
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (player == null)
            {
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
