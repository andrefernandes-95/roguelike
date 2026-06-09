using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Doorway on a room template.
    /// Solver marks IsConnected when used.
    /// </summary>
    public sealed class DoorSocket
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public bool IsConnected;

        public DoorSocket(Vector3 localPosition, Quaternion localRotation)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            IsConnected = false;
        }

        public DoorSocket Clone()
        {
            return new DoorSocket(LocalPosition, LocalRotation)
            {
                IsConnected = IsConnected
            };
        }
    }

    /// <summary>
    /// Room blueprint for the solver.
    /// No GameObject - adapter maps Id to prefab when spawning.
    /// </summary>
    public sealed class RoomTemplate
    {
        public string Id;
        public Vector3 LocalScale = Vector3.one;

        /// <summary>
        /// Local-space AABB of the room footprint (from RoomBounds collider)
        /// </summary>
        public Bounds Footprint;

        public List<DoorSocket> Entrances = new();
        public List<DoorSocket> Exits = new();

        public RoomTemplate(string id)
        {
            Id = id;
        }

        public RoomTemplate Clone()
        {
            RoomTemplate copy = new(Id)
            {
                LocalScale = LocalScale,
                Footprint = Footprint
            };

            foreach (DoorSocket e in Entrances)
            {
                copy.Entrances.Add(e.Clone());
            }

            foreach (DoorSocket x in Exits)
            {
                copy.Exits.Add(x.Clone());
            }

            return copy;
        }
    }

    /// <summary>
    /// Room after the solver picked the position and rotation
    /// </summary>
    public sealed class PlacedRoom
    {
        public RoomTemplate Template;
        public Vector3 Position;
        public Quaternion Rotation;

        public PlacedRoom(RoomTemplate template, Vector3 position, Quaternion rotation)
        {
            Template = template;
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Category pool passed into the solver
    /// </summary>
    public sealed class RoomCategoryConfig
    {
        public string Name;
        public List<RoomTemplate> Templates = new();
        public float SideRoomChance;
        public List<RoomTemplate> SideRoomTemplates;
    }
}
