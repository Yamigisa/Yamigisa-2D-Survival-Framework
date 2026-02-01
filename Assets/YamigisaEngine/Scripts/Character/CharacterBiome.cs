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
            // playerSpeed = baseSpeed * currentBiome.speedMultiplier;
            // staminaDrain = baseDrain * currentBiome.staminaDrainMultiplier;
            // temperature += currentBiome.temperatureModifier;
        }
    }
}