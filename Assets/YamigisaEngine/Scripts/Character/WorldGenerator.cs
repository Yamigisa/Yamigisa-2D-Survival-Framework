using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class WorldGenerator : MonoBehaviour, ISavable
    {
        [Header("Prefabs")]
        [SerializeField] private WorldChunk chunkPrefab;

        [Header("Biomes")]
        [SerializeField] private List<BiomeData> availableBiomes;

        [Header("World Size (IN CHUNKS)")]
        [SerializeField] private int worldWidth = 3;
        [SerializeField] private int worldHeight = 3;

        [Header("Chunk Settings")]
        [SerializeField] private int chunkSize = 16;

        private readonly List<WorldChunk> spawnedChunks = new();

        // =========================
        // SETUP (NEW GAME ONLY)
        // =========================
        public void Setup()
        {
            ClearWorld();
            GenerateWorldGrid();
        }

        private void GenerateWorldGrid()
        {
            Vector3 playerPos = Character.instance.transform.position;

            int halfW = worldWidth / 2;
            int halfH = worldHeight / 2;

            for (int x = -halfW; x <= halfW; x++)
            {
                for (int y = -halfH; y <= halfH; y++)
                {
                    Vector3 chunkPos = playerPos + new Vector3(
                        x * chunkSize,
                        y * chunkSize,
                        0f
                    );

                    BiomeData biome = availableBiomes[Random.Range(0, availableBiomes.Count)];
                    int seed = Random.Range(int.MinValue, int.MaxValue);

                    WorldChunk chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity);
                    chunk.Initialize(
                        biome,
                        chunkSize,
                        chunk.resourceCount,
                        chunk.enemyCount,
                        seed
                    );

                    spawnedChunks.Add(chunk);
                }
            }
        }

        // =========================
        // SAVE
        // =========================
        public void Save(ref SaveGameData data)
        {
            if (data.chunks == null)
                data.chunks = new List<ChunkSaveData>();
            else
                data.chunks.Clear();

            spawnedChunks.RemoveAll(c => c == null);

            foreach (WorldChunk c in spawnedChunks)
            {
                ChunkSaveData chunkData = new ChunkSaveData
                {
                    position = c.transform.position,
                    biomeKey = c.biome != null ? c.biome.name : "",
                    size = c.size,
                    resourceCount = c.resourceCount,
                    enemyCount = c.enemyCount,
                    seed = c.seed,
                    resourcesSpawned = c.resourcesSpawned,
                    enemiesSpawned = c.enemiesSpawned,
                    interactiveObjects = new List<InteractiveObjectSaveData>()


                };

                foreach (InteractiveObject io in c.GetComponentsInChildren<InteractiveObject>(true))
                {
                    io.SaveToList(chunkData.interactiveObjects);
                }
            }
        }

        // =========================
        // LOAD (NO SETUP HERE)
        // =========================
        public void Load(SaveGameData data)
        {
            if (data.chunks == null || data.chunks.Count == 0)
                return;

            ClearWorld();

            foreach (ChunkSaveData saved in data.chunks)
            {
                BiomeData biome = GetBiomeByKey(saved.biomeKey);
                if (biome == null) continue;

                WorldChunk chunk = Instantiate(
                    chunkPrefab,
                    saved.position,
                    Quaternion.identity
                );

                chunk.resourcesSpawned = saved.resourcesSpawned;
                chunk.enemiesSpawned = saved.enemiesSpawned;

                chunk.Initialize(
    biome,
    saved.size,
    saved.resourceCount,
    saved.enemyCount,
    saved.seed,
    saved.resourcesSpawned,
    saved.enemiesSpawned
);

                spawnedChunks.Add(chunk);

                // 🔽 RESTORE INTERACTIVE OBJECTS AFTER CHUNK IS READY
                if (saved.interactiveObjects != null && saved.interactiveObjects.Count > 0)
                {
                    chunk.RestoreInteractiveObjects(saved.interactiveObjects);
                }
            }
        }

        // =========================
        // HELPERS
        // =========================
        private void ClearWorld()
        {
            for (int i = spawnedChunks.Count - 1; i >= 0; i--)
            {
                if (spawnedChunks[i] != null)
                    Destroy(spawnedChunks[i].gameObject);
            }
            spawnedChunks.Clear();
        }

        private BiomeData GetBiomeByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            for (int i = 0; i < availableBiomes.Count; i++)
            {
                if (availableBiomes[i] != null && availableBiomes[i].name == key)
                    return availableBiomes[i];
            }
            return null;
        }
    }
}
