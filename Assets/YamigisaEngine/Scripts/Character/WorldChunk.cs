using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class WorldChunk : MonoBehaviour
    {
        [HideInInspector] public BiomeData biome;
        public Tilemap groundTilemap;

        public int resourceCount = 5;
        public int enemyCount = 3;

        [HideInInspector] public int size = 16;

        private void Start()
        {
            StartCoroutine(BuildChunk());
        }

        IEnumerator BuildChunk()
        {
            int halfsize = size / 2;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    // Offset tile positions so chunk is centered on transform.position
                    Vector3Int tilePos = new Vector3Int(
                        x - halfsize,
                        y - halfsize,
                        0
                    );

                    groundTilemap.SetTile(tilePos, biome.groundTile);

                    if ((x * size + y) % 16 == 0)
                        yield return null;
                }

            yield return StartCoroutine(SpawnResourcesCoroutine());
            yield return StartCoroutine(SpawnEnemiesCoroutine());
        }


        IEnumerator SpawnResourcesCoroutine()
        {
            foreach (var prefab in biome.resourcePrefabs)
            {
                for (int i = 0; i < resourceCount; i++)
                {
                    Instantiate(prefab, GetRandomWorldPos(), Quaternion.identity, transform);
                    yield return null; // 1 spawn per frame
                }
            }
        }

        IEnumerator SpawnEnemiesCoroutine()
        {
            foreach (var prefab in biome.enemyPrefabs)
            {
                for (int i = 0; i < enemyCount; i++)
                {
                    Instantiate(prefab, GetRandomWorldPos(), Quaternion.identity, transform);
                    yield return null;
                }
            }
        }

        Vector3 GetRandomWorldPos()
        {
            int halfsize = size / 2;

            return transform.position + new Vector3(
                Random.Range(-halfsize, halfsize),
                Random.Range(-halfsize, halfsize),
                0
            );
        }

    }
}
