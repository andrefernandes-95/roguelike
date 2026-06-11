using UnityEngine;

namespace AF.Core
{
    public interface ILocomotionReadout
    {
        Vector2 MoveInput { get; }
        bool IsGrounded { get; }
    }
}
