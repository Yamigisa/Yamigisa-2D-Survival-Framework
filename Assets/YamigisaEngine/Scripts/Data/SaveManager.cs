using UnityEngine;

namespace Yamigisa
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private bool saveGameOnQuit = true;
        private string path => Application.persistentDataPath + "/save.json";

        public void SaveGame()
        {
            SaveGameData data = new SaveGameData();

            foreach (var savable in FindObjectsOfType<MonoBehaviour>(true))
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
            if (!System.IO.File.Exists(path)) return;

            string json = System.IO.File.ReadAllText(path);
            SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);

            foreach (var savable in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (savable is ISavable s)
                {
                    s.Load(data);
                }
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
        }
    }
}
