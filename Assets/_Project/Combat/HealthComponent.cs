using System;
using AF.Stats;
using UnityEngine;

namespace AF.Combat
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] CombatActor combatActor;
        [SerializeField] StatProfile fallbackProfile = StatProfile.Default;

        ResourcePool pool;
        StatSheet sheet;

        public event Action<DamageResult> Damaged;
        public event Action Died;

        public int MaxHealth => pool?.Max ?? 0;
        public int CurrentHealth => pool?.Current ?? 0;
        public bool IsDead => pool?.IsEmpty ?? false;

        void Awake()
        {
            sheet = combatActor != null ? combatActor.Sheet : new StatSheet(fallbackProfile);
            pool = new ResourcePool(DerivedStats.MaxHealth(sheet));
        }

        public void RefreshMaxFromStats()
        {
            pool.RefreshMax(DerivedStats.MaxHealth(sheet));
        }

        public void Fill()
        {
            pool.Fill();
        }

        public void ApplyDamage(int amount)
        {
            if (pool.IsEmpty)
            {
                return;
            }

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(amount));
            if (result.DamageDealt <= 0)
            {
                return;
            }

            Damaged?.Invoke(result);

            if (result.Depleted)
            {
                Died?.Invoke();
            }
        }
    }
}
