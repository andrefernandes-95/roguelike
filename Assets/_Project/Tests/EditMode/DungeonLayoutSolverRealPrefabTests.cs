#if UNITY_EDITOR
using System.Collections.Generic;
using AF.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests
{
    public class DungeonLayoutSolverRealPrefabTests
    {
        const string StartPrefabPath = "Assets/Data/Dungeons/Start Room.prefab";
        const string MiddlePrefabPath = "Assets/Data/Dungeons/Middle Room.prefab";
        const string EndPrefabPath = "Assets/Data/Dungeons/End Room.prefab";

        [Test]
        public void TrySolveLayout_RealPrefabs_PlacesThreeRooms()
        {
            RoomTemplate start = LoadTemplate(StartPrefabPath);
            RoomTemplate middle = LoadTemplate(MiddlePrefabPath);
            RoomTemplate end = LoadTemplate(EndPrefabPath);

            Assert.NotNull(start, "Start Room prefab missing or has no bounds");
            Assert.NotNull(middle, "Middle Room prefab missing or has no bounds");
            Assert.NotNull(end, "End Room prefab missing or has no bounds");

            var startCat = new RoomCategoryConfig { Name = "Start" };
            startCat.Templates.Add(start);

            var midCat = new RoomCategoryConfig { Name = "Mid" };
            midCat.Templates.Add(middle);

            var endCat = new RoomCategoryConfig { Name = "End" };
            endCat.Templates.Add(end);

            var sequence = new List<RoomCategoryConfig> { startCat, midCat, endCat };

            var solver = new DungeonLayoutSolver();
            bool ok = solver.TrySolveLayout(
                roomSize: 3,
                sequence,
                defaultSideCategory: null,
                connectorCategory: null,
                connectorSpawnChance: 0f,
                baseSeed: 42,
                maxAttempts: 50,
                out List<PlacedRoom> result,
                out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(3, result.Count);
        }

        static RoomTemplate LoadTemplate(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return prefab != null ? RoomPrefabData.BuildTemplateFromPrefab(prefab) : null;
        }
    }
}
#endif
