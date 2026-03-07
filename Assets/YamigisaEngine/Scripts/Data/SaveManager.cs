using UnityEngine;

namespace Yamigisa
{
    public class SaveManager : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private bool saveGameOnQuit = true;

        [Header("Auto Save")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // seconds
        private float autoSaveTimer;

        [Header("Save Toggles")]
        [SerializeField] private bool saveWorldTime = true;
        [SerializeField] private bool savePlayer = true;
        [SerializeField] private bool saveInventory = true;
        [SerializeField] private bool saveDestroyables = true;
        [SerializeField] private bool saveInteractiveObjects = true;
        [SerializeField] private bool saveChunks = true;
        [SerializeField] private bool saveStorages = true;
        [SerializeField] private bool saveEquipment = true;

        public bool SaveWorldTime => saveWorldTime;
        public bool SavePlayer => savePlayer;
        public bool SaveInventory => saveInventory;
        public bool SaveDestroyables => saveDestroyables;
        public bool SaveInteractiveObjects => saveInteractiveObjects;
        public bool SaveChunks => saveChunks;
        public bool SaveStorages => saveStorages;
        public bool SaveEquipment => saveEquipment;

        private string path => Application.persistentDataPath + "/save.json";

        public void SaveGame()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Cannot save game outside Play Mode.");
                return;
            }

            SaveGameData data = new SaveGameData();
            data.saveManager = this;

            foreach (var savable in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (savable is ISavable s)
                    s.Save(ref data);
            }

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);

            Debug.Log("Save game");
        }

        public void LoadGame()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Cannot load game outside Play Mode.");
                return;
            }

            if (!System.IO.File.Exists(path)) return;

            string json = System.IO.File.ReadAllText(path);
            SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);

            foreach (var savable in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                if (savable is ISavable s)
                    s.Load(data);
            }

            GameManager.instance.OnGameStart();
            Debug.Log("Load game");
        }

        private void OnApplicationQuit()
        {
            if (!saveGameOnQuit) return;

            SaveGame();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                SaveGame();

            if (Input.GetKeyDown(KeyCode.F9))
                LoadGame();

            // Auto Save
            if (enableAutoSave)
            {
                autoSaveTimer += Time.deltaTime;

                if (autoSaveTimer >= autoSaveInterval)
                {
                    SaveGame();
                    autoSaveTimer = 0f;
                }
            }
        }
    }
}