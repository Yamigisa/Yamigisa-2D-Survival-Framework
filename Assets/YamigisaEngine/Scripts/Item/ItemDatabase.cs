using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> items;
        private static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (initialized) return;

            items = new Dictionary<string, ItemData>();
            ItemData[] loaded = Resources.LoadAll<ItemData>("Items");

            foreach (var item in loaded)
                items[item.Id] = item;

            initialized = true;
        }

        public static ItemData Get(string id)
        {
            if (!initialized)
            {
                Debug.LogError("ItemDatabase not initialized!");
                return null;
            }

            items.TryGetValue(id, out var item);
            return item;
        }
    }
}
