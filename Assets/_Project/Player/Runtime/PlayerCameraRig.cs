using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] float distance = 4f;
        [SerializeField] float height = 2f;
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;

        float yaw;
        float pitch = 15f;
        bool isEnabled = true;

        // Run after player movement has been processed in Update()
        // So we have the updated position to work with
        void LateUpdate()
        {
            if (!isEnabled || target == null || input == null)
            {
                return;
            }

            Vector2 look = input.Intent.Look;

            if (look.sqrMagnitude < 0.0001f)
            {
                ApplyTransform();
                return;
            }

            yaw += look.x * lookSensitivity;
            pitch -= look.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            ApplyTransform();
        }

        void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, height, -distance);
            Vector3 focus = target.position + Vector3.up * 1.5f;
            transform.position = focus + offset;
            transform.LookAt(focus);
        }

        public void SetCameraEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
