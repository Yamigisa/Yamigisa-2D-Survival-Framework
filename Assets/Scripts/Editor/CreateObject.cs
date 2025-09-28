#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    public enum ObjectType { Item = 0 }

    public class CreateObjectWindow : EditorWindow
    {
        private const string MenuPath = "Yamigisa Engine/Create Object...";
        private const string RES = "Assets/Resources";
        private const string PREFABS = "Assets/Prefabs";
        private const string PREFABS_ITEMS = "Assets/Prefabs/Items";
        private const string RES_ITEMS = "Assets/Resources/Items";

        private string objectName = "NewItem";
        private ObjectType objectType = ObjectType.Item;

        // ItemData fields (minimal; extend as needed)
        private string itemDescription = "";
        private Sprite iconWorld;
        private Sprite iconInventory;
        private ItemType itemType = ItemType.Resource;
        private int maxAmount = 99;
        private bool isDroppable = true;
        private bool isStackable = true;

        // Optional data pools
        private List<GroupData> groups = new();
        private List<ActionBase> itemActions = new();

        private int increaseHealth = 0;
        private int increaseHunger = 0;
        private int increaseThirst = 0;
        private int damage = 0;

        // Crafting
        private bool isCraftable = false;
        private int craftResultAmount = 1;
        private List<(GroupData group, int amount)> craftGroupsNeeded = new();
        private List<(ItemData item, int amount)> craftItemsNeeded = new();

        // Selectable
        private int selectableAmount = 1;
        private float autoHideDistance = 4f;

        [MenuItem(MenuPath, priority = 0)]
        public static void Open()
        {
            var w = GetWindow<CreateObjectWindow>("Create Yamigisa Object");
            w.minSize = new Vector2(420, 560);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create Yamigisa Prefab + ItemData", EditorStyles.boldLabel);
            objectName = EditorGUILayout.TextField("Object Name", objectName);
            objectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", objectType);

            EditorGUILayout.Space(6);
            itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);
            iconWorld = (Sprite)EditorGUILayout.ObjectField("Icon (World)", iconWorld, typeof(Sprite), false);
            iconInventory = (Sprite)EditorGUILayout.ObjectField("Icon (Inventory)", iconInventory, typeof(Sprite), false);
            itemDescription = EditorGUILayout.TextField("Description", itemDescription);

            EditorGUILayout.Space(6);
            selectableAmount = Mathf.Max(1, EditorGUILayout.IntField("Selectable Amount", selectableAmount));
            autoHideDistance = EditorGUILayout.FloatField("Auto Hide Distance", autoHideDistance);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create Prefab + ItemData", GUILayout.Height(32)))
            {
                CreateAll();
            }
        }

        private void CreateAll()
        {
            EnsureFolder(RES);
            EnsureFolder(PREFABS);
            EnsureFolder(PREFABS_ITEMS);

            // 1) ItemData asset
            string typeFolder = $"{RES_ITEMS}/{itemType}";
            EnsureFolder(RES_ITEMS);
            EnsureFolder(typeFolder);

            var so = ScriptableObject.CreateInstance<ItemData>();
            so.itemName = objectName;
            so.iconWorld = iconWorld;
            so.iconInventory = iconInventory;
            so.description = itemDescription;
            so.itemType = itemType;
            so.maxAmount = maxAmount;
            so.isDroppable = isDroppable;
            so.isStackable = isStackable;

            // Stats / crafting (if your ItemData defines these fields)
            so.increaseHealth = increaseHealth;
            so.increaseHunger = increaseHunger;
            so.increaseThirst = increaseThirst;
            so.damage = damage;
            so.isCraftable = isCraftable;
            so.craftResultAmount = craftResultAmount;

            // If you want to persist authored actions:
            // so.itemActions = new List<ActionBase>(itemActions);

            string safeName = MakeSafeFileName(objectName);
            string soPath = $"{typeFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(so, soPath);

            // 2) Prefab root
            GameObject root = new GameObject(objectName);

            // Collider
            var box = root.AddComponent<BoxCollider2D>();

            // Components
            var selectable = root.AddComponent<Selectable>(); // no Destructible; Selectable handles that

            // Child: Visual with SpriteRenderer
            var visualGO = new GameObject("Visual");
            visualGO.transform.SetParent(root.transform, false);
            var sr = visualGO.AddComponent<SpriteRenderer>();
            if (so.iconWorld) sr.sprite = so.iconWorld;

            // Sorting for Visual: Background / order 2
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 2;

            // Child: Outline (with SpriteRenderer)
            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(root.transform, false);
            outlineGO.transform.localScale = Vector3.one * 1.3f;     // size 1.3
            var outlineSR = outlineGO.AddComponent<SpriteRenderer>();
            outlineSR.sprite = so.iconWorld;
            outlineSR.color = Color.black;
            outlineSR.sortingLayerName = "Background";
            outlineSR.sortingOrder = 1;
            outlineGO.SetActive(false);

            // Fit collider to the Visual sprite (size & offset from sprite bounds)
            FitColliderToSprite(box, sr);

            // Fill Selectable
            var soSelectable = new SerializedObject(selectable);
            soSelectable.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soSelectable.FindProperty("itemData").objectReferenceValue = so;
            soSelectable.FindProperty("amount").intValue = selectableAmount;
            soSelectable.FindProperty("autoHideDistance").floatValue = autoHideDistance;
            soSelectable.FindProperty("outlineObject").objectReferenceValue = outlineGO;
            soSelectable.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PREFABS_ITEMS}/{safeName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            ShowNotificationSafe($"Created Prefab at {prefabPath}\nItemData at {soPath}");
        }

        private static void FitColliderToSprite(BoxCollider2D col, SpriteRenderer targetSR)
        {
            if (col == null || targetSR == null) return;

            var sp = targetSR.sprite;
            if (sp == null)
            {
                col.size = Vector2.one;
                col.offset = Vector2.zero;
                return;
            }

            // Sprite bounds are in local units (respecting pixels-per-unit)
            Bounds b = sp.bounds;

            // If the SpriteRenderer has no scaling/rotation and is a child at (0,0), this fits nicely.
            col.size = b.size;
            col.offset = b.center;
        }

        private static void EnsureFolder(string path)
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

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "NewItem" : name.Trim();
        }

        private void ShowNotificationSafe(string msg)
        {
            try { ShowNotification(new GUIContent(msg)); } catch { }
        }
    }
}
#endif
