using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Pure C# Dungeon Layout
    /// No prefabs - only RoomTemplate data
    /// </summary>
    public sealed class DungeonLayoutSolver
    {
        readonly List<Bounds> occupied = new();
        readonly List<PlacedRoom> placed = new();

        public int CurrentActiveSeed { get; private set; }

        public bool TrySolveLayout(
            int roomSize,
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int baseSeed,
            int maxAttempts,
            out List<PlacedRoom> resultRooms,
            out string errorMessage
        )
        {
            resultRooms = new List<PlacedRoom>();
            errorMessage = "";

            if (layoutSequence == null || layoutSequence.Count == 0)
            {
                errorMessage = "Layout sequence is empty";
                return false;
            }

            if (roomSize < 2)
            {
                errorMessage = "roomSize must be at least 2";
                return false;
            }

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                CurrentActiveSeed = baseSeed + (attempt - 1);
                Random.InitState(CurrentActiveSeed);

                occupied.Clear();
                placed.Clear();

                if (TryBuild(layoutSequence, defaultSideCategory, connectorCategory, connectorSpawnChance, roomSize, out errorMessage))
                {
                    resultRooms = new List<PlacedRoom>(placed);
                    return true;
                }
            }

            return false;
        }

        bool TryBuild(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int roomSize,
            out string errorMessage
        )
        {
            errorMessage = "";

            if (!PlaceStartRoom(layoutSequence, roomSize, out errorMessage))
            {
                return false;
            }

            if (!PlaceMainPath(layoutSequence, connectorCategory, connectorSpawnChance, roomSize, out errorMessage))
            {
                return false;
            }

            PlaceSideRooms(layoutSequence, defaultSideCategory, connectorCategory, connectorSpawnChance);
            return true;
        }

        bool PlaceStartRoom(List<RoomCategoryConfig> layoutSequence, int roomSize, out string errorMessage)
        {
            errorMessage = "";
            RoomCategoryConfig startCategory = GetCategoryForIndex(0, roomSize, layoutSequence);
            if (startCategory == null || startCategory.Templates.Count == 0)
            {
                errorMessage = "Start category is missing or has no templates";
                return false;
            }

            RoomTemplate startPrefab = startCategory.Templates[Random.Range(0, startCategory.Templates.Count)];
            RoomTemplate startRoom = startPrefab.Clone();

            if (!BoundsHelper.CanPlace(startRoom, Vector3.zero, Quaternion.identity, occupied, out Bounds footprint))
            {
                errorMessage = "Start room is invalid";
                return false;
            }

            occupied.Add(footprint);
            placed.Add(new PlacedRoom(startRoom, Vector3.zero, Quaternion.identity));
            return true;
        }

        bool PlaceMainPath(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            int roomSize,
            out string errorMessage
        )
        {
            errorMessage = "";
            PlacedRoom currentRoom = placed[0];
            bool pathCompleted = false;

            for (int step = 1; step < roomSize; step++)
            {
                bool isBossStep = step == roomSize - 1;
                RoomCategoryConfig category = isBossStep
                    ? layoutSequence[layoutSequence.Count - 1]
                    : GetCategoryForIndex(step, roomSize, layoutSequence);

                if (category == null || category.Templates.Count == 0)
                {
                    errorMessage = $"Category for step {step} is missing or empty";
                    return false;
                }

                if (!TryPlaceNextOnPath(currentRoom, category, connectorCategory, connectorSpawnChance, out PlacedRoom newRoom))
                {
                    errorMessage = $"Failed to place room at step {step} (roomSize={roomSize}, placed={placed.Count})";
                    return false;
                }

                currentRoom = newRoom;
                if (isBossStep)
                {
                    pathCompleted = true;
                }
            }

            if (!pathCompleted)
            {
                errorMessage = "Critical path was not completed";
                return false;
            }

            return true;
        }

        bool TryPlaceNextOnPath(
            PlacedRoom currentRoom,
            RoomCategoryConfig category,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance,
            out PlacedRoom newCurrentRoom
        )
        {
            newCurrentRoom = currentRoom;
            List<RoomTemplate> prefabs = new List<RoomTemplate>(category.Templates);
            Shuffle(prefabs);
            foreach (RoomTemplate prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                List<DoorSocket> exits = GetFreeExits(currentRoom.Template);
                if (exits.Count == 0)
                {
                    break;
                }

                Shuffle(exits);
                foreach (DoorSocket exit in exits)
                {
                    bool wantConnector = connectorCategory != null
                        && connectorCategory.Templates.Count > 0
                        && Random.value < connectorSpawnChance;

                    if (wantConnector
                        && TryAttachWithConnector(
                            currentRoom,
                            exit,
                            prefab,
                            connectorCategory,
                            out PlacedRoom afterConnector))
                    {
                        newCurrentRoom = afterConnector;
                        return true;
                    }

                    if (TryAttachDirect(currentRoom, exit, prefab, out PlacedRoom placed))
                    {
                        newCurrentRoom = placed;
                        return true;
                    }
                }
            }

            return false;
        }

        void PlaceSideRooms(
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance
        )
        {
            List<PlacedRoom> snapshot = new List<PlacedRoom>(placed);

            foreach (PlacedRoom room in snapshot)
            {
                RoomCategoryConfig sourceCategory = FindCategoryForRoom(room, layoutSequence, defaultSideCategory);
                float sideChance = sourceCategory != null ? sourceCategory.SideRoomChance : 0f;

                List<RoomTemplate> sidePrefabs = GetSideTemplates(sourceCategory, defaultSideCategory);
                if (sidePrefabs == null || sidePrefabs.Count == 0)
                {
                    continue;
                }

                List<DoorSocket> exits = GetFreeExits(room.Template);
                foreach (DoorSocket exit in exits)
                {
                    if (Random.value >= sideChance)
                    {
                        continue;
                    }

                    TryPlaceSideBranch(room, exit, sidePrefabs, connectorCategory, connectorSpawnChance);
                }
            }
        }

        void TryPlaceSideBranch(
            PlacedRoom fromRoom,
            DoorSocket exit,
            List<RoomTemplate> sidePrefabs,
            RoomCategoryConfig connectorCategory,
            float connectorSpawnChance
        )
        {
            List<RoomTemplate> shuffled = new List<RoomTemplate>(sidePrefabs);
            Shuffle(shuffled);
            foreach (RoomTemplate prefab in shuffled)
            {
                if (prefab == null)
                {
                    continue;
                }

                bool wantConnector = connectorCategory != null
                    && connectorCategory.Templates.Count > 0
                    && Random.value < connectorSpawnChance;

                if (wantConnector && TryAttachWithConnector(fromRoom, exit, prefab, connectorCategory, out _))
                {
                    return;
                }

                if (TryAttachDirect(fromRoom, exit, prefab, out _))
                {
                    return;
                }
            }
        }

        bool TryAttachDirect(PlacedRoom fromRoom, DoorSocket exit, RoomTemplate prefab, out PlacedRoom newRoom)
        {
            newRoom = null;
            RoomTemplate room = prefab.Clone();
            DoorSocket entrance = GetFirstFreeEntrance(room);
            if (entrance == null)
            {
                return false;
            }

            AlignRooms(fromRoom, exit, room, entrance, out Vector3 pos, out Quaternion rot);

            if (!BoundsHelper.CanPlace(room, pos, rot, occupied, out Bounds footprint))
            {
                return false;
            }

            exit.IsConnected = true;
            entrance.IsConnected = true;
            occupied.Add(footprint);
            newRoom = new PlacedRoom(room, pos, rot);
            placed.Add(newRoom);
            return true;
        }

        bool TryAttachWithConnector(
            PlacedRoom fromRoom,
            DoorSocket exit,
            RoomTemplate prefab,
            RoomCategoryConfig connectorCategory,
            out PlacedRoom newRoom
        )
        {
            newRoom = null;

            List<RoomTemplate> connectors = new List<RoomTemplate>(connectorCategory.Templates);
            Shuffle(connectors);

            foreach (RoomTemplate connPrefab in connectors)
            {
                if (connPrefab == null)
                {
                    continue;
                }

                RoomTemplate connector = connPrefab.Clone();
                DoorSocket connEntrance = GetFirstFreeEntrance(connector);
                if (connEntrance == null)
                {
                    continue;
                }

                AlignRooms(fromRoom, exit, connector, connEntrance, out Vector3 connPos, out Quaternion connRot);

                if (!BoundsHelper.CanPlace(connector, connPos, connRot, occupied, out Bounds connFootprint))
                {
                    continue;
                }

                PlacedRoom placedConnector = new PlacedRoom(connector, connPos, connRot);
                List<DoorSocket> connExits = GetFreeExits(connector);

                foreach (DoorSocket connExit in connExits)
                {
                    RoomTemplate room = prefab.Clone();
                    DoorSocket entrance = GetFirstFreeEntrance(room);
                    if (entrance == null)
                    {
                        continue;
                    }

                    AlignRooms(placedConnector, connExit, room, entrance, out Vector3 roomPos, out Quaternion roomRot);

                    if (!BoundsHelper.CanPlace(room, roomPos, roomRot, occupied, new[] { connFootprint }, out Bounds roomFootprint))
                    {
                        continue;
                    }

                    exit.IsConnected = true;
                    connEntrance.IsConnected = true;
                    connExit.IsConnected = true;
                    entrance.IsConnected = true;

                    occupied.Add(connFootprint);
                    occupied.Add(roomFootprint);

                    placed.Add(placedConnector);
                    newRoom = new PlacedRoom(room, roomPos, roomRot);
                    placed.Add(newRoom);
                    return true;
                }
            }

            return false;
        }

        static RoomCategoryConfig GetCategoryForIndex(int index, int totalRooms, List<RoomCategoryConfig> layoutSequence)
        {
            int count = layoutSequence.Count;
            if (index == 0)
            {
                return layoutSequence[0];
            }

            if (index == totalRooms - 1)
            {
                return layoutSequence[count - 1];
            }

            if (count <= 2)
            {
                return layoutSequence[Mathf.Clamp(index, 0, count - 1)];
            }

            if (totalRooms <= 3)
            {
                return layoutSequence[count / 2];
            }

            float t =
            // Move needle to minus one to ignore the start room
            (float)(index - 1)
            // Get only the middle of the room list (ignoring start, boss room and end room)
            / (totalRooms - 3);

            int midCount = count - 2; // Remove Start and Boss room, get only what's in the middle of the layou sequence

            // Shift plus 1 to start after Start Room
            // Mid count - 1 will clamp the max value to the last valid room that is not a boss room or end room
            int categoryIndex = 1 + Mathf.Clamp(Mathf.RoundToInt(t * (midCount - 1)), 0, midCount - 1);
            return layoutSequence[categoryIndex];
        }

        static RoomCategoryConfig FindCategoryForRoom(
            PlacedRoom placed,
            List<RoomCategoryConfig> layoutSequence,
            RoomCategoryConfig defaultSideCategory
        )
        {
            foreach (RoomCategoryConfig category in layoutSequence)
            {
                if (category == null)
                {
                    continue;
                }

                foreach (RoomTemplate template in category.Templates)
                {
                    if (template != null && template.Id == placed.Template.Id)
                    {
                        return category;
                    }
                }
            }

            if (defaultSideCategory != null)
            {
                foreach (RoomTemplate template in defaultSideCategory.Templates)
                {
                    if (template != null && template.Id == placed.Template.Id)
                    {
                        return defaultSideCategory;
                    }
                }
            }

            return null;
        }

        static List<RoomTemplate> GetSideTemplates(RoomCategoryConfig sourceCategory, RoomCategoryConfig defaultSideCategory)
        {
            if (sourceCategory != null
            && sourceCategory.SideRoomTemplates != null
            && sourceCategory.SideRoomTemplates.Count > 0)
            {
                return sourceCategory.SideRoomTemplates;
            }

            if (defaultSideCategory != null)
            {
                return defaultSideCategory.Templates;
            }

            return null;
        }

        // Door Helpers

        static List<DoorSocket> GetFreeExits(RoomTemplate template)
        {
            List<DoorSocket> list = new();
            foreach (DoorSocket exit in template.Exits)
            {
                if (!exit.IsConnected)
                {
                    list.Add(exit);
                }
            }

            return list;
        }

        static DoorSocket GetFirstFreeEntrance(RoomTemplate template)
        {
            foreach (DoorSocket entrance in template.Entrances)
            {
                if (!entrance.IsConnected)
                {
                    return entrance;
                }
            }

            return null;
        }

        // Alignment

        static void AlignRooms(
            PlacedRoom roomA,
            DoorSocket exit,
            RoomTemplate roomB,
            DoorSocket entrance,
            out Vector3 targetPos,
            out Quaternion targetRot
        )
        {
            Vector3 scaledExit = Vector3.Scale(exit.LocalPosition, roomA.Template.LocalScale);
            Vector3 exitWorldPos = roomA.Position + roomA.Rotation * scaledExit;
            Quaternion exitWorldRot = roomA.Rotation * exit.LocalRotation;

            targetRot = exitWorldRot * Quaternion.Inverse(entrance.LocalRotation);

            Vector3 scaledEntrance = Vector3.Scale(entrance.LocalPosition, roomB.LocalScale);
            Vector3 rotatedEntrance = targetRot * scaledEntrance;
            Vector3 exitForward = exitWorldRot * Vector3.forward;
            Vector3 separation = exitForward * 0.000005f;
            targetPos = exitWorldPos - rotatedEntrance + separation;
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
