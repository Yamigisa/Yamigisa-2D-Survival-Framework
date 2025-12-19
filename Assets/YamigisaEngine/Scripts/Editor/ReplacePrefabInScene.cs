using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yamigisa
{
    public class ReplacePrefabInScene : EditorWindow
    {
        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private GameObject newPrefab;

        [MenuItem("Yamigisa Engine/Replace Prefab In Scene", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<ReplacePrefabInScene>("Replace Prefab Instances");
            w.minSize = new Vector2(420, 140);
        }

        private void OnGUI()
        {
            sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab ", sourcePrefab, typeof(GameObject), false);
            newPrefab = (GameObject)EditorGUILayout.ObjectField("New Prefab ", newPrefab, typeof(GameObject), false);

            using (new EditorGUI.DisabledScope(sourcePrefab == null || newPrefab == null))
            {
                if (GUILayout.Button("Replace Now", GUILayout.Height(30)))
                    ReplaceInActiveScene();
            }
        }

        private void ReplaceInActiveScene()
        {
            if (!IsPrefabAsset(sourcePrefab) || !IsPrefabAsset(newPrefab))
            {
                EditorUtility.DisplayDialog("Invalid Input", "Both fields must be prefab assets (from Project window).", "OK");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("No Active Scene", "Active scene is not valid/loaded.", "OK");
                return;
            }

            var roots = scene.GetRootGameObjects();
            var stack = new Stack<Transform>(Mathf.Max(32, roots.Length * 4));
            var toReplace = new List<GameObject>(64);

            for (int i = 0; i < roots.Length; i++)
                if (roots[i] != null) stack.Push(roots[i].transform);

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                var go = t.gameObject;

                if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                {
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (src == sourcePrefab)
                        toReplace.Add(go);
                }

                for (int i = t.childCount - 1; i >= 0; i--)
                    stack.Push(t.GetChild(i));
            }

            if (toReplace.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing Found", "No instances of the Source Prefab were found in the active scene.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < toReplace.Count; i++)
            {
                var oldRoot = toReplace[i];
                if (oldRoot == null) continue;

                var oldT = oldRoot.transform;
                var parent = oldT.parent;
                var pos = oldT.position;
                var rot = oldT.rotation;
                var scale = oldT.localScale;
                var sibling = oldT.GetSiblingIndex();

                var newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab, scene);
                Undo.RegisterCreatedObjectUndo(newObj, "Replace Prefab Instance");

                var newT = newObj.transform;
                newT.SetParent(parent, true);
                newT.position = pos;
                newT.rotation = rot;
                newT.localScale = scale;
                newT.SetSiblingIndex(sibling);

                Undo.DestroyObjectImmediate(oldRoot);
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool IsPrefabAsset(GameObject go)
        {
            if (go == null) return false;
            return PrefabUtility.IsPartOfPrefabAsset(go);
        }
    }
}
