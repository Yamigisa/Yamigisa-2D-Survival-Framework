using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class WorldChunk : MonoBehaviour
    {
        private const float CHUNK_WIDTH = 25f;
        private const float CHUNK_HEIGHT = 14.4f;

        private const int TILE_WIDTH = 25;
        private const int TILE_HEIGHT = 14;

        private const float Y_SCALE = CHUNK_HEIGHT / TILE_HEIGHT;

        private HashSet<Vector2Int> usedSpawnCells = new();

        [HideInInspector] public BiomeData biome;

        [Header("Auto detects if use tilemap or not, can leave null")]
        public Tilemap groundTilemap;

        [HideInInspector] public int size = 16;
        [HideInInspector] public int seed;

        private System.Random rng;

        [HideInInspector] public bool resourcesSpawned;
        [HideInInspector] public bool enemiesSpawned;

        private Transform spawnedObjectsParent;
        private Bounds biomeBounds;

        private bool _spawnObjectsOnBuild;
        private bool _terrainBuilt;

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
            EnsureSpawnParent();
            StopAllCoroutines();
            GenerateAllImmediately();
        }

        public void Initialize(BiomeData biome, int size, int seed, bool spawnObjectsOnBuild)
        {
            this.biome = biome;
            this.size = size;
            this.seed = seed;

            rng = new System.Random(seed);

            _spawnObjectsOnBuild = spawnObjectsOnBuild;

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        private IEnumerator BuildChunk()
        {
            GenerateTerrainImmediate();
            _terrainBuilt = true;

            if (_spawnObjectsOnBuild)
            {
                GenerateObjectsOnlyImmediate();
            }

            yield break;
        }

        private void GenerateTerrainImmediate()
        {
            if (biome == null) return;

            usedSpawnCells.Clear();

            transform.localScale = new Vector3(1f, Y_SCALE, 1f);

            if (!UsesTilemap())
                GeneratePrefabChunk();
            else
                GenerateTileChunk();
        }

        public void GenerateObjectsOnlyImmediate()
        {
            if (biome == null) return;

            EnsureSpawnParent();

            // Make sure terrain exists (safe in case someone presses create objects first)
            if (!_terrainBuilt)
            {
                GenerateTerrainImmediate();
                _terrainBuilt = true;
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
        }

        public void SetSpawnFlags(bool resources, bool enemies)
        {
            resourcesSpawned = resources;
            enemiesSpawned = enemies;
        }

        private void GenerateAllImmediately()
        {
            if (biome == null) return;

            usedSpawnCells.Clear();

            // keep Grid cell size (1,1,1) but allow the chunk to represent 14.4 world height
            transform.localScale = new Vector3(1f, Y_SCALE, 1f);

            //ClearPreviousChunkContent();

            if (!UsesTilemap())
                GeneratePrefabChunk();
            else
                GenerateTileChunk();

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

        private void ClearPreviousChunkContent()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (groundTilemap != null && child == groundTilemap.transform) continue;

                Destroy(child.gameObject);
            }

            if (groundTilemap != null)
                groundTilemap.ClearAllTiles();
        }

        private void GenerateTileChunk()
        {
            if (groundTilemap == null)
                groundTilemap = GetComponentInChildren<Tilemap>(true);

            if (groundTilemap == null) return;

            groundTilemap.gameObject.SetActive(true);
            groundTilemap.transform.localPosition = Vector3.zero;

            groundTilemap.ClearAllTiles();

            // bottom-left anchored fill: tiles occupy [0..25) x [0..14)
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.groundTile);
                }
            }

            groundTilemap.CompressBounds();

            // chunk bounds in world (bottom-left origin at transform.position)
            biomeBounds = new Bounds(
                transform.position + new Vector3(CHUNK_WIDTH * 0.5f, CHUNK_HEIGHT * 0.5f, 0f),
                new Vector3(CHUNK_WIDTH, CHUNK_HEIGHT, 0f)
            );
        }

        private void GeneratePrefabChunk()
        {
            if (biome.biomePrefab == null) return;

            // hide tilemap if present
            if (groundTilemap != null)
                groundTilemap.gameObject.SetActive(false);

            GameObject instance = Instantiate(
                biome.biomePrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            // IMPORTANT: snap prefab bounds.min to chunk origin (edge-to-edge with tile chunks)
            SnapChildBoundsMinToChunkOrigin(instance);

            // chunk bounds in world (bottom-left origin at transform.position)
            biomeBounds = new Bounds(
                transform.position + new Vector3(CHUNK_WIDTH * 0.5f, CHUNK_HEIGHT * 0.5f, 0f),
                new Vector3(CHUNK_WIDTH, CHUNK_HEIGHT, 0f)
            );
        }

        private void SnapChildBoundsMinToChunkOrigin(GameObject childRoot)
        {
            // Compute combined bounds in WORLD space
            if (!TryGetWorldBounds(childRoot, out Bounds b))
                return;

            Vector3 chunkOrigin = transform.position; // bottom-left origin for the chunk
            Vector3 delta = b.min - chunkOrigin;

            // Move the child so that bounds.min == chunkOrigin
            childRoot.transform.position -= new Vector3(delta.x, delta.y, 0f);
        }

        private bool TryGetWorldBounds(GameObject root, out Bounds bounds)
        {
            bool has = false;
            bounds = new Bounds(root.transform.position, Vector3.zero);

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!has)
                {
                    bounds = renderers[i].bounds;
                    has = true;
                }
                else bounds.Encapsulate(renderers[i].bounds);
            }

            var col2D = root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < col2D.Length; i++)
            {
                if (!has)
                {
                    bounds = col2D[i].bounds;
                    has = true;
                }
                else bounds.Encapsulate(col2D[i].bounds);
            }

            var col3D = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < col3D.Length; i++)
            {
                if (!has)
                {
                    bounds = col3D[i].bounds;
                    has = true;
                }
                else bounds.Encapsulate(col3D[i].bounds);
            }

            return has;
        }

        public void ClearSpawnedObjects()
        {
            if (spawnedObjectsParent == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = spawnedObjectsParent.childCount - 1; i >= 0; i--)
                    DestroyImmediate(spawnedObjectsParent.GetChild(i).gameObject);
            }
            else
            {
                for (int i = spawnedObjectsParent.childCount - 1; i >= 0; i--)
                    Destroy(spawnedObjectsParent.GetChild(i).gameObject);
            }
