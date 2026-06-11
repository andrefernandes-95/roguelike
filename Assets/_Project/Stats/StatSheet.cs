using System;
using System.Collections.Generic;

namespace AF.Stats
{
    public sealed class StatSheet
    {
        readonly Dictionary<StatId, int> baseLevels = new();
        readonly Dictionary<string, List<StatModifier>> modifiersBySource = new();

        public StatSheet(StatProfile profile)
        {
            baseLevels[StatId.Vitality] = profile.Vitality;
            baseLevels[StatId.Endurance] = profile.Endurance;
        }

        public int GetTotal(StatId stat)
        {
            int total = baseLevels.TryGetValue(stat, out int baseLevel) ? baseLevel : 0;

            foreach (List<StatModifier> list in modifiersBySource.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Stat == stat)
                    {
                        total += list[i].FlatDelta;
                    }
                }
            }

            return Math.Max(0, total);
        }

        public void SetBase(StatId stat, int level)
        {
            baseLevels[stat] = level;
        }

        public void AddModifiers(string sourceId, IReadOnlyList<StatModifier> modifiers)
        {
            if (string.IsNullOrEmpty(sourceId) || modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            if (!modifiersBySource.TryGetValue(sourceId, out List<StatModifier> list))
            {
                list = new List<StatModifier>();
                modifiersBySource[sourceId] = list;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                list.Add(modifiers[i]);
            }
        }

        public void RemoveModifiers(string sourceId)
        {
            modifiersBySource.Remove(sourceId);
        }
    }
}