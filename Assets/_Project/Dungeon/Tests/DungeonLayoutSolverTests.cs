using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.Dungeon
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

        [Test]
        public void TrySolveLayout_CriticalPath_CountEqualsRoomSize()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f)
            );

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                roomSize: 5,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 42,
                maxAttempts: 10,
                out List<PlacedRoom> result,
                out string error
            );

            Assert.IsTrue(ok, error);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual("Start", result[0].Template.Id);
            Assert.AreEqual("End", result[4].Template.Id);
        }

        [Test]
        public void TrySolveLayout_SameSeed_SameLayout()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f)
            );

            var solverA = new DungeonLayoutSolver();
            solverA.TrySolveLayout(
                roomSize: 3,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 99,
                maxAttempts: 1,
                out List<PlacedRoom> a,
                out _
            );

            var solverB = new DungeonLayoutSolver();
            solverB.TrySolveLayout(
                roomSize: 3,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 99,
                maxAttempts: 1,
                out List<PlacedRoom> b,
                out _
            );

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Rotation, b[i].Rotation);
            }
        }

        [Test]
        public void TrySolveLayout_OverlappingDoors_Fails()
        {
            RoomTemplate Bad(string id)
            {
                var room = new RoomTemplate(id);
                room.FloorTiles.Add(new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f)));
                room.Exits.Add(new DoorSocket(Vector3.zero, Quaternion.identity));
                room.Entrances.Add(new DoorSocket(Vector3.zero, Quaternion.identity));
                return room;
            }

            var sequence = CreateSequence(Bad("Start"), Bad("Mid"), Bad("End"));
            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(3, sequence, null, null, 0f, 123, 1,
                out List<PlacedRoom> result, out string error);

            Assert.IsFalse(ok);
            Assert.IsEmpty(result);
            Assert.IsTrue(error.Contains("Failed to place room at step 1"));
        }

        [Test]
        public void TrySolveLayout_SideRoomChanceOne_AddsExtraRoom()
        {
            RoomTemplate side = TestRooms.Box("Side", 10f);

            var startCat = new RoomCategoryConfig { Name = "Start" };
            startCat.Templates.Add(TestRooms.Box("Start", 10f));

            var midCat = new RoomCategoryConfig { Name = "Mid" };
            midCat.Templates.Add(TestRooms.Box("Mid", 10f));

            var endCat = new RoomCategoryConfig { Name = "End", SideRoomChance = 1f };
            endCat.Templates.Add(TestRooms.Box("End", 10f));
            endCat.SideRoomTemplates = new List<RoomTemplate> { side };

            var sequence = new List<RoomCategoryConfig> { startCat, midCat, endCat };

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                3, sequence, null, null, 0f, 7, 20,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.Greater(result.Count, 3, "Side room should add at least one extra room.");
        }

        [Test]
        public void TrySolveLayout_ConnectorChanceOne_AddsConnector()
        {
            var sequence = CreateSequence(
                TestRooms.Box("Start", 10f),
                TestRooms.Box("Mid", 10f),
                TestRooms.Box("End", 10f)
            );

            var connectorCat = new RoomCategoryConfig { Name = "Connector" };
            connectorCat.Templates.Add(TestRooms.Box("Connector", 8f));

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                3, sequence, null, connectorCat, connectorSpawnChance: 1,
                baseSeed: 55, maxAttempts: 30, out List<PlacedRoom> result, out string error);

            Assert.IsTrue(ok, error);
            Assert.Greater(result.Count, 3, "Connector should add at least one extra room");
        }
    }
}
