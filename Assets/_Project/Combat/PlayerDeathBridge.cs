using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class PlayerDeathBridge : MonoBehaviour
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
            RunCoordinator.Instance.NotifyPlayerDied();
        }
    }
}
