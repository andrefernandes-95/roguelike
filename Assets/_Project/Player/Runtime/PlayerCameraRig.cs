using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputAdapter input;

        [Header("Orbit")]
        [SerializeField] float distance = 4f;
        [SerializeField] float focusHeight = 1.5f;
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;

        [Header("Collision")]
        [SerializeField] LayerMask collisionLayers;
        [SerializeField] float sphereRadius = 0.25f;
        [SerializeField] float minDistance = 1f;
        [SerializeField] float pushInSpeed = 12f;
        [SerializeField] float pullOutSpeed = 4f;

        float yaw;
        float pitch = 15f;
        float currentDistance;
        bool isEnabled = true;

        public float YawDegrees => yaw;
        public Transform Target => target;

        void Awake()
        {
            currentDistance = distance;
        }

        // Run after player movement has been processed in Update()
        // So we have the updated position to work with
        void LateUpdate()
        {
            if (!isEnabled || target == null || input == null)
            {
                return;
            }

            Vector2 look = input.Intent.Look;

            if (look.sqrMagnitude >= .0001f)
            {
                yaw += look.x * lookSensitivity;
                pitch -= look.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            ApplyTransform();
        }

        void ApplyTransform()
        {
            // Pure yaw-then-pitch orbit — position along camera back axis so rotation
            // matches aim. Avoids LookAt gimbal flip when pitch is clamped at the limit.
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * focusHeight;
            Vector3 orbitBack = rotation * Vector3.back;
            float castLength = distance;

            Vector3 castDirection = orbitBack;

            float targetDistance = castLength;
            if (Physics.SphereCast(
                focus,
                sphereRadius,
                castDirection,
                out RaycastHit hit,
                castLength,
                collisionLayers,
                QueryTriggerInteraction.Ignore
            ))
            {
                targetDistance = Mathf.Clamp(hit.distance - sphereRadius * 0.5f, minDistance, castLength);
            }

            currentDistance = LocomotionMath.SmoothCameraDistance(
                currentDistance,
                targetDistance,
                pushInSpeed,
                pullOutSpeed,
                Time.deltaTime
            );

            transform.position = focus + castDirection * currentDistance;
            transform.rotation = rotation;
        }

        public void SetCameraEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public void Initialize(PlayerInputAdapter playerInputAdapter)
        {
            input = playerInputAdapter;
            target = playerInputAdapter.transform;
        }
    }
}
