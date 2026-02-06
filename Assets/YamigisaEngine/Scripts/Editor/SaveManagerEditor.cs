#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Yamigisa;

[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        SaveManager saveManager = (SaveManager)target;

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
#endif
