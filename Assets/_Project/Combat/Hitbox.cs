using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AF.Combat
{

    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] Transform ownerRoot;
        [SerializeField] UnityEvent onBeginSwing;
        [SerializeField] UnityEvent onEndSwing;

        int damage;
        readonly HashSet<Hurtbox> hitThisSwing = new();

        public void ConfigureDamage(int amount)
        {
            damage = amount;
        }

        public void BeginSwing()
        {
            hitThisSwing.Clear();
            gameObject.SetActive(true);
            onBeginSwing?.Invoke();
        }

        public void EndSwing()
        {
            hitThisSwing.Clear();
            onEndSwing?.Invoke();
            gameObject.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out Hurtbox hurtbox))
            {
                return;
            }

            Transform owner = ownerRoot != null ? ownerRoot : transform.root;
            if (hurtbox.OwnerRoot == owner)
            {
                return;
            }

            if (hitThisSwing.Contains(hurtbox))
            {
                return;
            }

            hitThisSwing.Add(hurtbox);
            hurtbox.ReceiveHit(damage);
        }
    }
}
