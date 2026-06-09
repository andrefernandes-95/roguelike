# Dungeon slice 3 — prefab authoring

## Goal

Turn room **prefabs** into `RoomTemplate` data for the solver. One **RoomBounds** `BoxCollider` + door markers per room — no floor-tile scan.

**Prerequisite:** slice 1 + 2 compile; solver tests pass. `DoorEntrance.cs` and `DoorExit.cs` may already exist — skip those files if unchanged.

---

## Files

```
Assets/_Project/Dungeon/
├── DoorEntrance.cs      (skip if already present)
├── DoorExit.cs          (skip if already present)
├── RoomPrefabData.cs
└── RoomCategoryData.cs

Assets/_Project/Tests/EditMode/
├── RoomPrefabDataTests.cs
└── RoomCategoryDataTests.cs
```

**Asmdef:** `AF.Dungeon` — no new references.

---

### `Assets/_Project/Dungeon/DoorEntrance.cs`

```csharp
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Doorway into this room. Forward (blue) = into room
    /// </summary>
    public sealed class DoorEntrance : MonoBehaviour
    {
        public bool IsConnected;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
```

---

### `Assets/_Project/Dungeon/DoorExit.cs`

```csharp
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Doorway out of this room. Forward (red) = out of room
    /// </summary>
    public sealed class DoorExit : MonoBehaviour
    {
        public bool IsConnected;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
```

---

### `Assets/_Project/Dungeon/RoomPrefabData.cs`

```csharp
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// On room prefab root. Reads RoomBounds + door markers → RoomTemplate.
    /// RoomBounds child: axis-aligned BoxCollider (not trigger).
    /// </summary>
    public sealed class RoomPrefabData : MonoBehaviour
    {
        const string BoundsChildName = "RoomBounds";

        [SerializeField] BoxCollider boundsCollider;

        void OnValidate()
        {
            if (boundsCollider == null)
            {
                Transform child = transform.Find(BoundsChildName);
                if (child != null)
                {
                    boundsCollider = child.GetComponent<BoxCollider>();
                }
            }
        }

        /// <summary>Build solver template from a room hierarchy (instance or test object).</summary>
        public static RoomTemplate BuildTemplate(GameObject roomRoot)
        {
            if (roomRoot == null)
            {
                return null;
            }

            Transform root = roomRoot.transform;
            var data = roomRoot.GetComponent<RoomPrefabData>();

            BoxCollider box = data != null ? data.boundsCollider : null;
            if (box == null)
            {
                Transform boundsChild = root.Find(BoundsChildName);
                if (boundsChild != null)
                {
                    box = boundsChild.GetComponent<BoxCollider>();
                }
            }

            if (box == null)
            {
                Debug.LogWarning($"RoomPrefabData: no RoomBounds on '{roomRoot.name}'.");
                return null;
            }

            var template = new RoomTemplate(roomRoot.name)
            {
                LocalScale = root.localScale,
                Footprint = GetFootprintInRootSpace(root, box)
            };

            DoorEntrance[] entrances = roomRoot.GetComponentsInChildren<DoorEntrance>(true);
            foreach (DoorEntrance entrance in entrances)
            {
                template.Entrances.Add(ToSocket(root, entrance.transform));
            }

            DoorExit[] exits = roomRoot.GetComponentsInChildren<DoorExit>(true);
            foreach (DoorExit exit in exits)
            {
                template.Exits.Add(ToSocket(root, exit.transform));
            }

            return template;
        }

        /// <summary>Instantiate prefab asset, read template, destroy temp instance.</summary>
        public static RoomTemplate BuildTemplateFromPrefab(GameObject prefabAsset)
        {
            if (prefabAsset == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefabAsset);
            instance.SetActive(false);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.name = prefabAsset.name;

            try
            {
                RoomTemplate template = BuildTemplate(instance);
                if (template != null)
                {
                    template.Id = prefabAsset.name;
                }

                return template;
            }
            finally
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(instance);
                }
                else
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        static Bounds GetFootprintInRootSpace(Transform root, BoxCollider box)
        {
            Transform t = box.transform;
            Vector3 localCenter = root.InverseTransformPoint(t.TransformPoint(box.center));
            Vector3 size = Vector3.Scale(box.size, t.lossyScale);
            return new Bounds(localCenter, size);
        }

        static DoorSocket ToSocket(Transform root, Transform door)
        {
            Vector3 localPos = root.InverseTransformPoint(door.position);
            Quaternion localRot = Quaternion.Inverse(root.rotation) * door.rotation;
            return new DoorSocket(localPos, localRot);
        }
    }
}
```

---

