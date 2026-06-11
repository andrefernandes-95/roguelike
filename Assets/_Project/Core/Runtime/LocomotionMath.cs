using UnityEngine;

namespace AF.Core
{
    public static class LocomotionMath
    {
        public static float ComputeJumpVelocity(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        public static Vector3 CameraRelativeMove(Vector2 moveInput, float cameraYawDegrees)
        {
            Vector3 direction = new(moveInput.x, 0f, moveInput.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, cameraYawDegrees, 0f);
            return yawRotation * direction;
        }

        public static Vector3 FlattenForward(Vector3 worldForward)
        {
            worldForward.y = 0;
            return worldForward.sqrMagnitude > 0.0001f ? worldForward.normalized : Vector3.forward;
        }

        public static float SmoothCameraDistance(
            float current,
            float target,
            float pushInSpeed,
            float pullOutSpeed,
            float deltaTime
        )
        {
            float speed = target < current ? pushInSpeed : pullOutSpeed;
            return Mathf.Lerp(current, target, deltaTime * speed);
        }
    }
}
