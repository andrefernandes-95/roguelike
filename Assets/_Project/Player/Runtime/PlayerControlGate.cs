using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerCameraRig cameraRig;

        RunCoordinator runCoordinator;
        RunState lastState;

        void Awake()
        {
            runCoordinator = RunCoordinator.Instance;
        }

        void Update()
        {
            if (runCoordinator == null)
            {
                return;
            }

            RunState state = runCoordinator.State;
            if (state == lastState)
            {
                return;
            }

            lastState = state;
            bool gameplay = state == RunState.FloorActive;
            input.SetInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
    }
}
