using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "BiomeData", menuName = "Yamigisa/Biome")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        public string biomeName;
        public Color debugColor;

        [Header("Tiles")]
        public TileBase groundTile;

        [Header("Spawning")]
        public List<GameObject> resourcePrefabs;
        public List<GameObject> enemyPrefabs;

        [Header("Environment Modifiers")]
        public float speedAddition = 0f; // additive
        public List<AttributeModifier> modifiers;
    }

    [System.Serializable]
    public class AttributeModifier
    {
        public AttributeType type;

        [Header("Additive Modifiers")]
        public float regenAddition = 0f;     // += to RegenerateValuePerMinute
        public float depleteAddition = 0f;   // += to DepleteValuePerMinute
    }
}