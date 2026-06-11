using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] PlayerMotor motor;

        PlayerCameraRig cameraRig;
        RunCoordinator runCoordinator;
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
            ApplyState(GetCoordinator()?.State ?? RunState.Boot);
        }

        void Update()
        {
            if (runCoordinator == null)
            {
                return;
            }

            RunState state = runCoordinator.State;
            if (state != lastState)
            {
                ApplyState(state);
            }
        }

        void ApplyState(RunState state)
        {
            lastState = state;

            bool gameplay = state == RunState.FloorActive;
            input.SetInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            if (!gameplay)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        static RunCoordinator GetCoordinator()
        {
            return RunCoordinator.Instance;
        }
    }
}
