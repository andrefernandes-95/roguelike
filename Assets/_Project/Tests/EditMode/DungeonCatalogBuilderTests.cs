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
