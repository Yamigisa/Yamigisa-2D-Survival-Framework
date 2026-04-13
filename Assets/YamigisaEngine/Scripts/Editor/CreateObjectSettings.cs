using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(
        fileName = "CreateObjectSettings",
        menuName = "Yamigisa/Create Object Settings",
        order = 100)]
    public class CreateObjectSettings : ScriptableObject
    {
        [Header("Base Save Folders")]
        [Tooltip("Base folder for all generated prefabs")]
        public string prefabFolder = "Assets/YamigisaEngine/Prefabs";

        [Tooltip("Base folder for all generated ScriptableObjects / resources")]
        public string resourceFolder = "Assets/YamigisaEngine/Resources";

        public LayerMask interactiveObjectLayer;

        private void OnEnable()
        {
            if (interactiveObjectLayer.value == 0)
            {
                interactiveObjectLayer = LayerMask.GetMask("Interactive");
            }
        }

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