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

        public void ForceImmediateGeneration()
        {
            StopAllCoroutines();
            GenerateAllImmediately();
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
            this.size = size;
            this.seed = seed;

            this.resourcesSpawned = resourcesSpawned;
            this.enemiesSpawned = enemiesSpawned;

            rng = new System.Random(seed);

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
            Debug.Log(GetComponentInChildren<Renderer>().bounds.size);

        }

        private IEnumerator BuildChunk()
        {
            if (biome == null)
                yield break;

            usedSpawnCells.Clear();

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
            // TILEMAP-BASED BIOME (INSTANT TILE FILL)
            // =========================
            EnsureTilemapReady();

            FillTilemapInstant();

            CacheTilemapBoundsForSpawning();

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

        // =========================
        // NEW: INSTANT GENERATION PATH
        // =========================
        private void GenerateAllImmediately()
        {
            if (biome == null)
                return;

            usedSpawnCells.Clear();

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
                    SpawnResourcesImmediate();
                    resourcesSpawned = true;
                }

                if (!enemiesSpawned)
                {
                    SpawnEnemiesImmediate();
                    enemiesSpawned = true;
                }

                return;
            }

            EnsureTilemapReady();

            FillTilemapInstant();

            CacheTilemapBoundsForSpawning();

            if (!resourcesSpawned)
            {
                SpawnResourcesImmediate();
                resourcesSpawned = true;
            }

            if (!enemiesSpawned)
            {
                SpawnEnemiesImmediate();
                enemiesSpawned = true;
            }
        }

        private void EnsureTilemapReady()
        {
            if (groundTilemap == null)
            {
                groundTilemap = GetComponentInChildren<Tilemap>(true);
            }

            if (groundTilemap != null)
            {
                groundTilemap.gameObject.SetActive(true);
            }

            grid = GetComponentInChildren<Grid>(true);
            if (grid != null)
                grid.enabled = true;
        }

        private void FillTilemapInstant()
        {
            if (groundTilemap == null || biome == null || biome.groundTile == null)
                return;

            groundTilemap.ClearAllTiles();

            int halfSizeX = Mathf.FloorToInt(size * 0.5f);
            int halfSizeY = Mathf.FloorToInt(size * 0.5f);

            // Fill square size x size centered at (0,0)
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x - halfSizeX, y - halfSizeY, 0);
                    groundTilemap.SetTile(tilePos, biome.groundTile);
                }
            }

            groundTilemap.CompressBounds();
        }

        private void CacheTilemapBoundsForSpawning()
        {
            // This makes resource/enemy spawn area match the tile chunk area,
            // instead of being (0,0) or some prefab bounds.
            float cellW = 1f;
            float cellH = 1f;

            if (grid != null)
            {
                cellW = Mathf.Abs(grid.cellSize.x);
                cellH = Mathf.Abs(grid.cellSize.y);
            }

            float worldW = size * cellW;
            float worldH = size * cellH;

            biomeBounds = new Bounds(
                transform.position,
                new Vector3(worldW, worldH, 0f)
            );

            biomeColliders2D = null;
            biomeColliders3D = null;
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

            size = Mathf.CeilToInt(Mathf.Max(sizeWorld.x, sizeWorld.y));

            // 🔥 DO NOT MOVE CHUNK TRANSFORM
            // Instead reposition prefab child so its bottom-left aligns with chunk origin

            Vector3 offset = bounds.center - transform.position;
            prefabInstance.transform.position -= offset;

            biomeBounds = bounds;
        }

        bool UsesTilemap()
        {
            return groundTilemap != null && biome != null && biome.groundTile != null;
        }

        IEnumerator SpawnResourcesCoroutine()
        {
            if (biome == null || biome.resourceSpawns == null) yield break;

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
            if (biome == null || biome.creatureSpawns == null) yield break;

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

        // NEW: immediate versions (so ForceImmediateGeneration is truly immediate)
        private void SpawnResourcesImmediate()
        {
            if (biome == null || biome.resourceSpawns == null) return;

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
                }
            }
        }

        private void SpawnEnemiesImmediate()
        {
            if (biome == null || biome.creatureSpawns == null) return;

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
                }
            }
        }

        Vector3 GetUniqueRandomWorldPos()
        {
            // If bounds were never cached, fall back to chunk center
            if (biomeBounds.size.x <= 0.0001f || biomeBounds.size.y <= 0.0001f)
                biomeBounds = new Bounds(transform.position, new Vector3(size, size, 0f));

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

            // 🔥 DO NOT MODIFY transform.position HERE
        }

        bool IsInsideBiome(Vector3 worldPos)
        {
            if (biomeColliders2D != null && biomeColliders2D.Length > 0)
            {
                foreach (var col in biomeColliders2D)
                {
                    if (col != null && col.OverlapPoint(worldPos))
                        return true;
                }
                return false;
            }

            if (biomeColliders3D != null && biomeColliders3D.Length > 0)
            {
                foreach (var col in biomeColliders3D)
                {
                    if (col != null && col.bounds.Contains(worldPos))
                        return true;
                }
                return false;
            }

            return biomeBounds.Contains(worldPos);
        }

        public Vector2 GetWorldSize()
        {
            if (UsesTilemap() && groundTilemap != null)
            {
                groundTilemap.CompressBounds();

                var bounds = groundTilemap.localBounds;
                Vector3 worldSize = groundTilemap.transform.TransformVector(bounds.size);

                return new Vector2(
                    Mathf.Abs(worldSize.x),
                    Mathf.Abs(worldSize.y)
                );
            }

            // fallback for prefab
            return new Vector2(size, size);
        }

    }
}
