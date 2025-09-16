using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    [CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : Editor
    {
        private SerializedProperty propItemName;
        private SerializedProperty propIconWorld;
        private SerializedProperty propIconInventory;
        private SerializedProperty propDescription;
        private SerializedProperty propItemType;
        private SerializedProperty propMaxAmount;
        private SerializedProperty propIsDroppable;
        private SerializedProperty propIsStackable;

        private SerializedProperty propGroups;
        private SerializedProperty propItemActions;

        private SerializedProperty propIncreaseHealth;
        private SerializedProperty propIncreaseHunger;
        private SerializedProperty propIncreaseThirst;

        private SerializedProperty propDamage;

        // Crafting
        private SerializedProperty propIsCraftable;
        private SerializedProperty propCraftGroupsNeeded;
        private SerializedProperty propCraftItemsNeeded;
        private SerializedProperty propCraftResultAmount;

        private SerializedProperty Find(string name)
        {
            var p = serializedObject.FindProperty(name);
            if (p == null)
                EditorGUILayout.HelpBox($"Missing serialized field: {name}", MessageType.Error);
            return p;
        }

        private void OnEnable()
        {
            propItemName = Find("itemName");
            propIconWorld = Find("iconWorld");
            propIconInventory = Find("iconInventory");
            propDescription = Find("description");
            propItemType = Find("itemType");
            propMaxAmount = Find("maxAmount");
            propIsDroppable = Find("isDroppable");
            propIsStackable = Find("isStackable");

            propGroups = Find("groups");
            propItemActions = Find("itemActions");

            propIncreaseHealth = Find("increaseHealth");
            propIncreaseHunger = Find("increaseHunger");
            propIncreaseThirst = Find("increaseThirst");

            propDamage = Find("damage");

            // Crafting (matches ItemData)
            propIsCraftable = Find("isCraftable");
            propCraftGroupsNeeded = Find("craftGroupsNeeded");
            propCraftItemsNeeded = Find("craftItemsNeeded");
            propCraftResultAmount = Find("craftResultAmount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Base
            EditorGUILayout.PropertyField(propItemName);
            EditorGUILayout.PropertyField(propIconWorld);
            EditorGUILayout.PropertyField(propIconInventory);
            EditorGUILayout.PropertyField(propDescription);

            // Groups
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propGroups, true);

            // Type + stack rules
            EditorGUILayout.PropertyField(propItemType);

            EditorGUILayout.PropertyField(propIsStackable);
            if (propIsStackable != null && !propIsStackable.boolValue)
            {
                if (propMaxAmount != null) propMaxAmount.intValue = 1;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(new GUIContent("Max Amount"), 1);
                }
            }
            else
            {
                if (propMaxAmount != null)
                {
                    int currentMax = Mathf.Max(1, propMaxAmount.intValue);
                    currentMax = EditorGUILayout.IntField(new GUIContent("Max Amount"), currentMax);
                    propMaxAmount.intValue = Mathf.Max(1, currentMax);
                }
            }

            EditorGUILayout.PropertyField(propIsDroppable);

            // Actions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propItemActions, true);

            // Type-specific
            if (propItemType != null)
            {
                ItemType type = (ItemType)propItemType.enumValueIndex;

                if (type == ItemType.Consumable)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Consumable Effects", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(propIncreaseHealth);
                    EditorGUILayout.PropertyField(propIncreaseHunger);
                    EditorGUILayout.PropertyField(propIncreaseThirst);
                }

                if (type == ItemType.Equipment)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Equipment Effects", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(propDamage);
                    if (propDamage != null && propDamage.intValue < 0) propDamage.intValue = 0;
                }
            }

            // Crafting
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Crafting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propIsCraftable, new GUIContent("Is Craftable"));

            if (propIsCraftable != null && propIsCraftable.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(propCraftGroupsNeeded, new GUIContent("Groups Needed"), true);
                    EditorGUILayout.PropertyField(propCraftItemsNeeded, new GUIContent("Specific Items Needed"), true);

                    if (propCraftResultAmount != null)
                    {
                        int result = Mathf.Max(1, propCraftResultAmount.intValue);
                        result = EditorGUILayout.IntField(new GUIContent("Crafted Amount"), result);
                        propCraftResultAmount.intValue = Mathf.Max(1, result);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
