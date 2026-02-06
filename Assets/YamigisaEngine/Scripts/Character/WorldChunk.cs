using System;
using System.Collections;
using System.Collections.Generic;
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

        [HideInInspector] public int seed;

        private bool initialized;
        private System.Random rng;

        [HideInInspector] public bool resourcesSpawned;
        [HideInInspector] public bool enemiesSpawned;
        public void Initialize(BiomeData biome, int size, int resourceCount, int enemyCount, int seed)
        {
            this.biome = biome;
            this.size = size;
            this.resourceCount = resourceCount;
            this.enemyCount = enemyCount;
            this.seed = seed;

            rng = new System.Random(seed);
            initialized = true;

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        public void Initialize(
            BiomeData biome,
            int size,
            int resourceCount,
            int enemyCount,
            int seed,
            bool resourcesSpawned,
            bool enemiesSpawned
        )
        {
            this.biome = biome;
            this.size = size;
            this.resourceCount = resourceCount;
            this.enemyCount = enemyCount;
            this.seed = seed;

            this.resourcesSpawned = resourcesSpawned;
            this.enemiesSpawned = enemiesSpawned;

            rng = new System.Random(seed);
            initialized = true;

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        IEnumerator BuildChunk()
        {
            int halfsize = size / 2;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x - halfsize, y - halfsize, 0);
                    groundTilemap.SetTile(tilePos, biome.groundTile);

                    if ((x * size + y) % 16 == 0)
                        yield return null;
                }

            if (!resourcesSpawned)
            {
                yield return StartCoroutine(SpawnResourcesCoroutine());
                resourcesSpawned = true;
            }

            if (!enemiesSpawned)
            {
                yield return StartCoroutine(SpawnEnemiesCoroutine());
                enemiesSpawned = true;
            }
        }

        IEnumerator SpawnResourcesCoroutine()
        {
            if (biome.resourcePrefabs == null) yield break;

            for (int p = 0; p < biome.resourcePrefabs.Count; p++)
            {
                GameObject prefab = biome.resourcePrefabs[p];
                if (prefab == null) continue;

                for (int i = 0; i < resourceCount; i++)
                {
                    Instantiate(prefab, GetRandomWorldPos(), Quaternion.identity, transform);
                    yield return null;
                }
            }
        }

        IEnumerator SpawnEnemiesCoroutine()
        {
            if (biome.enemyPrefabs == null) yield break;

            for (int p = 0; p < biome.enemyPrefabs.Count; p++)
            {
                GameObject prefab = biome.enemyPrefabs[p];
                if (prefab == null) continue;

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

            float rx = (float)(rng.NextDouble() * (halfsize * 2) - halfsize);
            float ry = (float)(rng.NextDouble() * (halfsize * 2) - halfsize);

            return transform.position + new Vector3(rx, ry, 0f);
        }

        public void RestoreInteractiveObjects(List<InteractiveObjectSaveData> savedObjects)
        {
            if (savedObjects == null || savedObjects.Count == 0)
                return;

            InteractiveObject[] existing =
                GetComponentsInChildren<InteractiveObject>(true);

            foreach (var saved in savedObjects)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i].IdMatches(saved.id))
                    {
                        existing[i].Load(new SaveGameData
                        {
                            interactiveObjects = savedObjects
                        });
                        break;
                    }
                }
            }
        }

    }
}
