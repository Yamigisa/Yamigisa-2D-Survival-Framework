using System.Collections.Generic;

namespace Yamigisa
{
    [System.Serializable]
    public class StorageSaveData
    {
        public string storageId;
        public List<InventoryItemSaveData> items = new();
    }
}