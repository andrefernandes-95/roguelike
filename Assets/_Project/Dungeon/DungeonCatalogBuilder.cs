using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Converts authoring assets into solver input and prefab lookup for spawning
    /// </summary>
    public static class DungeonCatalogBuilder
    {
        public static List<RoomCategoryConfig> ToLayoutSequence(
            IEnumerable<RoomCategoryData> sequence
        )
        {
            var configs = new List<RoomCategoryConfig>();
            if (sequence == null)
            {
                return configs;
            }

            foreach (RoomCategoryData category in sequence)
            {
                if (category != null)
                {
                    configs.Add((category.ToConfig()));
                }
            }

            return configs;
        }

        public static Dictionary<string, GameObject> BuildPrefabLookup(
            IEnumerable<RoomCategoryData> layoutSequence,
            RoomCategoryData sideCategory,
            RoomCategoryData connectorCategory
        )
        {
            var lookup = new Dictionary<string, GameObject>();

            void Register(RoomCategoryData category)
            {
                if (category == null)
                {
                    return;
                }

                foreach (GameObject prefab in category.prefabs)
                {
                    if (prefab != null)
                    {
                        lookup[prefab.name] = prefab;
                    }
                }

                Register(category.sideRoomCategory);
            }

            if (layoutSequence != null)
            {
                foreach (RoomCategoryData category in layoutSequence)
                {
                    Register(category);
                }
            }

            Register(sideCategory);
            Register(connectorCategory);

            return lookup;
        }
    }
}
