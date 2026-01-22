#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    public enum ObjectType
    {
        Item,
        Destroyable,
        Animal,
        Placeable
    }

    public enum PlaceableType
    {
        Storage,
    }

    public class CreateObjectWindow : EditorWindow
    {
        private string objectName = "NewItem";
        private ObjectType objectType = ObjectType.Item;

        private Sprite iconWorld;
        private Sprite iconInventory;
        private ItemType itemType = ItemType.Resource;
        private int destroyableHP = 100;

        private AnimalBehaviour animalBehaviour = AnimalBehaviour.Passive;
        private Sprite animalSprite;

        private PlaceableType placeableType = PlaceableType.Storage;


        // USER-SELECTABLE SETTINGS
        public CreateObjectSettings settings;

        [MenuItem("Yamigisa Engine/Create Object", priority = 0)]
        public static void Open()
        {
            GetWindow<CreateObjectWindow>("Create Object");
        }

        private void OnEnable()
        {
            if (settings == null)
                AutoFindSettings();
        }

        private void AutoFindSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:CreateObjectSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<CreateObjectSettings>(path);
            }
        }

        private void OnGUI()
        {
            // SETTINGS PICKER
            settings = (CreateObjectSettings)EditorGUILayout.ObjectField(
                "Create Object Settings",
                settings,
                typeof(CreateObjectSettings),
                false
            );

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "CreateObjectSettings is required.\n\n" +
                    "Create one via:\nCreate → YamigisaEngine → Create Object Settings",
                    MessageType.Error);
                return;
            }


            EditorGUILayout.Space(6);

            objectName = EditorGUILayout.TextField("Object Name", objectName);
            objectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", objectType);

            if (objectType == ObjectType.Item)
            {
                itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);
            }
            else if (objectType == ObjectType.Destroyable || objectType == ObjectType.Animal)
            {
                destroyableHP = EditorGUILayout.IntField("Health (HP)", destroyableHP);
            }
            else if (objectType == ObjectType.Placeable)
            {
                placeableType = (PlaceableType)EditorGUILayout.EnumPopup(
                    "Placeable Type",
                    placeableType
                );
            }

            if (objectType != ObjectType.Animal)
            {
                iconWorld = (Sprite)EditorGUILayout.ObjectField(
                    "Icon (World)",
                    iconWorld,
                    typeof(Sprite),
                    false
                );

                iconInventory = (Sprite)EditorGUILayout.ObjectField(
                    "Icon (Inventory)",
                    iconInventory,
                    typeof(Sprite),
                    false
                );
            }

            EditorGUILayout.Space(10);

            if (objectType == ObjectType.Animal)
            {
                animalBehaviour = (AnimalBehaviour)EditorGUILayout.EnumPopup(
                    "Animal Behaviour",
                    animalBehaviour
                );

                animalSprite = (Sprite)EditorGUILayout.ObjectField(
                    "Animal Sprite",
                    animalSprite,
                    typeof(Sprite),
                    false
                );
            }

            if (GUILayout.Button("Create Prefab + Data"))
            {
                if (objectType == ObjectType.Animal)
                    CreateAnimal();
                else if (objectType == ObjectType.Placeable)
                    CreatePlaceable();
                else
                    CreateItemOrDestroyable();
            }

        }

        private void CreatePlaceable()
        {
            EnsureFolder(settings.itemsFolder);
            EnsureFolder(settings.prefabItemsFolder);

            string safeName = MakeSafeFileName(objectName);

            // ---------- ITEM DATA (SCRIPTABLE OBJECT) ----------
            var so = CreateInstance<ItemData>();
            so.itemName = objectName;
            so.iconWorld = iconWorld;
            so.iconInventory = iconInventory;
            so.itemType = ItemType.Placeable;

            string soPath = $"{settings.itemsFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(so, soPath);

            // ---------- ROOT ----------
            GameObject root = new GameObject(objectName);
            root.AddComponent<BoxCollider2D>();

            var interactive = root.AddComponent<InteractiveObject>();
            var placeable = root.AddComponent<Placeable>();

            // ---------- VISUAL ----------
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = iconWorld;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;

            // ---------- OUTLINE ----------
            var outline = new GameObject("Outline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.3f;
            var osr = outline.AddComponent<SpriteRenderer>();
            osr.sprite = iconWorld;
            osr.color = Color.black;
            osr.sortingLayerName = "Object";
            osr.sortingOrder = -1;
            outline.SetActive(false);

            SerializedObject sio = new SerializedObject(interactive);
            sio.FindProperty("outlineObject").objectReferenceValue = outline;
            sio.ApplyModifiedPropertiesWithoutUndo();

            // ---------- PLACEABLE TYPE DISPATCH ----------
            switch (placeableType)
            {
                case PlaceableType.Storage:
                    root.AddComponent<Storage>();
                    break;
            }

            // ---------- DEFAULT ACTIONS ----------
            if (settings.defaultPlaceableActions != null)
            {
                foreach (var entry in settings.defaultPlaceableActions)
                {
                    if (entry.type == placeableType)
                    {
                        interactive.Actions =
                            new System.Collections.Generic.List<ActionBase>(entry.actions);
                        break;
                    }
                }
            }

            // ---------- ITEM COMPONENT ----------
            var item = root.AddComponent<Item>();
            item.itemData = so;
            item.quantity = 1;

            // ---------- SAVE PREFAB ----------
            string prefabPath = $"{settings.prefabItemsFolder}/{safeName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            // Link prefab back to ItemData
            so.itemPrefab = prefab;
            EditorUtility.SetDirty(so);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private void CreateAnimal()
        {
            EnsureFolder(settings.prefabAnimalsFolder);
            EnsureFolder(settings.animalDataFolder);

            string safeName = MakeSafeFileName(objectName);

            // ---------- AnimalData ----------
            var animalData = CreateInstance<AnimalData>();
            animalData.behaviour = animalBehaviour;

            string dataPath = $"{settings.animalDataFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(animalData, dataPath);

            // ---------- Root ----------
            GameObject root = new GameObject(objectName);

            var box = root.AddComponent<BoxCollider2D>();
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var interactive = root.AddComponent<InteractiveObject>();
            if (settings.defaultAnimalActions != null &&
                settings.defaultAnimalActions.Length > 0)
            {
                interactive.Actions =
                    new System.Collections.Generic.List<ActionBase>(
                        settings.defaultAnimalActions
                    );
            }
            if (objectType == ObjectType.Item)
            {
                itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);
            }
            else if (objectType == ObjectType.Destroyable || objectType == ObjectType.Animal)
            {
                destroyableHP = EditorGUILayout.IntField("Health (HP)", destroyableHP);
            }
            var destroyable = root.AddComponent<Destroyable>();
            destroyable.hp = destroyableHP;

            var animal = root.AddComponent<Animal>();
            animal.animalData = animalData;

            var animator = root.AddComponent<Animator>();

            // ---------- Visual ----------
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = animalSprite;
            sr.sortingLayerName = "NPC";
            sr.sortingOrder = 0;

            // ---------- Outline ----------
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.3f;

            var osr = outline.AddComponent<SpriteRenderer>();
            osr.sprite = animalSprite;
            osr.color = Color.black;
            osr.sortingLayerName = "NPC";
            osr.sortingOrder = -1;

            outline.SetActive(false);

            SerializedObject sio = new SerializedObject(interactive);
            sio.FindProperty("outlineObject").objectReferenceValue = outline;
            sio.ApplyModifiedPropertiesWithoutUndo();

            // ---------- Prefab ----------
            string prefabPath = $"{settings.prefabAnimalsFolder}/{safeName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }


        private void CreateItemOrDestroyable()
        {
            EnsureFolder(settings.itemsFolder);
            EnsureFolder(settings.prefabItemsFolder);

            // ---------- ScriptableObject ----------
            var so = CreateInstance<ItemData>();
            so.itemName = objectName;
            so.iconWorld = iconWorld;
            so.iconInventory = iconInventory;
            so.itemType = itemType;

            if (settings.defaultItemActions != null &&
                settings.defaultItemActions.Length > 0)
            {
                so.itemActions = new System.Collections.Generic.List<ActionBase>(
                    settings.defaultItemActions);
            }

            string safeName = MakeSafeFileName(objectName);
            string soPath = $"{settings.itemsFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(so, soPath);

            // ---------- Prefab ----------
            GameObject root = new GameObject(objectName);
            root.AddComponent<BoxCollider2D>();

            var interactive = root.AddComponent<InteractiveObject>();

            if (settings.defaultInteractiveObjectActions != null &&
                settings.defaultInteractiveObjectActions.Length > 0)
            {
                interactive.Actions = new System.Collections.Generic.List<ActionBase>(
                    settings.defaultInteractiveObjectActions);
            }

            if (objectType == ObjectType.Item)
            {
                var item = root.AddComponent<Item>();
                item.itemData = so;
                item.quantity = 1;
            }
            else
            {
                var destroyable = root.AddComponent<Destroyable>();
                destroyable.hp = destroyableHP;
            }

            // Visual
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = iconWorld;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;

            // Outline
            var outline = new GameObject("Outline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.3f;
            var osr = outline.AddComponent<SpriteRenderer>();
            osr.sprite = iconWorld;
            osr.color = Color.black;
            osr.sortingLayerName = "Object";
            osr.sortingOrder = -1;
            outline.SetActive(false);

            SerializedObject sio = new SerializedObject(interactive);
            sio.FindProperty("outlineObject").objectReferenceValue = outline;
            sio.ApplyModifiedPropertiesWithoutUndo();

            string prefabPath = $"{settings.prefabItemsFolder}/{safeName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            // LINK PREFAB
            so.itemPrefab = prefab;
            EditorUtility.SetDirty(so);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 🔥 SHOW IMMEDIATELY IN PROJECT WINDOW
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder($"{current}/{parts[i]}"))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current += "/" + parts[i];
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
#endif
