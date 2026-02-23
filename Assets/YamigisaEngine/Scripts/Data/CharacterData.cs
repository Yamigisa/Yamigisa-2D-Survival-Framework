using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [System.Serializable]
    public class CharacterData
    {
        public Vector3 position;
        public Quaternion rotation;

        public List<AttributeSaveData> attributes;

    }

    [System.Serializable]
    public class AttributeSaveData
    {
        public AttributeType type;
        public float current;
        public float baseMax; // IMPORTANT: store base, not final
    }
}