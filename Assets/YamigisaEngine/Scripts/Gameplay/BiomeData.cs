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

        [Header("Tiles (If using TileRule)")]
        public TileBase groundTile;

        [Header("Prefab (If not using TileRule)")]
        public GameObject biomePrefab;

        [Header("Spawning - Resources")]
        public List<BiomeSpawnEntry> resourceSpawns;

        [Header("Spawning - Creatures")]
        public List<BiomeSpawnEntry> creatureSpawns;

        [Header("Environment Modifiers")]
        public float speedAddition = 0f;
        public List<AttributeModifier> attributeModifiers;
    }

    [System.Serializable]
    public class BiomeSpawnEntry
    {
        public GameObject prefab;
        public int minSpawn;
        public int maxSpawn;
    }

    [System.Serializable]
    public class AttributeModifier
    {
        public AttributeType type;

        [Header("Additive Modifiers")]
        public float regenAddition = 0f;
        public float depleteAddition = 0f;
    }
}