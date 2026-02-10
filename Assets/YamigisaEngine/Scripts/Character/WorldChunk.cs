using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class WorldChunk : MonoBehaviour
    {
        private HashSet<Vector2Int> usedSpawnCells = new();

        [HideInInspector] public BiomeData biome;

        [Header("Auto detects if use tilemap or not, can leave null")]
        public Tilemap groundTilemap;

        [HideInInspector] public int size = 16;

        [HideInInspector] public int seed;

        private System.Random rng;

        [HideInInspector] public bool resourcesSpawned;
        [HideInInspector] public bool enemiesSpawned;

        private Grid grid;

        private Bounds biomeBounds;
        private Collider2D[] biomeColliders2D;
        private Collider[] biomeColliders3D;

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

                    GameObject biomeInstance = Instantiate(
                    biome.biomePrefab,
                    transform.position,
                     Quaternion.identity,
                    transform
                    );

                    CacheBiomeBounds(biomeInstance);

                    AutoResizeChunkToPrefab(biomeInstance);

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

        void AutoResizeChunkToPrefab(GameObject prefabInstance)
        {
            Bounds bounds = new Bounds(prefabInstance.transform.position, Vector3.zero);

            Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            Collider2D[] colliders2D = prefabInstance.GetComponentsInChildren<Collider2D>();
            foreach (var c in colliders2D)
                bounds.Encapsulate(c.bounds);

            Collider[] colliders3D = prefabInstance.GetComponentsInChildren<Collider>();
            foreach (var c in colliders3D)
                bounds.Encapsulate(c.bounds);

            Vector3 sizeWorld = bounds.size;

            int newSize = Mathf.CeilToInt(Mathf.Max(sizeWorld.x, sizeWorld.y));
            size = newSize;

            transform.position = bounds.center;
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
            for (int attempt = 0; attempt < 60; attempt++)
            {
                float x = Random.Range(biomeBounds.min.x, biomeBounds.max.x);
                float y = Random.Range(biomeBounds.min.y, biomeBounds.max.y);

                Vector3 worldPos = new Vector3(x, y, transform.position.z);

                if (!IsInsideBiome(worldPos))
                    continue;

                Vector2Int cell = Vector2Int.RoundToInt(worldPos - transform.position);
                if (usedSpawnCells.Contains(cell))
                    continue;

                usedSpawnCells.Add(cell);
                return worldPos;
            }

            return biomeBounds.center;
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

        void CacheBiomeBounds(GameObject biomeInstance)
        {
            Bounds bounds = new Bounds(biomeInstance.transform.position, Vector3.zero);

            Renderer[] renderers = biomeInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            Collider2D[] col2D = biomeInstance.GetComponentsInChildren<Collider2D>();
            foreach (var c in col2D)
                bounds.Encapsulate(c.bounds);

            Collider[] col3D = biomeInstance.GetComponentsInChildren<Collider>();
            foreach (var c in col3D)
                bounds.Encapsulate(c.bounds);

            biomeBounds = bounds;

            biomeColliders2D = col2D;
            biomeColliders3D = col3D;

            size = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.y));
            transform.position = bounds.center;
        }

        bool IsInsideBiome(Vector3 worldPos)
        {
            if (biomeColliders2D != null && biomeColliders2D.Length > 0)
            {
                foreach (var col in biomeColliders2D)
                {
                    if (col.OverlapPoint(worldPos))
                        return true;
                }
                return false;
            }

            if (biomeColliders3D != null && biomeColliders3D.Length > 0)
            {
                foreach (var col in biomeColliders3D)
                {
                    if (col.bounds.Contains(worldPos))
                        return true;
                }
                return false;
            }

            return biomeBounds.Contains(worldPos);
        }

    }
}
