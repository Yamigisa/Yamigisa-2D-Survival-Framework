using UnityEngine;

namespace Yamigisa
{
    public class CharacterBiome : MonoBehaviour
    {
        private BiomeData currentBiome;

        private CharacterMovement characterMovement;
        private CharacterAttribute characterAttribute;

        void Start()
        {
            characterMovement = Character.instance.characterMovement;
            characterAttribute = Character.instance.characterAttribute;
        }
        public void SetBiome(BiomeData biome)
        {
            currentBiome = biome;
            ApplyBiome();
        }

        void ApplyBiome()
        {
            characterMovement.SetSpeedMultiplier(currentBiome.speedAddition);

            characterAttribute.ApplyBiomeModifiers(currentBiome.modifiers);
        }

    }
}