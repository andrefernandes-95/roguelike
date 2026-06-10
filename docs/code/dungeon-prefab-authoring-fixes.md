# Dungeon slice 3 — RoomPrefabData fixes

## Goal

`RoomPrefabData` footprint math was breaking `BoundsHelper.CanPlace` for prefab-built rooms (footprint misaligned vs doors → constant overlap rejection).

**Fixed in repo** — if your local copy still has the old code, apply the changes below.

Symptoms: `DungeonLayoutSolver` fails at step 1+, or `CanPlace` always false with real prefabs while `TestRooms.Box` tests pass.

**Prerequisite:** `RoomPrefabData.cs` and `RoomCategoryData.cs` already exist.

---

## Fix 1 — `OnValidate` + bounds lookup in `BuildTemplate`

Replace your `RoomPrefabData` class body with this version (or merge the marked sections):

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

### What changed

| Issue | Fix |
|-------|-----|
| `boundsCollider` null → NRE | Find `RoomBounds` child; return `null` + warning if missing |
| Footprint wrong when bounds child is offset | Use `t.TransformPoint(box.center)` (world center → root-local) |
| Inspector wiring tedious | `OnValidate` auto-finds `RoomBounds` collider |

---

## Next after this fix

1. Type slice 3 tests from [dungeon-prefab-authoring.md](dungeon-prefab-authoring.md) (`RoomPrefabDataTests`, `RoomCategoryDataTests`)
2. Build room prefabs — [dungeon-room-prefabs-unity.md](dungeon-room-prefabs-unity.md)
3. Type slice 4 from [dungeon-generator.md](dungeon-generator.md)

---

## Checklist

- [ ] `RoomPrefabData.cs` updated
- [ ] Compiles
- [ ] Test Runner → Edit Mode → slice 3 tests pass (after typing them)
