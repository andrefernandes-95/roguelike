using System.Collections.Generic;
using UnityEngine;

namespace AF.Dungeon
{
    [CreateAssetMenu(fileName = "RoomCategory", menuName = "AF/Dungeon/Room Category")]
    public sealed class RoomCategoryData : ScriptableObject
    {
        public string categoryName;
        public List<GameObject> prefabs = new List<GameObject>();
        [Range(0f, 1f)] public float sideRoomChance;
        public RoomCategoryData sideRoomCategory;

        public RoomCategoryConfig ToConfig()
        {
            var config = new RoomCategoryConfig
            {
                Name = categoryName,
                SideRoomChance = sideRoomChance
            };

            foreach (GameObject prefab in prefabs)
            {
                RoomTemplate template = RoomPrefabData.BuildTemplateFromPrefab(prefab);
                if (template != null)
                {
                    config.Templates.Add(template);
                }
            }

            if (sideRoomCategory != null)
            {
                config.SideRoomTemplates = new List<RoomTemplate>();
                foreach (GameObject prefab in sideRoomCategory.prefabs)
                {
                    RoomTemplate template = RoomPrefabData.BuildTemplateFromPrefab(prefab);
                    if (template != null)
                    {
                        config.SideRoomTemplates.Add(template);
                    }
                }
            }

            return config;
        }
    }
}
