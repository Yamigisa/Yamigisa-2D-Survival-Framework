using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [System.Serializable]
    public class ChunkSaveData
    {
        public Vector2Int coord;
        public Vector3 position;
        public string biomeKey;
        public int size;
        public int resourceCount;
        public int enemyCount;
        public int seed;

        public bool resourcesSpawned;
        public bool enemiesSpawned;

        public List<InteractiveObjectSaveData> interactiveObjects;
    }

}