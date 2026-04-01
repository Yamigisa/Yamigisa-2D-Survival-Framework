    using UnityEngine;

    namespace Yamigisa
    {
        public class BiomeTrigger : MonoBehaviour
        {
            [Header("If Customized Regions")]
            [SerializeField] private bool useCustomBiome = false;

            [SerializeField] private BiomeData customBiomeData;

            private BiomeData biomeData;

            void Start()
            {
                if (useCustomBiome)
                {
                    biomeData = customBiomeData;
                }
                else
                {
                    WorldChunk chunk = GetComponentInParent<WorldChunk>();

                    if (chunk != null)
                        biomeData = chunk.biome;
                    else
                        Debug.LogWarning($"BiomeTrigger on {name} has no WorldChunk parent.");
                }
            }

            void OnTriggerEnter2D(Collider2D other)
            {
                if (biomeData == null)
                    return;

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