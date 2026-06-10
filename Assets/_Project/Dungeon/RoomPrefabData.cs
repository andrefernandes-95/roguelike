using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// On room prefab root. Bakes RoomFloorTile bounds + door markers into RoomTemplate.
    /// </summary>
    public sealed class RoomPrefabData : MonoBehaviour
    {
        public static RoomTemplate BuildTemplate(GameObject roomRoot)
        {
            if (roomRoot == null)
            {
                return null;
            }

            Transform root = roomRoot.transform;
            var template = new RoomTemplate(roomRoot.name)
            {
                LocalScale = root.localScale
            };

            if (!TryCollectFloorTiles(root, template.FloorTiles, out string floorError))
            {
                Debug.LogWarning($"RoomPrefabData: '{roomRoot.name}' — {floorError}");
                return null;
            }

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

            Vector3 prefabScale = prefabAsset.transform.localScale;

            GameObject instance = Object.Instantiate(prefabAsset);
            instance.SetActive(false);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            instance.name = prefabAsset.name;

            try
            {
                RoomTemplate template = BuildTemplate(instance);
                if (template != null)
                {
                    template.Id = prefabAsset.name;
                    template.LocalScale = prefabScale;
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

        static bool TryCollectFloorTiles(Transform root, List<Bounds> tiles, out string error)
        {
            error = "";
            RoomFloorTile[] markers = root.GetComponentsInChildren<RoomFloorTile>(true);
            if (markers.Length == 0)
            {
                error = "needs at least one RoomFloorTile component on a floor child.";
                return false;
            }

            foreach (RoomFloorTile marker in markers)
            {
                Transform tile = marker.transform;
                MeshFilter meshFilter = tile.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    error = $"RoomFloorTile on '{tile.name}' requires a MeshFilter with a mesh.";
                    return false;
                }

                Vector3 relativePos = root.InverseTransformPoint(tile.position);
                Quaternion relativeRot = Quaternion.Inverse(root.rotation) * tile.rotation;

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 scaledSize = Vector3.Scale(meshBounds.size, tile.localScale);
                meshBounds.center = Vector3.Scale(meshBounds.center, tile.localScale);
                meshBounds.size = new Vector3(scaledSize.x, 0.5f, scaledSize.z);
                tiles.Add(LocalBoundsInRootSpace(relativePos, relativeRot, meshBounds));
            }

            return true;
        }

        static Bounds LocalBoundsInRootSpace(Vector3 relativePos, Quaternion relativeRot, Bounds localBounds)
        {
            Vector3 center = relativePos + relativeRot * localBounds.center;
            Vector3 extents = localBounds.extents;

            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new(extents.x * x, extents.y * y, extents.z * z);
                        Vector3 rootPos = center + relativeRot * corner;
                        min = Vector3.Min(min, rootPos);
                        max = Vector3.Max(max, rootPos);
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        static DoorSocket ToSocket(Transform root, Transform door)
        {
            Vector3 localPos = root.InverseTransformPoint(door.position);
            Quaternion localRot = Quaternion.Inverse(root.rotation) * door.rotation;
            return new DoorSocket(localPos, localRot);
        }
    }
}
