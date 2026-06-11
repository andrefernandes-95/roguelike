using AF.Core;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.Player
{
    public sealed class LocomotionMathTests
    {
        [Test]
        public void ComputeJumpVelocity_MatchesPhysicalFormula()
        {
            float gravity = -20f;
            float jumpHeight = 1.2f;
            float expected = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Assert.AreEqual(expected, LocomotionMath.ComputeJumpVelocity(jumpHeight, gravity), 0.001f);
        }

        [Test]
        public void CameraRelativeMove_WithYawZero_MapStickToWorldXZ()
        {
            Vector3 result = LocomotionMath.CameraRelativeMove(new Vector2(0f, 1f), 0f);
            Assert.AreEqual(Vector3.forward, result);
        }

        [Test]
        public void CameraRelativeMove_WithYaw90_RotatesInput()
        {
            Vector3 result = LocomotionMath.CameraRelativeMove(new Vector2(0f, 1f), 90f);

            Assert.That(result.x, Is.EqualTo(Vector3.right.x).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(Vector3.right.y).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(Vector3.right.z).Within(0.001f));
        }
    }
}
