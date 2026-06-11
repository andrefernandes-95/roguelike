using UnityEngine;

namespace AF.Combat
{
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] Transform ownerRoot;
        [SerializeField] HealthComponent health;

        public Transform OwnerRoot => ownerRoot != null ? ownerRoot : transform.root;

        public void ReceiveHit(int damage)
        {
            if (health == null)
            {
                return;
            }

            health.ApplyDamage(damage);
        }
    }
}
