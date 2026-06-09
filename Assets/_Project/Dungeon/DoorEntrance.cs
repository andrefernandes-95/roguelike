using UnityEngine;

namespace AF.Dungeon
{
    /// <summary>
    /// Doorway into this room. Forward (blue) = into room
    /// </summary>
    public sealed class DoorEntrance : MonoBehaviour
    {
        public bool IsConnected;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
