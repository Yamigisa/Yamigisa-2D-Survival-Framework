using UnityEngine;

namespace Yamigisa
{
    [System.Serializable]
    public class InteractiveObjectSaveData
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public bool active;
        public bool pickedUp;
    }

}