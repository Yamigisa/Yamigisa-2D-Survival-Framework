using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        public WorldChunk chunkPrefab;

        [Header("Biomes")]
        public List<BiomeData> availableBiomes;

        [Header("World Size")]
        public int worldWidth = 20;
        public int worldHeight = 20;
        public int chunkSize = 16;

        private bool[,] occupied;

        private void Start()
        {
            //StartCoroutine(GenerateWorldCoroutine());
            SpawnChunk();
        }

        private void SpawnChunk()
        {
            Vector3 pos = Character.instance.transform.position;

            BiomeData biome = availableBiomes[Random.Range(0, availableBiomes.Count)];

            WorldChunk chunk = Instantiate(chunkPrefab, pos, Quaternion.identity);
            chunk.biome = biome;
            chunk.size = chunkSize;
        }
        // IEnumerator GenerateWorldCoroutine()
        // {
        //     occupied = new bool[worldWidth, worldHeight];

        //     // center index of grid
        //     int centerX = worldWidth / 2;
        //     int centerY = worldHeight / 2;

        //     foreach (var biome in availableBiomes)
        //     {
        //         int sizeX = biome.regionSizeX;
        //         int sizeY = biome.regionSizeY;

        //         Vector2Int pos = FindFreeRegion(sizeX, sizeY);

        //         for (int x = 0; x < sizeX; x++)
        //             for (int y = 0; y < sizeY; y++)
        //             {
        //                 int wx = pos.x + x;
        //                 int wy = pos.y + y;

        //                 occupied[wx, wy] = true;

        //                 // OFFSET FROM CENTER, NOT FROM (0,0)
        //                 int gridX = wx - centerX;
        //                 int gridY = wy - centerY;

        //                 SpawnChunk(gridX, gridY, biome);
        //                 yield return null;
        //             }
        //     }
        // }

        Vector2Int FindFreeRegion(int sizeX, int sizeY)
        {
            for (int i = 0; i < 1000; i++)
            {
                int x = Random.Range(0, worldWidth - sizeX);
                int y = Random.Range(0, worldHeight - sizeY);

                bool free = true;

                for (int ix = 0; ix < sizeX; ix++)
                    for (int iy = 0; iy < sizeY; iy++)
                        if (occupied[x + ix, y + iy])
                            free = false;

                if (free)
                    return new Vector2Int(x, y);
            }

            Debug.LogError("No free space for biome!");
            return new Vector2Int(0, 0);
        }

        void SpawnChunk(int gridX, int gridY, BiomeData biome)
        {
            Vector3 origin = Character.instance.transform.position;

            Vector3 pos = origin + new Vector3(
                gridX * chunkSize,
                gridY * chunkSize,
                0
            );

            WorldChunk chunk = Instantiate(chunkPrefab, pos, Quaternion.identity);
            chunk.biome = biome;
        }
    }
}
