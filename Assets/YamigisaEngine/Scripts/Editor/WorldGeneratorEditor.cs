using UnityEditor;
using UnityEngine;
using Yamigisa;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);
        GUILayout.Label("World Controls", EditorStyles.boldLabel);

        WorldGenerator generator = (WorldGenerator)target;

        GUILayout.Space(5);

        GUILayout.Label("World Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Create World (Add)"))
        {
            generator.EditorCreateWorld();
        }

        if (GUILayout.Button("Refresh World"))
        {
            generator.EditorRefreshWorld();
        }

        if (GUILayout.Button("Delete World"))
        {
            generator.EditorDeleteWorld();
        }

        GUILayout.Space(10);
        GUILayout.Label("Object Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Objects In World"))
        {
            generator.EditorCreateObjects();
        }

        if (GUILayout.Button("Delete Objects In World"))
        {
            generator.EditorDeleteObjects();
        }
    }
}