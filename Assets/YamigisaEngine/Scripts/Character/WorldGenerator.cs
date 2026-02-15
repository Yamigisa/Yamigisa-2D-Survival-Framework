using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class WorldGenerator : MonoBehaviour, ISavable
    {
        [Header("Bools")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool generateProcedurally = true;

        [Header("Prefabs")]
        [SerializeField] private WorldChunk chunkPrefab;

        [Header("Biome Zones")]
        [SerializeField] private List<BiomeZone> biomeZones;

        [Header("Streaming")]
        [SerializeField] private int loadRadius = 1;

        [Header("Unified Chunk World Size (FOR BOTH PREFAB & TILEMAP)")]
        [SerializeField] private Vector2 fixedChunkWorldSize = new Vector2(25f, 14.4f);

        private readonly Dictionary<Vector2Int, WorldChunk> chunkMap = new();

        private Vector2Int lastPlayerChunk;

        public void Setup()
        {
            if (!generateOnStart)
                return;

            ClearWorld();

            if (Character.instance == null)
                return;

            lastPlayerChunk = WorldToChunkCoord(Character.instance.transform.position);
            SpawnAround(lastPlayerChunk);
        }

        private void Update()
        {
            if (!generateProcedurally)
                return;

            if (Character.instance == null)
                return;

            Vector2Int current = WorldToChunkCoord(Character.instance.transform.position);

            if (current == lastPlayerChunk)
                return;

            lastPlayerChunk = current;
            SpawnAround(current);
        }

        private void SpawnAround(Vector2Int center)
        {
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int y = -loadRadius; y <= loadRadius; y++)
                {
                    SpawnChunkAt(center + new Vector2Int(x, y));
                }
            }
        }

        private void SpawnChunkAt(Vector2Int coord)
        {
            if (chunkMap.ContainsKey(coord))
                return;

            Vector3 worldPos = new Vector3(
                coord.x * fixedChunkWorldSize.x,
                coord.y * fixedChunkWorldSize.y,
                0f
            );

            BiomeGroup group = GetBiomeGroupByChunkCoord(coord);
            if (group == null || group.biomes == null || group.biomes.Count == 0)
                return;

            BiomeData biome = group.biomes[Random.Range(0, group.biomes.Count)];
            int seed = Random.Range(int.MinValue, int.MaxValue);

            WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity);

            chunk.Initialize(biome, chunk.size, seed);
            chunk.ForceImmediateGeneration();

            chunkMap.Add(coord, chunk);
        }

        private Vector2Int WorldToChunkCoord(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / fixedChunkWorldSize.x);
            int y = Mathf.FloorToInt(worldPos.y / fixedChunkWorldSize.y);

            return new Vector2Int(x, y);
        }

        private BiomeGroup GetBiomeGroupByChunkCoord(Vector2Int coord)
        {
            if (biomeZones == null || biomeZones.Count == 0)
                return null;

            int distance = Mathf.Max(Mathf.Abs(coord.x), Mathf.Abs(coord.y));

            int zoneSize = 5; // ← distance band size (change this)

            int zoneIndex = distance / zoneSize;

            int groupIndex = zoneIndex % biomeZones.Count;

            return biomeZones[groupIndex].biomeGroup;
        }

        public void Save(ref SaveGameData data)
        {
            if (data.chunks == null)
                data.chunks = new List<ChunkSaveData>();
            else
                data.chunks.Clear();

            foreach (var kv in chunkMap)
            {
                WorldChunk c = kv.Value;
                if (c == null)
                    continue;

                ChunkSaveData chunkData = new ChunkSaveData
                {
                    position = c.transform.position,
                    biomeKey = c.biome != null ? c.biome.name : "",
                    size = c.size,
                    seed = c.seed,
                    resourcesSpawned = c.resourcesSpawned,
                    enemiesSpawned = c.enemiesSpawned,
                    interactiveObjects = new List<InteractiveObjectSaveData>()
                };

                data.chunks.Add(chunkData);
            }
        }

        public void Load(SaveGameData data)
        {
            if (data.chunks == null || data.chunks.Count == 0)
                return;

            ClearWorld();

            foreach (ChunkSaveData saved in data.chunks)
            {
                BiomeData biome = GetBiomeByKey(saved.biomeKey);
                if (biome == null)
                    continue;

                Vector2Int coord = WorldToChunkCoord(saved.position);

                Vector3 worldPos = new Vector3(
                    coord.x * fixedChunkWorldSize.x,
                    coord.y * fixedChunkWorldSize.y,
                    0f
                );

                WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity);
                chunk.Initialize(biome, saved.size, saved.seed);
                chunk.ForceImmediateGeneration();

                chunkMap.Add(coord, chunk);
            }

            if (Character.instance != null)
            {
                lastPlayerChunk = WorldToChunkCoord(Character.instance.transform.position);
                SpawnAround(lastPlayerChunk);
            }
        }

        private void ClearWorld()
        {
            foreach (var kv in chunkMap)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }

            chunkMap.Clear();
        }

        private BiomeData GetBiomeByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            foreach (var zone in biomeZones)
            {
                if (zone.biomeGroup == null)
                    continue;

                foreach (var biome in zone.biomeGroup.biomes)
                {
                    if (biome != null && biome.name == key)
                        return biome;
                }
            }

            return null;
        }

        [System.Serializable]
        public class BiomeZone
        {
            public string zoneName;
            public int minDistance;
            public BiomeGroup biomeGroup;
        }
    }
}
