using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class WorldGenerator : MonoBehaviour, ISavable
    {
        [Header("Prefabs")]
        [SerializeField] private WorldChunk chunkPrefab;

        [Header("Biome Zones")]
        [SerializeField] private List<BiomeZone> biomeZones;

        [Header("Streaming")]
        [SerializeField] private int loadRadius = 1;

        [Header("Chunk Size Source")]
        [SerializeField] private bool autoDetectChunkSize = true;

        [Header("Chunk Size (Default 25 x 14.4)")]
        [SerializeField] private Vector2 manualChunkWorldSize = new Vector2(25f, 14.4f);

        private readonly Dictionary<Vector2Int, WorldChunk> chunkMap = new();

        private Vector2 chunkWorldSize;
        private Vector2Int lastPlayerChunk;

        private bool isTilemapWorld;

        public void Setup()
        {
            ClearWorld();

            if (Character.instance == null)
                return;

            isTilemapWorld = DetermineIsTilemapWorld();

            if (isTilemapWorld && autoDetectChunkSize)
                chunkWorldSize = DetectChunkWorldSize();   // tilemap sizing (accurate)
            else
                chunkWorldSize = manualChunkWorldSize;     // prefab sizing (manual)

            lastPlayerChunk = WorldToChunkCoord(Character.instance.transform.position);
            SpawnAround(lastPlayerChunk);
        }

        private void Update()
        {
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

            Vector3 worldPos = ChunkCoordToWorld(coord);

            BiomeGroup group = GetBiomeGroupByChunkCoord(coord);
            if (group == null || group.biomes == null || group.biomes.Count == 0)
                return;

            BiomeData biome = group.biomes[Random.Range(0, group.biomes.Count)];
            int seed = Random.Range(int.MinValue, int.MaxValue);

            WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity);

            // Keep your existing init signature usage:
            // For tilemap world, size is tiles-per-side used by WorldChunk tile filling.
            // For prefab world, size can be ignored or used for spawn logic.
            chunk.Initialize(biome, chunk.size, seed);

            // Instant generation (no slow tile spawning)
            chunk.ForceImmediateGeneration();

            chunkMap.Add(coord, chunk);
        }

        private Vector2Int WorldToChunkCoord(Vector3 worldPos)
        {
            // Absolute grid. No origin snapping.
            // This is stable for both tilemap/prefab as long as chunkWorldSize matches real world chunk dimensions.
            int cx = Mathf.FloorToInt(worldPos.x / chunkWorldSize.x);
            int cy = Mathf.FloorToInt(worldPos.y / chunkWorldSize.y);
            return new Vector2Int(cx, cy);
        }

        private Vector3 ChunkCoordToWorld(Vector2Int coord)
        {
            return new Vector3(
                coord.x * chunkWorldSize.x,
                coord.y * chunkWorldSize.y,
                0f
            );
        }

        private Vector3 SnapOriginToGrid(Vector3 worldPos)
        {
            // Kept (NOT used). Origin snapping caused drift/gaps before.
            return Vector3.zero;
        }

        private Vector2 DetectChunkWorldSize()
        {
            // Kept method name/signature, but now it is tilemap-accurate:
            // Build a temp chunk instantly and read the Tilemap's real world bounds size.
            // This avoids renderer/collider padding issues.
            BiomeData sampleBiome = GetAnyTilemapBiome();
            if (sampleBiome == null)
                return manualChunkWorldSize;

            WorldChunk temp = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
            temp.gameObject.hideFlags = HideFlags.HideAndDontSave;

            temp.Initialize(sampleBiome, temp.size, 0);
            temp.ForceImmediateGeneration();

            Vector2 size = manualChunkWorldSize;

            Tilemap tm = temp.groundTilemap != null ? temp.groundTilemap : temp.GetComponentInChildren<Tilemap>(true);
            if (tm != null)
            {
                tm.CompressBounds();
                var lb = tm.localBounds;
                Vector3 world = tm.transform.TransformVector(lb.size);
                size = new Vector2(Mathf.Abs(world.x), Mathf.Abs(world.y));
            }

            Destroy(temp.gameObject);

            // Safety clamp
            if (size.x <= 0.0001f || size.y <= 0.0001f)
                size = manualChunkWorldSize;

            return size;
        }

        private BiomeGroup GetBiomeGroupByChunkCoord(Vector2Int coord)
        {
            int distance = Mathf.Max(Mathf.Abs(coord.x), Mathf.Abs(coord.y));

            BiomeZone selected = null;

            for (int i = 0; i < biomeZones.Count; i++)
            {
                var z = biomeZones[i];
                if (z == null || z.biomeGroup == null)
                    continue;

                if (distance >= z.minDistance)
                {
                    if (selected == null || z.minDistance > selected.minDistance)
                        selected = z;
                }
            }

            if (selected == null && biomeZones.Count > 0)
                return biomeZones[0].biomeGroup;

            return selected != null ? selected.biomeGroup : null;
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

                foreach (InteractiveObject io in c.GetComponentsInChildren<InteractiveObject>(true))
                    io.SaveToList(chunkData.interactiveObjects);

                data.chunks.Add(chunkData);
            }
        }

        public void Load(SaveGameData data)
        {
            if (data.chunks == null || data.chunks.Count == 0)
                return;

            ClearWorld();

            isTilemapWorld = DetermineIsTilemapWorld();

            if (isTilemapWorld && autoDetectChunkSize)
                chunkWorldSize = DetectChunkWorldSize();
            else
                chunkWorldSize = manualChunkWorldSize;

            foreach (ChunkSaveData saved in data.chunks)
            {
                BiomeData biome = GetBiomeByKey(saved.biomeKey);
                if (biome == null)
                    continue;

                Vector2Int coord = WorldToChunkCoord(saved.position);
                Vector3 worldPos = ChunkCoordToWorld(coord);

                WorldChunk chunk = Instantiate(chunkPrefab, worldPos, Quaternion.identity);
                chunk.Initialize(biome, saved.size, saved.seed, saved.resourcesSpawned, saved.enemiesSpawned);
                chunk.ForceImmediateGeneration();

                chunkMap.Add(coord, chunk);

                if (saved.interactiveObjects != null && saved.interactiveObjects.Count > 0)
                    chunk.RestoreInteractiveObjects(saved.interactiveObjects);
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
                if (zone == null || zone.biomeGroup == null || zone.biomeGroup.biomes == null)
                    continue;

                foreach (var biome in zone.biomeGroup.biomes)
                {
                    if (biome != null && biome.name == key)
                        return biome;
                }
            }

            return null;
        }

        private bool DetermineIsTilemapWorld()
        {
            // If ANY biome has groundTile AND chunkPrefab has a Tilemap, treat as tilemap world.
            if (chunkPrefab == null)
                return false;

            Tilemap tm = chunkPrefab.groundTilemap != null ? chunkPrefab.groundTilemap : chunkPrefab.GetComponentInChildren<Tilemap>(true);
            if (tm == null)
                return false;

            for (int i = 0; i < biomeZones.Count; i++)
            {
                var z = biomeZones[i];
                if (z == null || z.biomeGroup == null || z.biomeGroup.biomes == null)
                    continue;

                foreach (var b in z.biomeGroup.biomes)
                {
                    if (b != null && b.groundTile != null)
                        return true;
                }
            }

            return false;
        }

        private BiomeData GetAnyTilemapBiome()
        {
            for (int i = 0; i < biomeZones.Count; i++)
            {
                var z = biomeZones[i];
                if (z == null || z.biomeGroup == null || z.biomeGroup.biomes == null)
                    continue;

                foreach (var b in z.biomeGroup.biomes)
                {
                    if (b != null && b.groundTile != null)
                        return b;
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
