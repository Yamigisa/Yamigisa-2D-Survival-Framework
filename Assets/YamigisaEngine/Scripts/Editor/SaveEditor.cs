#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class SaveEditor
{
    private static string SavePath =>
        Application.persistentDataPath + "/save.json";

    // =============================
    // DELETE SAVE MENU ITEM
    // =============================

    [MenuItem("Yamigisa Engine/Delete Save", false, 10000)]
    private static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save file deleted: " + SavePath);
        }
    }

    // =============================
    // VALIDATION (Enable/Disable)
    // =============================

    [MenuItem("Yamigisa Engine/Delete Save", true)]
    private static bool ValidateDeleteSave()
    {
        return File.Exists(SavePath);
    }
}
#endif
