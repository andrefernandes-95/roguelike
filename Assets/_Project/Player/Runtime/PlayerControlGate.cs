using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerDodge dodge;

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
            motor.SetMotorEnabled(gameplay);
            dodge.SetDodgeEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            if (!gameplay)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
