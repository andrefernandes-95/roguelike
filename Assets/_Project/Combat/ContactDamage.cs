using UnityEngine;

namespace AF.Combat
{
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] int damagePerTick = 10;
        [SerializeField] float tickInterval = 0.5f;
        [SerializeField] Transform ownerRoot;

        float nextTickTime;

        void OnTriggerStay(Collider other)
        {
            if (Time.time < nextTickTime)
            {
                return;
            }

            if (!other.TryGetComponent(out Hurtbox hurtbox))
            {
                return;
            }

            Transform owner = ownerRoot != null ? ownerRoot : transform.root;
            if (hurtbox.OwnerRoot == owner)
            {
                return;
            }

            nextTickTime = Time.time + tickInterval;
            hurtbox.ReceiveHit(damagePerTick);
        }
    }
}
