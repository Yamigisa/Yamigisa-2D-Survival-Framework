using UnityEngine;

namespace Yamigisa
{
    public class CharacterBiome : MonoBehaviour
    {
        [SerializeField] private bool canApplyBiome = true;
        private BiomeData currentBiome;

        public void SetBiome(BiomeData biome)
        {
            if (!canApplyBiome) return;

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