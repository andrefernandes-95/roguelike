using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class BoundsHelperTests
    {
        const float TileSize = 10f;

        [Test]
        public void ToWorldBounds_AtOrigin_MatchesLocalTile()
        {
            Bounds local = new Bounds(Vector3.zero, new Vector3(TileSize, 2f, TileSize));
            Bounds world = BoundsHelper.ToWorldBounds(Vector3.zero, Quaternion.identity, Vector3.one, local);

            Assert.AreEqual(TileSize, world.size.x, 0.01f);
            Assert.AreEqual(TileSize, world.size.z, 0.01f);
        }

        [Test]
        public void OverlapsAny_TouchingTiles_DoesNotOverlap()
        {
            Bounds a = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(TileSize, 2f, TileSize));
            Bounds b = new Bounds(new Vector3(TileSize, 0f, 0f), new Vector3(TileSize, 2f, TileSize));

            Assert.IsFalse(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void OverlapsAny_OverlappingTiles_ReturnsTrue()
        {
            Bounds a = new Bounds(Vector3.zero, new Vector3(TileSize, 2f, TileSize));
            Bounds b = new Bounds(new Vector3(5f, 0f, 0f), new Vector3(TileSize, 2f, TileSize));

            Assert.IsTrue(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void CanPlace_RejectsOverlap()
        {
            RoomTemplate room = TestRooms.Box("A", TileSize);
            var occupied = new List<Bounds>
            {
                BoundsHelper.ToWorldBounds(Vector3.zero, Quaternion.identity, Vector3.one, room.FloorTiles[0])
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(5f, 0f, 0f),
                Quaternion.identity,
                occupied,
                out List<Bounds> tiles);

            Assert.IsFalse(ok);
            Assert.AreEqual(1, tiles.Count);
        }

        [Test]
        public void CanPlace_AllowsAdjacent()
        {
            RoomTemplate room = TestRooms.Box("A", TileSize);
            var occupied = new List<Bounds>
            {
                BoundsHelper.ToWorldBounds(Vector3.zero, Quaternion.identity, Vector3.one, room.FloorTiles[0])
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(TileSize, 0f, 0f),
                Quaternion.identity,
                occupied,
                out List<Bounds> tiles);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, tiles.Count);
        }

        [Test]
        public void CanPlace_MultiTileRoom_ChecksEveryTile()
        {
            RoomTemplate room = TestRooms.TwoTiles("Split", TileSize);
            var occupied = new List<Bounds>
            {
                new Bounds(Vector3.zero, new Vector3(TileSize, 2f, TileSize))
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(TileSize, 0f, 0f),
                Quaternion.identity,
                occupied,
                out _);

            Assert.IsFalse(ok);
        }
    }
}
