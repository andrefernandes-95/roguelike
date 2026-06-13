using AF.Combat;
using UnityEngine;

namespace AF.Player
{
    /// <summary>Maps player intent → CombatController.TryStart.</summary>
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] CombatController combat;
        [SerializeField] MeleeHitboxAction lightAttack;
        [SerializeField] DodgeCombatAction dodge;

        IPlayerIntentSource intentSource;

        void Awake()
        {
            intentSource = GetComponent<IPlayerIntentSource>();
        }

        void Update()
        {
            if (intentSource == null || combat == null)
            {
                return;
            }

            PlayerIntent intent = intentSource.Intent;

            if (intent.LightAttack && lightAttack != null)
            {
                combat.TryStart(lightAttack);
            }

            if (intent.Dodge && dodge != null)
            {
                combat.TryStart(dodge);
            }
        }
    }
}
