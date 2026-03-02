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
        Placeable,
        Biome,
        Character
    }

    public enum PlaceableType
    {
        Storage,
        CraftingPlaceable,
        Bed
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

        // Biome
        private Sprite biomeSprite;
        private bool biomeUseCustom = false;
        private BiomeData selectedBiomeData;

        // Character
        private Sprite characterIcon;

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

            if (objectType == ObjectType.Character)
            {
                characterIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Character Icon",
                    characterIcon,
                    typeof(Sprite),
                    false
                );
            }

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
            else if (objectType == ObjectType.Biome)
            {
                biomeSprite = (Sprite)EditorGUILayout.ObjectField(
                    "Biome Sprite",
                    biomeSprite,
                    typeof(Sprite),
                    false
                );

                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField("Biome Assignment", EditorStyles.boldLabel);

                biomeUseCustom = EditorGUILayout.Toggle(
                    "Use Custom Biome",
                    biomeUseCustom
                );

                if (biomeUseCustom)
                {
                    selectedBiomeData = (BiomeData)EditorGUILayout.ObjectField(
                        "Biome Data",
                        selectedBiomeData,
                        typeof(BiomeData),
                        false
                    );
                }
            }

            if (objectType != ObjectType.Animal &&
        objectType != ObjectType.Character)
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
                else if (objectType == ObjectType.Biome)
                    CreateBiome(); // 🔥 NEW
                else if (objectType == ObjectType.Character)
                    CreateCharacter(); // 🔥 NEW
                else
                    CreateItemOrDestroyable();
            }
        }

        private void CreateCharacter()
        {
            EnsureFolder(settings.prefabItemsFolder);

            string safeName = MakeSafeFileName(objectName);
            string prefabPath = $"{settings.prefabItemsFolder}/{safeName}.prefab";

            // ==========================
            // ROOT
            // ==========================

            GameObject root = new GameObject(objectName);

            root.layer = Mathf.RoundToInt(
                Mathf.Log(settings.interactiveObjectLayer.value, 2)
            );

            // ==========================
            // COMPONENTS ON ROOT
            // ==========================

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            root.AddComponent<Animator>();

            root.AddComponent<CharacterMovement>();
            root.AddComponent<CharacterAttribute>();
            root.AddComponent<CharacterControls>();
            root.AddComponent<CharacterAnimation>();
            root.AddComponent<Character>();
            root.AddComponent<CharacterCombat>();
            root.AddComponent<CharacterBiome>();

            // ==========================
            // CHILD: Visual
            // ==========================

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = characterIcon; // 🔥 APPLY ICON HERE
            sr.sortingLayerName = "Character";
            sr.sortingOrder = 0;

            // ==========================
            // CHILD: Collider
            // ==========================

            GameObject colliderObj = new GameObject("Collider");
            colliderObj.transform.SetParent(root.transform, false);

            var collider = colliderObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;

            // 🔥 Auto size collider to sprite
            if (characterIcon != null)
            {
                collider.size = characterIcon.bounds.size;
                collider.offset = characterIcon.bounds.center;
            }

            // ==========================
            // CHILD: Main Camera
            // ==========================

            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.transform.SetParent(root.transform, false);

            var cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.tag = "MainCamera";

            cameraObj.transform.position = new Vector3(0, 0, -10);

            // ==========================
            // SAVE PREFAB
            // ==========================

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
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

        private void CreatePlaceable()
        {
            EnsureFolder(settings.prefabItemsFolder);
            EnsureFolder(settings.itemsFolder);

            string safeName = MakeSafeFileName(objectName);

            // =====================================================
            // 1️⃣ CREATE ITEM DATA (FOR BUILDING)
            // =====================================================

            var so = CreateInstance<ItemData>();
            so.itemName = objectName;
            so.iconWorld = iconWorld;
            so.iconInventory = iconInventory;
            so.itemType = ItemType.Placeable;
            // =====================================================
            // ACTION ORDER (STRICT ORDER CONTROL)
            // =====================================================

            so.itemActions = new List<ActionBase>();

            // 1️⃣ Insert placeable inventory actions at index 0
            if (settings.globalPlaceableItemDataActions != null)
            {
                for (int i = settings.globalPlaceableItemDataActions.Length - 1; i >= 0; i--)
                {
                    var action = settings.globalPlaceableItemDataActions[i];

                    if (action != null && !so.itemActions.Contains(action))
                        so.itemActions.Insert(0, action);
                }
            }

            // 2️⃣ Add generic item actions AFTER
            if (settings.defaultItemActions != null)
            {
                foreach (var action in settings.defaultItemActions)
                {
                    if (action != null && !so.itemActions.Contains(action))
                        so.itemActions.Add(action);
                }
            }

            string soFolder = $"{settings.itemsFolder}/Placeable";
            EnsureFolder(soFolder);

            string soPath = $"{soFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(so, soPath);

            // =====================================================
            // 2️⃣ CREATE PREFAB (WORLD OBJECT)
            // =====================================================

            string prefabPath = $"{settings.prefabItemsFolder}/{safeName}.prefab";

            GameObject root = new GameObject(objectName);
            root.layer = Mathf.RoundToInt(Mathf.Log(settings.interactiveObjectLayer.value, 2));

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var interactive = root.AddComponent<InteractiveObject>();
            root.AddComponent<Placeable>();

            // Add Item component to placeable
            var item = root.AddComponent<Item>();
            item.itemData = so;
            item.quantity = 1;
            // =====================================================
            // APPLY INTERACTIVE ACTIONS (PLACEABLE FIRST)
            // =====================================================

            interactive.Actions = new List<ActionBase>();

            // 1️⃣ Add placeable-specific actions FIRST
            if (settings.defaultPlaceableActions != null)
            {
                foreach (var entry in settings.defaultPlaceableActions)
                {
                    if (entry.type != placeableType || entry.actions == null)
                        continue;

                    for (int i = 0; i < entry.actions.Length; i++)
                    {
                        var action = entry.actions[i];
                        if (action != null && !interactive.Actions.Contains(action))
                            interactive.Actions.Add(action);
                    }

                    break;
                }
            }

            // 2️⃣ THEN add default interactive actions AFTER (Pick etc.)
            if (settings.defaultInteractiveObjectActions != null)
            {
                foreach (var action in settings.defaultInteractiveObjectActions)
                {
                    if (action != null && !interactive.Actions.Contains(action))
                        interactive.Actions.Add(action);
                }
            }

            // Add specific components
            switch (placeableType)
            {
                case PlaceableType.CraftingPlaceable:
                    root.AddComponent<CraftingPlaceable>();
                    break;

                case PlaceableType.Bed:
                    root.AddComponent<Bed>();
                    break;
            }

            // =====================================================
            // 4️⃣ VISUAL
            // =====================================================

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = iconWorld;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;

            if (iconWorld != null)
            {
                collider.size = iconWorld.bounds.size;
                collider.offset = iconWorld.bounds.center;
            }

            // =====================================================
            // OUTLINE (MATCH OTHER PREFABS)
            // =====================================================

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

            // =====================================================
            // 5️⃣ SAVE PREFAB
            // =====================================================

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
            // ==========================
            // ENSURE FOLDERS
            // ==========================

            EnsureFolder(settings.prefabAnimalsFolder);
            EnsureFolder(settings.animalDataFolder);

            string safeName = MakeSafeFileName(objectName);

            // ==========================
            // CREATE ANIMAL DATA (SO)
            // ==========================

            var animalData = CreateInstance<AnimalData>();
            animalData.behaviour = animalBehaviour;

            string dataPath = $"{settings.animalDataFolder}/{safeName}.asset";
            AssetDatabase.CreateAsset(animalData, dataPath);

            // ==========================
            // CREATE PREFAB ROOT
            // ==========================

            GameObject root = new GameObject(objectName);
            root.layer = Mathf.RoundToInt(Mathf.Log(settings.interactiveObjectLayer.value, 2));

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var destroyable = root.AddComponent<Destroyable>();
            destroyable.hp = destroyableHP;

            var interactive = root.AddComponent<InteractiveObject>();

            // Apply default animal interactive actions
            if (settings.defaultAnimalActions != null &&
                settings.defaultAnimalActions.Length > 0)
            {
                interactive.Actions = new List<ActionBase>(
                    settings.defaultAnimalActions
                );
            }
            else
            {
                interactive.Actions = new List<ActionBase>();
            }

            var animal = root.AddComponent<Animal>();
            animal.animalData = animalData;

            // ==========================
            // VISUAL
            // ==========================

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = animalSprite;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;

            if (animalSprite != null)
            {
                collider.size = animalSprite.bounds.size;
                collider.offset = animalSprite.bounds.center;
            }

            // ==========================
            // OUTLINE (MATCH OTHER PREFABS)
            // ==========================

            var outline = new GameObject("Outline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = Vector3.one * 1.3f;

            var osr = outline.AddComponent<SpriteRenderer>();
            osr.sprite = animalSprite;
            osr.color = Color.black;
            osr.sortingLayerName = "Object";
            osr.sortingOrder = -1;
            outline.SetActive(false);

            SerializedObject sio = new SerializedObject(interactive);
            sio.FindProperty("outlineObject").objectReferenceValue = outline;
            sio.ApplyModifiedPropertiesWithoutUndo();

            // ==========================
            // SAVE PREFAB
            // ==========================

            string prefabPath =
                $"{settings.prefabAnimalsFolder}/{safeName}.prefab";

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

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

            // Only items get default item actions
            if (objectType == ObjectType.Item)
            {
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
            }
            else
            {
                // Destroyable → no item actions
                so.itemActions = new List<ActionBase>();
            }

            string safeName = MakeSafeFileName(objectName);

            EnsureItemTypeFolderExists(itemType);

            string soFolder = GetItemDataFolderForType(itemType);
            string soPath = $"{soFolder}/{safeName}.asset";

            AssetDatabase.CreateAsset(so, soPath);

            GameObject root = new GameObject(objectName);
            root.layer = Mathf.RoundToInt(Mathf.Log(settings.interactiveObjectLayer.value, 2));
            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var interactive = root.AddComponent<InteractiveObject>();

            // Only items get default interactive actions
            if (objectType == ObjectType.Item &&
                settings.defaultInteractiveObjectActions != null &&
                settings.defaultInteractiveObjectActions.Length > 0)
            {
                interactive.Actions = new List<ActionBase>(
                    settings.defaultInteractiveObjectActions);
            }
            else
            {
                interactive.Actions = new List<ActionBase>();
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
            // 🔥 AUTO RESIZE COLLIDER TO SPRITE
            if (iconWorld != null)
            {
                collider.size = iconWorld.bounds.size;
                collider.offset = iconWorld.bounds.center;
            }
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

        private void CreateBiome()
        {
            EnsureFolder(settings.prefabItemsFolder);

            string safeName = MakeSafeFileName(objectName);
            string prefabPath = $"{settings.prefabItemsFolder}/{safeName}.prefab";

            GameObject root = new GameObject(objectName);

            root.layer = Mathf.RoundToInt(
                Mathf.Log(settings.interactiveObjectLayer.value, 2)
            );

            // ==========================
            // COLLIDER
            // ==========================

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            // ==========================
            // SPRITE
            // ==========================

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = biomeSprite;
            sr.sortingLayerName = "Object";
            sr.sortingOrder = -5;

            if (biomeSprite != null)
            {
                collider.size = biomeSprite.bounds.size;
                collider.offset = biomeSprite.bounds.center;
            }

            // ==========================
            // BIOME TRIGGER
            // ==========================

            var trigger = root.AddComponent<BiomeTrigger>();

            SerializedObject so = new SerializedObject(trigger);

            so.FindProperty("useCustomBiome").boolValue = biomeUseCustom;

            if (biomeUseCustom)
                so.FindProperty("customBiomeData").objectReferenceValue = selectedBiomeData;

            so.ApplyModifiedPropertiesWithoutUndo();

            // ==========================
            // SAVE PREFAB
            // ==========================

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            DestroyImmediate(root);

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