using UnityEngine;

namespace Utility
{
    public class LockRotation : MonoBehaviour
    {
        void Update()
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
