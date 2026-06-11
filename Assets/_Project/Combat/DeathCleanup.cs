using UnityEngine;

namespace AF.Combat
{
    public sealed class DeathCleanup : MonoBehaviour
    {
        [SerializeField] HealthComponent health;

        void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        void OnDied()
        {
            gameObject.SetActive(false);
        }
    }
}
