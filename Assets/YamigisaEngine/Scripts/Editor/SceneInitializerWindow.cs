#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yamigisa
{
    public class SceneInitializerWindow : EditorWindow
    {
        private InitializeSceneSettings settings;

        [MenuItem("Yamigisa Engine/Scene Initializer", priority = 1)]
        public static void Open()
        {
            GetWindow<SceneInitializerWindow>("Scene Initializer");
        }

        private void OnEnable()
        {
            if (settings == null)
                AutoFindSettings();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scene Initializer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            settings = (InitializeSceneSettings)EditorGUILayout.ObjectField(
                "Initialize Scene Settings",
                settings,
                typeof(InitializeSceneSettings),
                false
            );

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "InitializeSceneSettings is required.\n\nCreate one via:\nCreate -> Yamigisa -> Initialize Scene Settings",
                    MessageType.Warning
                );

                if (GUILayout.Button("Find Settings Automatically"))
                    AutoFindSettings();

                return;
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Initialize Scene", GUILayout.Height(30)))
            {
                InitializeScene();
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

        private void InitializeScene()
        {
            if (settings == null)
            {
                Debug.LogError("InitializeSceneSettings is missing!");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogError("No active scene found!");
                return;
            }

            List<GameObject> missingObjects = new();

            foreach (GameObject requiredObject in settings.requiredObjects)
            {
                if (requiredObject == null)
                    continue;

                if (IsObjectAlreadyInScene(requiredObject, activeScene))
                    continue;

                missingObjects.Add(requiredObject);
            }

            if (missingObjects.Count == 0)
            {
                Debug.Log("Objects are already initialized!");
                return;
            }

            foreach (GameObject missingObject in missingObjects)
            {
                CreateObjectInScene(missingObject, activeScene);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"Scene initialized! Added {missingObjects.Count} missing object(s).");
        }

        private bool IsObjectAlreadyInScene(GameObject requiredObject, Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in allTransforms)
                {
                    GameObject sceneObject = t.gameObject;

                    // Check by prefab source first
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject);
                    if (prefabSource == requiredObject)
                        return true;

                    // Fallback: check by name
                    if (sceneObject.name == requiredObject.name)
                        return true;
                }
            }

            return false;
        }

        private void CreateObjectInScene(GameObject requiredObject, Scene scene)
        {
            GameObject newObject;

            if (PrefabUtility.IsPartOfPrefabAsset(requiredObject))
            {
                newObject = (GameObject)PrefabUtility.InstantiatePrefab(requiredObject, scene);
            }
            else
            {
                newObject = Instantiate(requiredObject);
                newObject.name = requiredObject.name;
                SceneManager.MoveGameObjectToScene(newObject, scene);
            }

            Undo.RegisterCreatedObjectUndo(newObject, "Initialize Scene Object");
        }
    }
}
#endif