#else
    for (int i = spawnedObjectsParent.childCount - 1; i >= 0; i--)
        Destroy(spawnedObjectsParent.GetChild(i).gameObject);
#endif

            resourcesSpawned = false;
            enemiesSpawned = false;
        }

        bool UsesTilemap()
        {
            return groundTilemap != null && biome != null && biome.groundTile != null;
        }

        private void SpawnResourcesImmediate()
        {
            if (biome == null || biome.resourceSpawns == null) return;

            EnsureSpawnParent();

            foreach (var entry in biome.resourceSpawns)
            {
                if (entry.prefab == null) continue;

                int min = Mathf.Max(0, entry.minSpawn);
                int max = Mathf.Max(min, entry.maxSpawn);
                int spawnCount = rng.Next(min, max + 1);

                for (int i = 0; i < spawnCount; i++)
                {
                    Vector3 pos = GetRandomPosInChunk();
                    Instantiate(entry.prefab, pos, Quaternion.identity, spawnedObjectsParent);
                }
            }
        }

        private void SpawnEnemiesImmediate()
        {
            if (biome == null || biome.creatureSpawns == null) return;

            EnsureSpawnParent();

            foreach (var entry in biome.creatureSpawns)
            {
                if (entry.prefab == null) continue;

                int min = Mathf.Max(0, entry.minSpawn);
                int max = Mathf.Max(min, entry.maxSpawn);
                int spawnCount = rng.Next(min, max + 1);

                for (int i = 0; i < spawnCount; i++)
                    Instantiate(entry.prefab, GetRandomPosInChunk(), Quaternion.identity, spawnedObjectsParent);
            }
        }

        private Vector3 GetRandomPosInChunk()
        {
            float x = (float)rng.NextDouble() * CHUNK_WIDTH;
            float y = (float)rng.NextDouble() * CHUNK_HEIGHT;
            return transform.position + new Vector3(x, y, 0f);
        }

        public Vector2 GetWorldSize()
        {
            return new Vector2(CHUNK_WIDTH, CHUNK_HEIGHT);
        }

        private void EnsureSpawnParent()
        {
            if (spawnedObjectsParent == null)
            {
                GameObject go = new GameObject("SpawnedObjects");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                spawnedObjectsParent = go.transform;
            }
        }
    }
}
