using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "BiomeGroup", menuName = "Yamigisa/Biome Group")]
    public class BiomeGroup : ScriptableObject
    {
        public string groupName;
        public List<BiomeData> biomes;
    }
}
