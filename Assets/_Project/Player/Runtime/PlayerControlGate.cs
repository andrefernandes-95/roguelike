using AF.Character;
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] PlayerLocomotionInput playerLocomotionInput;
        [SerializeField] CharacterMotor motor;

        PlayerCameraRig cameraRig;
        RunState lastState = RunState.Boot;

        void Awake()
        {
            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
                cameraRig.Initialize(input);
            }
        }

        void Start()
        {
            ApplyState(RunCoordinator.Instance.State);
        }

        void Update()
        {
            if (RunCoordinator.Instance.State != lastState)
            {
                ApplyState(RunCoordinator.Instance.State);
            }
        }

        void ApplyState(RunState state)
        {
            lastState = state;

            bool gameplay = state == RunState.FloorActive;
            input.SetInputEnabled(gameplay);
            playerLocomotionInput.SetLocomotionInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            if (!gameplay)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
