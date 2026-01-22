using UnityEngine;

namespace Yamigisa
{
    /// <summary>
    /// Default settings used by CreateObject editor tool
    /// </summary>
    [CreateAssetMenu(
        fileName = "CreateObjectSettings",
        menuName = "Yamigisa/Create Object Settings",
        order = 100)]
    public class CreateObjectSettings : ScriptableObject
    {
        [Header("Save Folders")]
        [Tooltip("Base folder for prefabs")]
        public string prefabFolder = "Assets/YamigisaEngine/Prefabs";

        [Tooltip("Folder for item prefabs")]
        public string prefabItemsFolder = "Assets/YamigisaEngine/Prefabs/Items";

        [Tooltip("Folder for item ScriptableObjects")]
        public string itemsFolder = "Assets/YamigisaEngine/Resources/Items";

        [Header("Animal Folders")]
        public string prefabAnimalsFolder = "Assets/YamigisaEngine/Prefabs/Animals";
        public string animalDataFolder = "Assets/YamigisaEngine/Resources/Animals";
        // -------------------------------------------------------

        [Header("Default ItemData Actions")]
        [Tooltip("Default actions assigned to ItemData (ScriptableObject)")]
        public ActionBase[] defaultItemActions;

        [Header("Default Interactive Object Actions")]
        [Tooltip("Default actions assigned to the interactive object prefab (ScriptableObject)")]
        public ActionBase[] defaultInteractiveObjectActions;

        [Header("Default Animal Interactive Actions")]
        public ActionBase[] defaultAnimalActions;

        [Header("Default Placeable Actions")]
        public PlaceableDefaultActions[] defaultPlaceableActions;

    }

    [System.Serializable]
    public class PlaceableDefaultActions
    {
        public PlaceableType type;
        public ActionBase[] actions;
    }
}
