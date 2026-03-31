using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(
        fileName = "CreateObjectSettings",
        menuName = "Yamigisa/Create Object Settings",
        order = 100)]
    public class CreateObjectSettings : ScriptableObject
    {
        [Header("Save Folders")]
        public string prefabFolder = "Assets/YamigisaEngine/Prefabs";
        public string prefabItemsFolder = "Assets/YamigisaEngine/Prefabs/Items";
        public string itemsFolder = "Assets/YamigisaEngine/Resources/Items";

        [Header("Animal Folders")]
        public string prefabAnimalsFolder = "Assets/YamigisaEngine/Prefabs/Animals";
        public string animalDataFolder = "Assets/YamigisaEngine/Resources/Animals";

        [Header("Layer Settings")]
        [Tooltip("All Interactive Objects will use this layer")]
        public LayerMask interactiveObjectLayer;

        [Header("Default ItemData Actions (All Items)")]
        public ActionBase[] defaultItemActions;

        [Header("Default Consumable Actions")]
        public ActionBase[] defaultConsumableActions;

        [Header("Default Equipment Actions")]
        public ActionBase[] defaultEquipmentActions;

        [Header("Default Interactive Object Actions")]
        public ActionBase[] defaultInteractiveObjectActions;

        [Header("Default Destroyable Interactive Actions")]
        public ActionBase[] defaultDestroyableActions;

        [Header("Default Animal Interactive Actions")]
        public ActionBase[] defaultAnimalActions;

        [Header("Global Placeable ItemData Actions")]
        [Tooltip("These actions will be injected into ItemData when creating ANY Placeable.")]
        public ActionBase[] globalPlaceableItemDataActions;

        [Header("Specific Placeable Actions")]
        public PlaceableDefaultActions[] defaultPlaceableActions;
    }

    [System.Serializable]
    public class PlaceableDefaultActions
    {
        public PlaceableType type;
        public ActionBase[] actions;
    }
}