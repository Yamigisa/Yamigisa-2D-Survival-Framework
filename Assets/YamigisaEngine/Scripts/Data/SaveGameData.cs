using System.Collections.Generic;

namespace Yamigisa
{

    [System.Serializable]
    public class SaveGameData
    {
        // World
        public int day;
        public int hour;
        public int minute;

        // Player
        public CharacterData player;

        // Inventory
        public InventorySaveData inventory;
        public Dictionary<string, DestroyableSaveData> destroyables;
        public List<InteractiveObjectSaveData> interactiveObjects = new();
        public List<ChunkSaveData> chunks = new();
    }
}