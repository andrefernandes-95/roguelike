using AF.Dungeon;
using UnityEngine;

namespace AF.Tests.Dungeon
{
    /// <summary>
    /// Shared test room builders for dungeon Edit Mode tests.
    /// </summary>
    public static class TestRooms
    {
        public static RoomTemplate Box(string id, float size)
        {
            float half = size / 2f;
            var room = new RoomTemplate(id)
            {
                LocalScale = Vector3.one
            };
            room.FloorTiles.Add(new Bounds(Vector3.zero, new Vector3(size, 2f, size)));

            room.Exits.Add(
                new DoorSocket(new Vector3(0f, 0f, half), Quaternion.identity)
            );
            room.Entrances.Add(
                new DoorSocket(new Vector3(0f, 0f, -half), Quaternion.identity)
            );

            return room;
        }

        public static RoomTemplate TwoTiles(string id, float size)
        {
            float half = size / 2f;
            var room = new RoomTemplate(id);
            room.FloorTiles.Add(new Bounds(new Vector3(-half, 0f, 0f), new Vector3(size, 2f, size)));
            room.FloorTiles.Add(new Bounds(new Vector3(half, 0f, 0f), new Vector3(size, 2f, size)));
            room.Exits.Add(new DoorSocket(new Vector3(0f, 0f, half), Quaternion.identity));
            room.Entrances.Add(new DoorSocket(new Vector3(0f, 0f, -half), Quaternion.identity));
            return room;
        }
    }
}
