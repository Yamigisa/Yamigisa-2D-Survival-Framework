using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] private int editorExpansionRadius = 1;

        [Header("Unified Chunk World Size (FOR BOTH PREFAB & TILEMAP)")]
        [SerializeField] private Vector2 fixedChunkWorldSize = new Vector2(25f, 14.4f);

        private readonly Dictionary<Vector2Int, WorldChunk> chunkMap = new();

        private Vector2Int lastPlayerChunk;

        private bool isLoadingFromSave = false;
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
            if (isLoadingFromSave)
                return;

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

        private void SpawnAround(Vector2Int center, bool spawnObjects = true)
        {
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int y = -loadRadius; y <= loadRadius; y++)
                {
                    SpawnChunkAt(center + new Vector2Int(x, y), spawnObjects);
                }
            }
        }

        private void SpawnChunkAt(Vector2Int coord, bool spawnObjects = true)
        {
            if (chunkMap.ContainsKey(coord))
                return;

            Vector3 worldPos = ChunkToWorldPos(coord);

            BiomeGroup group = GetBiomeGroupByChunkCoord(coord);
            if (group == null || group.biomes == null || group.biomes.Count == 0)
                return;

            BiomeData biome = group.biomes[Random.Range(0, group.biomes.Count)];
            int seed = Random.Range(int.MinValue, int.MaxValue);

            WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);

            // keep your original initialize call intact
            chunk.Initialize(biome, chunk.size, seed, spawnObjects);

            if (!chunkMap.ContainsKey(coord))
                chunkMap.Add(coord, chunk);
        }

        private Vector2 GetActualChunkWorldSize()
        {
            if (chunkPrefab != null)
                return chunkPrefab.GetWorldSize();

            return fixedChunkWorldSize;
        }

        private Vector2Int WorldToChunkCoord(Vector3 worldPos)
        {
            Vector2 chunkWorldSize = GetActualChunkWorldSize();

            int x = Mathf.RoundToInt(worldPos.x / chunkWorldSize.x);
            int y = Mathf.RoundToInt(worldPos.y / chunkWorldSize.y);

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
            if (!data.saveManager.SaveChunks)
                return;

            if (data.chunks == null)
                data.chunks = new List<ChunkSaveData>();
            else
                data.chunks.Clear();

            foreach (var kv in chunkMap)
            {
                Vector2Int coord = kv.Key;
                WorldChunk c = kv.Value;
                if (c == null) continue;

                data.chunks.Add(new ChunkSaveData
                {
                    coord = coord, // ✅ ALWAYS use this
                    position = c.transform.position, // optional legacy
                    biomeKey = c.biome != null ? c.biome.name : "",
                    size = c.size,
                    seed = c.seed,
                    resourcesSpawned = c.resourcesSpawned,
                    enemiesSpawned = c.enemiesSpawned,
                    interactiveObjects = new List<InteractiveObjectSaveData>()
                });
            }
        }

        public void Load(SaveGameData data)
        {
            isLoadingFromSave = true;

            ClearWorld();

            if (data.chunks != null && data.chunks.Count > 0)
            {
                foreach (ChunkSaveData saved in data.chunks)
                {
                    BiomeData biome = GetBiomeByKey(saved.biomeKey);
                    if (biome == null) continue;

                    // ✅ Backward compatible: if coord not present (old saves), derive once using SAFE rounding
                    Vector2Int coord = saved.coord != default ? saved.coord : WorldToChunkCoordSafe(saved.position);

                    Vector3 worldPos = ChunkToWorldPos(coord);

                    WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
                    chunk.Initialize(biome, saved.size, saved.seed, false);
                    chunk.SetSpawnFlags(saved.resourcesSpawned, saved.enemiesSpawned);

                    if (saved.resourcesSpawned || saved.enemiesSpawned)
                        chunk.GenerateObjectsOnlyImmediate();

                    if (!chunkMap.ContainsKey(coord))
                        chunkMap.Add(coord, chunk);
                }
            }

            if (Character.instance != null)
                lastPlayerChunk = WorldToChunkCoord(Character.instance.transform.position);

            isLoadingFromSave = false;
        }

        private Vector3 ChunkToWorldPos(Vector2Int coord)
        {
            Vector2 chunkWorldSize = GetActualChunkWorldSize();

            return new Vector3(
                coord.x * chunkWorldSize.x,
                coord.y * chunkWorldSize.y,
                0f
            );
        }

        // ✅ SAFE conversion for legacy saves only (no FloorToInt)
        private Vector2Int WorldToChunkCoordSafe(Vector3 worldPos)
        {
            Vector2 chunkWorldSize = GetActualChunkWorldSize();

            float fx = worldPos.x / chunkWorldSize.x;
            float fy = worldPos.y / chunkWorldSize.y;

            int x = Mathf.RoundToInt(fx);
            int y = Mathf.RoundToInt(fy);

            return new Vector2Int(x, y);
        }

        private void ClearWorld()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
            else
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
#else
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
        Destroy(transform.GetChild(i).gameObject);
    }
#endif

            editorExpansionRadius = loadRadius;
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


        // Editor
        public void EditorCreateWorld()
        {
            Vector2Int center = Vector2Int.zero;

            if (Character.instance != null)
                center = WorldToChunkCoord(Character.instance.transform.position);

            // FIRST CLICK → spawn full square
            if (chunkMap.Count == 0)
            {
                SpawnAround(center, false);
                editorExpansionRadius = loadRadius + 1;
                return;
            }

            // NEXT CLICKS → spawn only outer ring
            SpawnRing(center, editorExpansionRadius);
            editorExpansionRadius++;
        }

        private void SpawnRing(Vector2Int center, int radius)
        {
            // Spawn only the OUTER RING
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    bool isBorder =
                        x == -radius ||
                        x == radius ||
                        y == -radius ||
                        y == radius;

                    if (!isBorder)
                        continue;

                    SpawnChunkAt(center + new Vector2Int(x, y), false);
                }
            }
        }

        public void EditorDeleteWorld()
        {
            ClearWorld();
        }

        public void EditorCreateObjects()
        {
            foreach (var kv in chunkMap)
            {
                if (kv.Value != null)
                    kv.Value.GenerateObjectsOnlyImmediate();
            }
        }

        public void EditorDeleteObjects()
        {
            foreach (var kv in chunkMap)
            {
                if (kv.Value != null)
                    kv.Value.ClearSpawnedObjects();
            }
        }

        public void EditorRefreshWorld()
        {
            Vector2Int center = Vector2Int.zero;

            if (Character.instance != null)
                center = WorldToChunkCoord(Character.instance.transform.position);

            // Store current expansion radius BEFORE clearing
            int currentRadius = editorExpansionRadius - 1;

            ClearWorld();

            // Rebuild full square using current radius
            for (int x = -currentRadius; x <= currentRadius; x++)
            {
                for (int y = -currentRadius; y <= currentRadius; y++)
                {
                    SpawnChunkAt(center + new Vector2Int(x, y), false);
                }
            }

            // Restore expansion state
            editorExpansionRadius = currentRadius + 1;
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
