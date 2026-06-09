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
