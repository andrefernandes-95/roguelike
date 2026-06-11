using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerInputAdapter : MonoBehaviour, IPlayerIntentSource
    {
        PlayerInputActions actions;
        bool isEnabled;

        public PlayerIntent Intent { get; private set; }

        void Awake()
        {
            actions = new PlayerInputActions();
        }

        void OnDestroy()
        {
            actions?.Dispose();
        }

        void Update()
        {
            if (!isEnabled)
            {
                Intent = default;
                return;
            }

            Intent = new PlayerIntent
            {
                Move = actions.Gameplay.Move.ReadValue<Vector2>(),
                Look = actions.Gameplay.Look.ReadValue<Vector2>(),
                Dodge = actions.Gameplay.Dodge.WasPressedThisFrame(),
                LightAttack = actions.Gameplay.LightAttack.WasPressedThisFrame(),
                Block = actions.Gameplay.Block.IsPressed(),
                Jump = actions.Gameplay.Jump.WasPressedThisFrame()
            };
        }

        public void SetInputEnabled(bool enabled)
        {
            if (isEnabled == enabled)
            {
                return;
            }

            isEnabled = enabled;
            if (enabled)
            {
                actions.Gameplay.Enable();
            }
            else
            {
                actions.Gameplay.Disable();
                Intent = default;
            }
        }
    }
}
