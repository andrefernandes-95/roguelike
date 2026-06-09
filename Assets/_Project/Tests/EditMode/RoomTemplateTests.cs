using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    /// <summary>
    /// Shared test room builders. Solver tests will reuse this.
    /// </summary>
    public static class TestRooms
    {
        public static RoomTemplate Box(string id, float size)
        {
            float half = size / 2f;
            var room = new RoomTemplate(id)
            {
                Footprint = new Bounds(Vector3.zero, new Vector3(size, 2f, size))
            };

            room.Exits.Add(
                new DoorSocket(new Vector3(0f, 0f, half), Quaternion.identity)
            );
            room.Entrances.Add(
                new DoorSocket(new Vector3(0f, 0f, -half), Quaternion.Euler(0f, 180f, 0f))
            );

            return room;
        }
    }

    public class RoomTemplateTests
    {
        [Test]
        public void Clone_CopiesDoorsAndResetsNotRequired()
        {
            RoomTemplate original = TestRooms.Box("Start", 10f);
            original.Exits[0].IsConnected = true;

            RoomTemplate copy = original.Clone();
            Assert.AreNotSame(original, copy);
            Assert.AreEqual(original.Id, copy.Id);
            Assert.AreEqual(1, copy.Exits.Count);
            Assert.IsTrue(copy.Exits[0].IsConnected);
        }
    }
}
