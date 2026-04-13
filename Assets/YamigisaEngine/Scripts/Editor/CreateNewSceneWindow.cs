#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yamigisa
{
    public class CreateNewSceneWindow : EditorWindow
    {
        private InitializeSceneSettings settings;

        private string sceneName = "NewScene";

        private DefaultAsset sceneFolder;

        [MenuItem("Yamigisa Engine/Create New Scene", priority = 2)]
        public static void Open()
        {
            GetWindow<CreateNewSceneWindow>("Create New Scene");
        }

        private void OnEnable()
        {
            AutoFindSettings();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create New Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            settings = (InitializeSceneSettings)EditorGUILayout.ObjectField(
                "Initialize Scene Settings",
                settings,
                typeof(InitializeSceneSettings),
                false
            );

            sceneName = EditorGUILayout.TextField("Scene Name", sceneName);

            EditorGUILayout.LabelField(
    "Optional: If left empty, will use default from settings",
    EditorStyles.miniLabel
);

            sceneFolder = (DefaultAsset)EditorGUILayout.ObjectField(
     "Scene Folder",
     sceneFolder,
     typeof(DefaultAsset),
     false
 );

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Create Scene", GUILayout.Height(30)))
            {
                CreateScene();
            }
        }

        private void AutoFindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:InitializeSceneSettings");
            if (guids.Length <= 0)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<InitializeSceneSettings>(path);
        }

        private void CreateScene()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Scene name cannot be empty!");
                return;
            }

            string folderPath = GetFolderPath(sceneFolder);

            // kalau kosong → pakai default dari settings
            if (string.IsNullOrEmpty(folderPath))
            {
                if (settings == null || string.IsNullOrEmpty(settings.defaultSceneFolder))
                {
                    Debug.LogError("No folder assigned and no default folder in settings!");
                    return;
                }

                folderPath = settings.defaultSceneFolder;
            }

            EnsureFolder(folderPath);

            string safeName = MakeSafeFileName(sceneName);
            string scenePath = $"{folderPath}/{safeName}.unity";

            if (File.Exists(scenePath))
            {
                Debug.LogError("Scene already exists!");
                return;
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, scenePath);
            InitializeRequiredObjects(newScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Scene created: {scenePath}");
        }

        private void InitializeRequiredObjects(Scene scene)
        {
            if (settings == null || settings.requiredObjects == null)
            {
                Debug.LogWarning("No InitializeSceneSettings or required objects found!");
                return;
            }

            int createdCount = 0;

            foreach (var prefab in settings.requiredObjects)
            {
                if (prefab == null)
                    continue;

                // check if already exists (by name)
                bool exists = false;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == prefab.name)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                    continue;

                GameObject obj;

                if (PrefabUtility.IsPartOfPrefabAsset(prefab))
                {
                    obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                }
                else
                {
                    obj = Instantiate(prefab);
                    obj.name = prefab.name;
                    SceneManager.MoveGameObjectToScene(obj, scene);
                }

                Undo.RegisterCreatedObjectUndo(obj, "Initialize Scene Object");
                createdCount++;
            }

            Debug.Log($"Initialized {createdCount} object(s) in new scene.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        private string GetFolderPath(DefaultAsset folderAsset)
        {
            if (folderAsset == null)
                return null;

            string path = AssetDatabase.GetAssetPath(folderAsset);

            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                return null;

            return path;
        }
    }
}
#endif