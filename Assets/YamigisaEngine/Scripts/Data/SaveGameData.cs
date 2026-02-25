using System.Collections.Generic;

namespace Yamigisa
{

    [System.Serializable]
    public class SaveGameData
    {
        [System.NonSerialized] public SaveManager saveManager;
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
        public List<StorageSaveData> storages = new();
        public List<EquipmentSaveData> equippedItems = new();
    }
}