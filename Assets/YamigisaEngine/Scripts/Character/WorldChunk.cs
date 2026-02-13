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

        private Bounds biomeBounds;

        public void Initialize(BiomeData biome, int size, int seed)
        {
            this.biome = biome;
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
            this.seed = seed;

            this.resourcesSpawned = resourcesSpawned;
            this.enemiesSpawned = enemiesSpawned;

            rng = new System.Random(seed);

            StopAllCoroutines();
            StartCoroutine(BuildChunk());
        }

        private IEnumerator BuildChunk()
        {
            GenerateAllImmediately();
            yield break;
        }

        private void GenerateAllImmediately()
        {
            if (biome == null) return;

            usedSpawnCells.Clear();

            // keep Grid cell size (1,1,1) but allow the chunk to represent 14.4 world height
            transform.localScale = new Vector3(1f, Y_SCALE, 1f);

            ClearPreviousChunkContent();

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
            // destroy previous instantiated children but keep tilemap objects if they exist
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                // keep tilemap object (it should already be on the chunk)
                if (groundTilemap != null && child == groundTilemap.transform) continue;

                DestroyImmediate(child.gameObject);
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

        bool UsesTilemap()
        {
            return groundTilemap != null && biome != null && biome.groundTile != null;
        }

        private void SpawnResourcesImmediate()
        {
            if (biome == null || biome.resourceSpawns == null) return;

            foreach (var entry in biome.resourceSpawns)
            {
                if (entry.prefab == null) continue;

                int min = Mathf.Max(0, entry.minSpawn);
                int max = Mathf.Max(min, entry.maxSpawn);
                int spawnCount = rng.Next(min, max + 1);

                for (int i = 0; i < spawnCount; i++)
                    Instantiate(entry.prefab, GetRandomPosInChunk(), Quaternion.identity, transform);
            }
        }

        private void SpawnEnemiesImmediate()
        {
            if (biome == null || biome.creatureSpawns == null) return;

            foreach (var entry in biome.creatureSpawns)
            {
                if (entry.prefab == null) continue;

                int min = Mathf.Max(0, entry.minSpawn);
                int max = Mathf.Max(min, entry.maxSpawn);
                int spawnCount = rng.Next(min, max + 1);

                for (int i = 0; i < spawnCount; i++)
                    Instantiate(entry.prefab, GetRandomPosInChunk(), Quaternion.identity, transform);
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
    }
}
