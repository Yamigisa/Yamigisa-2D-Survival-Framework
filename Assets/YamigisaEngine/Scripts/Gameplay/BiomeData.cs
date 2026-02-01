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
        public float temperatureModifier = 0f;
        public float speedMultiplier = 1f;
        public float staminaDrainMultiplier = 1f;
    }
}