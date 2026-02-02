using UnityEngine;

namespace Yamigisa
{
    public class BiomeTrigger : MonoBehaviour
    {
        private BiomeData biomeData;

        void Start()
        {
            WorldChunk chunk = GetComponentInParent<WorldChunk>();
            biomeData = chunk.biome;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                CharacterBiome currentCharacterBiome =
             other.GetComponentInParent<CharacterBiome>();

                if (currentCharacterBiome != null)
                    currentCharacterBiome.SetBiome(biomeData);
            }
        }
    }
}