using UnityEngine;

namespace Yamigisa
{
    public abstract class CollectibleBase : MonoBehaviour
    {
        public bool LockInteraction { get; set; } = false;

        public abstract void Collect();
    }
}