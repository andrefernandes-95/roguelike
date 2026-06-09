using AF.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests
{
    public class BoundsHelperTests
    {
        Vector3 size = new Vector3(10f, 2f, 10f);

        [Test]
        public void ToWorldBounds_AtOrigin_MatchesLocalFootprint()
        {
            Bounds local = new Bounds(Vector3.zero, size);
            Bounds world = BoundsHelper.ToWorldBounds(Vector3.zero, Quaternion.identity, Vector3.one, local);

            Assert.AreEqual(10f, world.size.x, 0.01f);
            Assert.AreEqual(10f, world.size.z, 0.01f);
        }

        [Test]
        public void OverlapsAny_TouchingEdges_DoesNotOverlap()
        {

            Bounds a = new Bounds(new Vector3(0f, 0f, 0f), size);
            Bounds b = new Bounds(new Vector3(10f, 0f, 0f), size);

            Assert.IsFalse(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void OverlapsAny_Overlapping_ReturnsTrue()
        {
            Bounds a = new Bounds(Vector3.zero, size);
            Bounds b = new Bounds(new Vector3(5f, 0f, 0f), size);

            Assert.IsTrue(BoundsHelper.OverlapsAny(a, new[] { b }));
        }

        [Test]
        public void CanPlace_RejectsOverlap()
        {
            int size = 10;
            RoomTemplate room = TestRooms.Box("A", size);
            var occupied = new System.Collections.Generic.List<Bounds>
            {
                BoundsHelper.ToWorldBounds(room, Vector3.zero, Quaternion.identity)
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(5f, 0f, 0f),
                Quaternion.identity,
                occupied,
                out Bounds footprint
            );

            Assert.IsFalse(ok);
        }

        [Test]
        public void CanPlace_AllowsAdjacent()
        {
            int size = 10;
            RoomTemplate room = TestRooms.Box("A", size);
            var occupied = new System.Collections.Generic.List<Bounds>
            {
                BoundsHelper.ToWorldBounds(room, Vector3.zero, Quaternion.identity)
            };

            bool ok = BoundsHelper.CanPlace(
                room,
                new Vector3(10f, 0f, 0f),
                Quaternion.identity,
                occupied,
                out Bounds footprint
            );

            Assert.IsTrue(ok);
        }

    }
}
