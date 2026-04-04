#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Yamigisa
{

    [CustomEditor(typeof(SaveManager))]
    public class SaveManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);
            GUILayout.Label("Save Controls", EditorStyles.boldLabel);

            SaveManager saveManager = (SaveManager)target;

            GUILayout.Space(5);

            if (GUILayout.Button("Save Game"))
            {
                if (Application.isPlaying)
                    saveManager.SaveGame();
                else
                    Debug.LogWarning("Enter Play Mode to save.");
            }

            if (GUILayout.Button("Load Game"))
            {
                saveManager.LoadGame();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Delete Save"))
            {
                string path = Application.persistentDataPath + "/save.json";

                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log("Save file deleted: " + path);
                }
                else
                {
                    Debug.LogWarning("No save file found to delete.");
                }
            }
        }
    }
}
#endif