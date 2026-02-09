using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class WorldChunk : MonoBehaviour
    {
        private HashSet<Vector2Int> usedSpawnCells = new();

        public BiomeData biome;

        [Header("Auto detects if use tilemap or not, can leave null")]
        public Tilemap groundTilemap;

        [HideInInspector] public int size = 16;

        [HideInInspector] public int seed;

        private System.Random rng;

        [HideInInspector] public bool resourcesSpawned;
        [HideInInspector] public bool enemiesSpawned;

        private Grid grid;

        public void Initialize(BiomeData biome, int size, int seed)
        {
            this.biome = biome;
            this.size = size;
            this.seed = seed;

            rng = new System.Random(seed);

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        public void Initialize(
            BiomeData biome,
            int size,
            int seed,
            bool resourcesSpawned,
            bool enemiesSpawned
        )
        {
            this.biome = biome;
            this.size = size; ;
            this.seed = seed;

            this.resourcesSpawned = resourcesSpawned;
            this.enemiesSpawned = enemiesSpawned;

            rng = new System.Random(seed);

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        private IEnumerator BuildChunk()
        {
            // =========================
            // PREFAB-BASED BIOME
            // =========================
            if (!UsesTilemap())
            {
                if (biome.biomePrefab != null)
                {
                    Tilemap tilemap = GetComponentInChildren<Tilemap>(true);
                    if (tilemap != null)
                        tilemap.gameObject.SetActive(false);

                    grid = GetComponentInChildren<Grid>(true);
                    if (grid != null)
                        grid.enabled = false;

                    Instantiate(
                        biome.biomePrefab,
                        transform.position,
                        Quaternion.identity,
                        transform
                    );
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

                yield break;
            }

            // =========================
            // TILEMAP-BASED BIOME
            // =========================
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

        bool UsesTilemap()
        {
            return groundTilemap != null && biome.groundTile != null;
        }

        IEnumerator SpawnResourcesCoroutine()
        {
            if (biome.resourceSpawns == null) yield break;

            foreach (var entry in biome.resourceSpawns)
            {
                if (entry.prefab == null) continue;

                int spawnCount = rng.Next(
                    Mathf.Max(0, entry.minSpawn),
                    Mathf.Max(entry.minSpawn, entry.maxSpawn) + 1
                );

                for (int i = 0; i < spawnCount; i++)
                {
                    Instantiate(entry.prefab, GetUniqueRandomWorldPos(), Quaternion.identity, transform);
                    yield return null;
                }
            }
        }

        IEnumerator SpawnEnemiesCoroutine()
        {
            if (biome.creatureSpawns == null) yield break;

            foreach (var entry in biome.creatureSpawns)
            {
                if (entry.prefab == null) continue;

                int spawnCount = rng.Next(
                    Mathf.Max(0, entry.minSpawn),
                    Mathf.Max(entry.minSpawn, entry.maxSpawn) + 1
                );

                for (int i = 0; i < spawnCount; i++)
                {
                    Instantiate(entry.prefab, GetUniqueRandomWorldPos(), Quaternion.identity, transform);

                    yield return null;
                }
            }
        }

        Vector3 GetUniqueRandomWorldPos()
        {
            int halfsize = size / 2;

            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = rng.Next(-halfsize, halfsize + 1);
                int y = rng.Next(-halfsize, halfsize + 1);

                Vector2Int cell = new Vector2Int(x, y);

                if (usedSpawnCells.Contains(cell))
                    continue;

                usedSpawnCells.Add(cell);
                return transform.position + new Vector3(x, y, 0f);
            }

            // fallback (very unlikely unless chunk is full)
            return transform.position;
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