### `Assets/_Project/Dungeon/RoomCategoryData.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    [CreateAssetMenu(fileName = "RoomCategory", menuName = "AF/Dungeon/Room Category")]
    public sealed class RoomCategoryData : ScriptableObject
    {
        public string categoryName;
        public List<GameObject> prefabs = new List<GameObject>();
        [Range(0f, 1f)] public float sideRoomChance;
        public RoomCategoryData sideRoomCategory;

        public RoomCategoryConfig ToConfig()
        {
            var config = new RoomCategoryConfig
            {
                Name = categoryName,
                SideRoomChance = sideRoomChance
            };

            foreach (GameObject prefab in prefabs)
            {
                RoomTemplate template = RoomPrefabData.BuildTemplateFromPrefab(prefab);
                if (template != null)
                {
                    config.Templates.Add(template);
                }
            }

            if (sideRoomCategory != null)
            {
                config.SideRoomTemplates = new List<RoomTemplate>();
                foreach (GameObject prefab in sideRoomCategory.prefabs)
                {
                    RoomTemplate template = RoomPrefabData.BuildTemplateFromPrefab(prefab);
                    if (template != null)
                    {
                        config.SideRoomTemplates.Add(template);
                    }
                }
            }

            return config;
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/RoomPrefabDataTests.cs`

```csharp
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class RoomPrefabDataTests
    {
        [TearDown]
        public void TearDown()
        {
            var roots = Object.FindObjectsByType<RoomPrefabData>(FindObjectsSortMode.None);
            foreach (RoomPrefabData data in roots)
            {
                Object.DestroyImmediate(data.gameObject);
            }
        }

        static GameObject CreateTestRoom(string name, float size)
        {
            float half = size / 2f;
            var root = new GameObject(name);
            root.AddComponent<RoomPrefabData>();

            var boundsObject = new GameObject("RoomBounds");
            boundsObject.transform.SetParent(root.transform, false);
            var box = boundsObject.AddComponent<BoxCollider>();
            box.size = new Vector3(size, 2f, size);

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

        [Test]
        public void BuildTemplate_ReadsFootprintAndDoors()
        {
            GameObject room = CreateTestRoom("Room_Start", 10f);

            RoomTemplate template = RoomPrefabData.BuildTemplate(room);

            Assert.NotNull(template);
            Assert.AreEqual("Room_Start", template.Id);
            Assert.AreEqual(new Vector3(10f, 2f, 10f), template.Footprint.size);
            Assert.AreEqual(1, template.Exits.Count);
            Assert.AreEqual(1, template.Entrances.Count);
            Assert.AreEqual(new Vector3(0f, 0f, 5f), template.Exits[0].LocalPosition);
            Assert.AreEqual(new Vector3(0f, 0f, -5f), template.Entrances[0].LocalPosition);
        }

        [Test]
        public void BuildTemplate_MissingBounds_ReturnsNull()
        {
            var root = new GameObject("BrokenRoom");
            root.AddComponent<RoomPrefabData>();

            RoomTemplate template = RoomPrefabData.BuildTemplate(root);

            Assert.IsNull(template);
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/RoomCategoryDataTests.cs`

```csharp
using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class RoomCategoryDataTests
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

        [Test]
        public void ToConfig_BuildsTemplatesFromPrefabs()
        {
            var category = ScriptableObject.CreateInstance<RoomCategoryData>();
            createdObjects.Add(category);
            category.categoryName = "Start";
            category.prefabs.Add(CreateRoomPrefab("Room_Start"));

            RoomCategoryConfig config = category.ToConfig();

            Assert.AreEqual("Start", config.Name);
            Assert.AreEqual(1, config.Templates.Count);
            Assert.AreEqual("Room_Start", config.Templates[0].Id);
        }

        [Test]
        public void ToConfig_IncludesSideRoomOverride()
        {
            var sideCategory = ScriptableObject.CreateInstance<RoomCategoryData>();
            createdObjects.Add(sideCategory);
            sideCategory.categoryName = "SidePool";
            sideCategory.prefabs.Add(CreateRoomPrefab("Room_Side"));

            var category = ScriptableObject.CreateInstance<RoomCategoryData>();
            createdObjects.Add(category);
            category.categoryName = "End";
            category.sideRoomChance = 1f;
            category.sideRoomCategory = sideCategory;
            category.prefabs.Add(CreateRoomPrefab("Room_End"));

            RoomCategoryConfig config = category.ToConfig();

            Assert.AreEqual(1f, config.SideRoomChance);
            Assert.NotNull(config.SideRoomTemplates);
            Assert.AreEqual(1, config.SideRoomTemplates.Count);
            Assert.AreEqual("Room_Side", config.SideRoomTemplates[0].Id);
        }
    }
}
```

---

## Prefab recipe

```
Room_Start
├── RoomPrefabData
├── RoomBounds     (BoxCollider 10×2×10, not trigger)
├── DoorExit       (+Z, forward = out of room)
├── DoorEntrance   (-Z, forward = into room)
├── PlayerSpawn    (Start room only — empty Transform at center)
└── Floor          (visual)
```

Prefab **name** becomes template `Id` (e.g. `Room_Start`). Repeat for Mid, End, Connector, Side variants.

## Category assets

**Create → AF/Dungeon/Room Category:**

| Asset | Contents |
|-------|----------|
| `Cat_Start` | Start prefab(s) |
| `Cat_Mid` | Mid prefab(s) |
| `Cat_End` | End prefab(s); optional `sideRoomChance` + `sideRoomCategory` → `Cat_Side` |
| `Cat_Connector` | Connector prefab(s) |
| `Cat_Side` | Side room prefab(s) |

---

## Unity setup

1. Type the production + test files above.
2. Build 3–5 room prefabs following the recipe.
3. Create category ScriptableObjects and assign prefabs.

---

## Checklist

- [ ] Compiles with zero errors
- [ ] Test Runner → Edit Mode → all dungeon tests pass (including new prefab/category tests)
- [ ] Room prefabs show blue/red door gizmos in Scene view
- [ ] `RoomCategoryData.ToConfig()` returns templates when you inspect in debugger (optional)

**Next slice:** `dungeon-generator.md` (slice 4).
