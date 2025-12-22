#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    public class DuplicateObject : ScriptableWizard
    {
        private const string RES = "Assets/YamigisaEngine/Resources";
        private const string PREFABS = "Assets/YamigisaEngine/Prefabs";
        private const string PREFABS_ITEMS = "Assets/YamigisaEngine/Prefabs/Items";
        private const string RES_ITEMS = "Assets/YamigisaEngine/Resources/Items";

        public ItemData source;
        public string duplicatedName;

        public bool makeNewPrefab = true;
        public Sprite iconInventory;
        public Sprite iconWorld;

        public bool retainItemInfo = true;

        [MenuItem("Yamigisa Engine/Duplicate Object", priority = 0)]
        static void Open()
        {
            DisplayWizard<DuplicateObject>("Duplicate Object", "Duplicate");
        }

        void OnWizardUpdate()
        {
            helpString = "Duplicates an ItemData asset.";
        }

        protected override bool DrawWizardGUI()
        {

            EditorGUILayout.Space(10);

            source = (ItemData)EditorGUILayout.ObjectField("Source ItemData", source, typeof(ItemData), false);
            duplicatedName = EditorGUILayout.TextField("Duplicated Name", duplicatedName);

            EditorGUILayout.Space(6);

            makeNewPrefab = EditorGUILayout.ToggleLeft("Make New Prefab", makeNewPrefab);

            if (makeNewPrefab)
            {
                iconWorld = (Sprite)EditorGUILayout.ObjectField("Icon (World)", iconWorld, typeof(Sprite), false);
                iconInventory = (Sprite)EditorGUILayout.ObjectField("Icon (Inventory)", iconInventory, typeof(Sprite), false);
            }

            retainItemInfo = EditorGUILayout.ToggleLeft("Retain Item Info", retainItemInfo);

            return true;
        }

        void OnWizardCreate()
        {
            if (source == null)
            {
                Debug.LogError("Source ItemData must be assigned.");
                return;
            }

            if (string.IsNullOrWhiteSpace(duplicatedName))
            {
                Debug.LogError("Duplicated name can't be blank.");
                return;
            }

            EnsureFolder(RES);
            EnsureFolder(PREFABS);
            EnsureFolder(PREFABS_ITEMS);
            EnsureFolder(RES_ITEMS);

            var itemType = retainItemInfo ? source.itemType : ItemType.Resource;
            string typeFolder = $"{RES_ITEMS}/{itemType}";
            EnsureFolder(typeFolder);

            string safeFileName = MakeSafeFileName(duplicatedName);
            string soPath = $"{typeFolder}/{safeFileName}.asset";

            if (File.Exists(soPath))
            {
                Debug.LogError("File already exists: " + soPath);
                return;
            }

            ItemData newItem;

            if (retainItemInfo)
            {
                var srcPath = AssetDatabase.GetAssetPath(source);
                AssetDatabase.CopyAsset(srcPath, soPath);
                newItem = AssetDatabase.LoadAssetAtPath<ItemData>(soPath);
                if (newItem == null) return;
            }
            else
            {
                newItem = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(newItem, soPath);
            }

            newItem.itemName = duplicatedName;

            if (!retainItemInfo)
                newItem.itemType = itemType;

            if (makeNewPrefab)
            {
                if (iconWorld != null) newItem.iconWorld = iconWorld;
                if (iconInventory != null) newItem.iconInventory = iconInventory;
            }
            else if (!retainItemInfo)
            {
                newItem.iconWorld = source.iconWorld;
                newItem.iconInventory = source.iconInventory;
            }

            EditorUtility.SetDirty(newItem);

            Object prefab = null;

            if (makeNewPrefab)
            {
                var prefabPath = $"{PREFABS_ITEMS}/{safeFileName}.prefab";
                if (File.Exists(prefabPath))
                {
                    Debug.LogError("Prefab already exists: " + prefabPath);
                    return;
                }

                prefab = CreateItemPrefabLikeCreateObject(newItem, prefabPath, duplicatedName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab != null ? prefab : newItem;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        static Object CreateItemPrefabLikeCreateObject(ItemData so, string prefabPath, string objectName)
        {
            var root = new GameObject(objectName);

            var box = root.AddComponent<BoxCollider2D>();
            var InteractiveObject = root.AddComponent<InteractiveObject>();

            var visualGO = new GameObject("Visual");
            visualGO.transform.SetParent(root.transform, false);
            var sr = visualGO.AddComponent<SpriteRenderer>();
            if (so.iconWorld) sr.sprite = so.iconWorld;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 2;

            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(root.transform, false);
            outlineGO.transform.localScale = Vector3.one * 1.3f;
            var outlineSR = outlineGO.AddComponent<SpriteRenderer>();
            outlineSR.sprite = so.iconWorld;
            outlineSR.color = Color.black;
            outlineSR.sortingLayerName = "Background";
            outlineSR.sortingOrder = 1;
            outlineGO.SetActive(false);

            FitColliderToSprite(box, sr);

            var soInteractiveObject = new SerializedObject(InteractiveObject);
            var pSR = soInteractiveObject.FindProperty("spriteRenderer");
            if (pSR != null) pSR.objectReferenceValue = sr;

            var pItem = soInteractiveObject.FindProperty("itemData");
            if (pItem != null) pItem.objectReferenceValue = so;

            var pOutline = soInteractiveObject.FindProperty("outlineObject");
            if (pOutline != null) pOutline.objectReferenceValue = outlineGO;

            soInteractiveObject.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void FitColliderToSprite(BoxCollider2D col, SpriteRenderer targetSR)
        {
            if (col == null || targetSR == null) return;
            var sp = targetSR.sprite;
            if (sp == null)
            {
                col.size = Vector2.one;
                col.offset = Vector2.zero;
                return;
            }
            Bounds b = sp.bounds;
            col.size = b.size;
            col.offset = b.center;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "NewItem" : name.Trim();
        }
    }
}
#endif
