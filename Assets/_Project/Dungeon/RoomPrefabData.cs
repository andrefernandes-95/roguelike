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
