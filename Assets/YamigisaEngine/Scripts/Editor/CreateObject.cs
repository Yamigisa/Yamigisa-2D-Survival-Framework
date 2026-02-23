#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
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

        // NEW: temp editable lists
        private List<ConsumableEffect> tempConsumableEffects = new();
        private List<EquipmentStatModifier> tempEquipmentStats = new();

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
            settings = (CreateObjectSettings)EditorGUILayout.ObjectField(
                "Create Object Settings",
                settings,
                typeof(CreateObjectSettings),
                false
            );

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "CreateObjectSettings is required.\n\nCreate one via:\nCreate → YamigisaEngine → Create Object Settings",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.Space(6);

            objectName = EditorGUILayout.TextField("Object Name", objectName);
            objectType = (ObjectType)EditorGUILayout.EnumPopup("Object Type", objectType);

            if (objectType == ObjectType.Item)
            {
                DrawFilteredItemTypeDropdown();

                if (itemType == ItemType.Consumable)
                    DrawConsumableEffectsEditor();

                if (itemType == ItemType.Equipment)
                {
                    DrawEquipmentSection();
                }
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
                iconWorld = (Sprite)EditorGUILayout.ObjectField("Icon (World)", iconWorld, typeof(Sprite), false);
                iconInventory = (Sprite)EditorGUILayout.ObjectField("Icon (Inventory)", iconInventory, typeof(Sprite), false);
            }

            EditorGUILayout.Space(10);

            if (objectType == ObjectType.Animal)
            {
                animalBehaviour = (AnimalBehaviour)EditorGUILayout.EnumPopup("Animal Behaviour", animalBehaviour);
                animalSprite = (Sprite)EditorGUILayout.ObjectField("Animal Sprite", animalSprite, typeof(Sprite), false);
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


        // ===============================
        // FILTER ITEM TYPE (Remove Placeable)
        // ===============================

        private void DrawFilteredItemTypeDropdown()
        {
            EditorGUILayout.LabelField("Item Type");

            var values = System.Enum.GetValues(typeof(ItemType));
            List<ItemType> filtered = new();

            foreach (ItemType val in values)
            {
                if (val == ItemType.Placeable)
                    continue;

                filtered.Add(val);
            }

            int currentIndex = filtered.IndexOf(itemType);
            if (currentIndex < 0) currentIndex = 0;

            currentIndex = EditorGUILayout.Popup(
                currentIndex,
                filtered.ConvertAll(v => v.ToString()).ToArray()
            );

            itemType = filtered[currentIndex];
        }

        // ===============================
        // CONSUMABLE EFFECT EDITOR
        // ===============================

        private void DrawConsumableEffectsEditor()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Consumable Effects", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Effect"))
                tempConsumableEffects.Add(new ConsumableEffect());

            int removeIndex = -1;

            for (int i = 0; i < tempConsumableEffects.Count; i++)
            {
                var effect = tempConsumableEffects[i];

                EditorGUILayout.BeginVertical("box");

                effect.effectType = (ConsumableEffectType)
                    EditorGUILayout.EnumPopup("Effect Type", effect.effectType);

                effect.attributeType = (AttributeType)
                    EditorGUILayout.EnumPopup("Attribute", effect.attributeType);

                effect.instantAmount =
                    EditorGUILayout.IntField("Instant Amount", effect.instantAmount);

                effect.amountPerTick =
                    EditorGUILayout.IntField("Amount Per Tick", effect.amountPerTick);

                effect.tickInterval =
                    EditorGUILayout.FloatField("Tick Interval", effect.tickInterval);

                effect.duration =
                    EditorGUILayout.FloatField("Duration", effect.duration);

                effect.buffType = (BuffType)
                    EditorGUILayout.EnumPopup("Buff Type", effect.buffType);

                effect.buffAmount =
                    EditorGUILayout.FloatField("Buff Amount", effect.buffAmount);

                if (GUILayout.Button("Remove Effect"))
                    removeIndex = i;

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0 && removeIndex < tempConsumableEffects.Count)
                tempConsumableEffects.RemoveAt(removeIndex);
        }

        // ===============================
        // EQUIPMENT EFFECT EDITOR
        // ===============================

        private EquipmentSlotType selectedEquipmentSlot = EquipmentSlotType.None;

        private void DrawEquipmentSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Equipment Settings", EditorStyles.boldLabel);

            // SLOT SELECTION
            selectedEquipmentSlot = (EquipmentSlotType)
                EditorGUILayout.EnumPopup("Slot Type", selectedEquipmentSlot);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Stat Modifiers", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Modifier"))
                tempEquipmentStats.Add(new EquipmentStatModifier());

            int removeIndex = -1;

            for (int i = 0; i < tempEquipmentStats.Count; i++)
            {
                var stat = tempEquipmentStats[i];

                EditorGUILayout.BeginVertical("box");

                stat.statType = (StatType)
                    EditorGUILayout.EnumPopup("Stat", stat.statType);

                if (stat.statType == StatType.Attribute)
                {
                    stat.attributeType = (AttributeType)
                        EditorGUILayout.EnumPopup("Attribute", stat.attributeType);
                }

                stat.valueType = (StatValueType)
                    EditorGUILayout.EnumPopup("Value Type", stat.valueType);

                stat.value = EditorGUILayout.FloatField("Value", stat.value);

                if (GUILayout.Button("Remove Modifier"))
                    removeIndex = i;

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0 && removeIndex < tempEquipmentStats.Count)
                tempEquipmentStats.RemoveAt(removeIndex);
        }

        // ===============================
        // EXISTING METHODS (UNCHANGED)
        // ===============================

        private void CreatePlaceable() { /* UNCHANGED */ }
        private void CreateAnimal() { /* UNCHANGED */ }

        private void CreateItemOrDestroyable()
        {
            EnsureFolder(settings.itemsFolder);
            EnsureFolder(settings.prefabItemsFolder);

            var so = CreateInstance<ItemData>();
            so.itemName = objectName;
            so.iconWorld = iconWorld;
            so.iconInventory = iconInventory;
            so.itemType = itemType;

            if (itemType == ItemType.Equipment)
            {
                so.equipmentSlotType = selectedEquipmentSlot;
                so.equipmentStats = new List<EquipmentStatModifier>(tempEquipmentStats);
                so.isStackable = false;
            }
            else
            {
                so.equipmentSlotType = EquipmentSlotType.None;
            }

            if (itemType == ItemType.Consumable)
                so.consumableEffects = new List<ConsumableEffect>(tempConsumableEffects);

            if (itemType == ItemType.Equipment)
                so.equipmentStats = new List<EquipmentStatModifier>(tempEquipmentStats);

            so.itemActions = new List<ActionBase>();

            so.itemActions = new List<ActionBase>();

            // 1️⃣ Add specific type actions FIRST
            if (itemType == ItemType.Equipment &&
                settings.defaultEquipmentActions != null)
            {
                so.itemActions.AddRange(settings.defaultEquipmentActions);
            }
            else if (itemType == ItemType.Consumable &&
                     settings.defaultConsumableActions != null)
            {
                so.itemActions.AddRange(settings.defaultConsumableActions);
            }

            // 2️⃣ Add generic actions LAST (Drop, Split, etc)
            if (settings.defaultItemActions != null)
            {
                so.itemActions.AddRange(settings.defaultItemActions);
            }

            string safeName = MakeSafeFileName(objectName);

            EnsureItemTypeFolderExists(ItemType.Placeable);

            string soFolder = GetItemDataFolderForType(ItemType.Placeable);
            string soPath = $"{soFolder}/{safeName}.asset";

            AssetDatabase.CreateAsset(so, soPath);

            GameObject root = new GameObject(objectName);
            root.layer = Mathf.RoundToInt(Mathf.Log(settings.interactiveObjectLayer.value, 2));
            root.AddComponent<BoxCollider2D>();

            var interactive = root.AddComponent<InteractiveObject>();

            if (settings.defaultInteractiveObjectActions != null &&
                settings.defaultInteractiveObjectActions.Length > 0)
            {
                interactive.Actions = new List<ActionBase>(
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

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = iconWorld;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;

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

            so.itemPrefab = prefab;
            EditorUtility.SetDirty(so);

            tempConsumableEffects.Clear();
            tempEquipmentStats.Clear();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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

        private void EnsureItemTypeFolderExists(ItemType type)
        {
            EnsureFolder(settings.itemsFolder);

            string targetFolder = GetItemDataFolderForType(type);
            EnsureFolder(targetFolder);
        }

        private string GetItemDataFolderForType(ItemType type)
        {
            string baseFolder = settings.itemsFolder;

            // Create subfolder based on enum name automatically
            string subFolder = type.ToString();

            return $"{baseFolder}/{subFolder}";
        }
    }
}
#endif