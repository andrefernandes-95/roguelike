using AF.Dungeon;
using NUnit.Framework;

namespace AF.Tests.Dungeon
{
    public class RoomTemplateTests
    {
        [Test]
        public void Clone_CopiesDoorsAndFloorTiles()
        {
            RoomTemplate original = TestRooms.Box("Start", 10f);
            original.Exits[0].IsConnected = true;

            RoomTemplate copy = original.Clone();
            Assert.AreNotSame(original, copy);
            Assert.AreEqual(original.Id, copy.Id);
            Assert.AreEqual(1, copy.FloorTiles.Count);
            Assert.AreEqual(1, copy.Exits.Count);
            Assert.IsTrue(copy.Exits[0].IsConnected);
        }
    }
}
