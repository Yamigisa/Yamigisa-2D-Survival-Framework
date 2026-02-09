using UnityEngine;

namespace Yamigisa
{
    public class CharacterBiome : MonoBehaviour
    {
        private BiomeData currentBiome;

        public void SetBiome(BiomeData biome)
        {
            currentBiome = biome;
            ApplyBiome();
        }

        void ApplyBiome()
        {
            Character.instance.characterMovement.SetSpeedMultiplier(currentBiome.speedAddition);
            Character.instance.characterAttribute.ApplyBiomeModifiers(currentBiome.attributeModifiers);
        }
    }
}