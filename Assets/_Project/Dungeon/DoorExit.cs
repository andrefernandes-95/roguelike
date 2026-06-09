using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Doorway out of this room. Forward (red) = out of room
    /// </summary>
    public sealed class DoorExit : MonoBehaviour
    {
        public bool IsConnected;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
