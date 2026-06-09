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
