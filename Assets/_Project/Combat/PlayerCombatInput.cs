using UnityEngine;
using AF.Player;

namespace AF.Combat
{
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] CombatController combat;
        [SerializeField] MeleeHitboxAction lightAttack;
        IPlayerIntentSource intentSource;

        void Awake()
        {
            intentSource = GetComponent<IPlayerIntentSource>();
        }

        void Update()
        {
            if (intentSource == null || combat == null || lightAttack == null)
            {
                return;
            }

            if (intentSource.Intent.LightAttack)
            {
                combat.TryStart(lightAttack);
            }
        }
    }
}
