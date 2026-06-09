using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;
using static AF.Dungeon.PlacedRoom;

namespace AF.Tests
{
    public class DungeonLayoutSolverTests
    {
        static List<RoomCategoryConfig> CreateSequence(RoomTemplate start, RoomTemplate mid, RoomTemplate end)
        {
            var startCat = new RoomCategoryConfig { Name = "Start" };
            startCat.Templates.Add(start);

            var midCat = new RoomCategoryConfig { Name = "Mid" };
            midCat.Templates.Add(mid);

            var endCat = new RoomCategoryConfig { Name = "End" };
            endCat.Templates.Add(end);

            return new List<RoomCategoryConfig> { startCat, midCat, endCat };
        }

        [Test]
        public void TrySolveLayout_StartRoom_AtOrigin()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f)
            );

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                roomSize: 3,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 10,
                maxAttempts: 1,
                out List<PlacedRoom> result,
                out string error
            );

            Assert.IsTrue(ok, error);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(Vector3.zero, result[0].Position);
            Assert.AreEqual(Quaternion.identity, result[0].Rotation);
        }
    }
}