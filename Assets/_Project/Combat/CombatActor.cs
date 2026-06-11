using AF.Stats;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CombatActor : MonoBehaviour
    {
        [SerializeField] StatProfile baseProfile = StatProfile.Default;

        StatSheet sheet;

        public StatSheet Sheet
        {
            get
            {
                if (sheet == null)
                {
                    sheet = new StatSheet(baseProfile);
                }

                return sheet;
            }
        }

        public void ApplyEquipment(string sourceId, StatModifier modifier)
        {
            Sheet.AddModifiers(sourceId, new[] { modifier });
        }

        public void RemoveEquipment(string sourceId)
        {
            Sheet.RemoveModifiers(sourceId);
        }
    }
}
