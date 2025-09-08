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

        // CHANGED: now a list named "groups" in ItemData
        private SerializedProperty propGroups;
        private SerializedProperty propItemActions;

        private SerializedProperty propIncreaseHealth;
        private SerializedProperty propIncreaseHunger;
        private SerializedProperty propIncreaseThirst;

        // NEW: damage field in ItemData
        private SerializedProperty propDamage;

        private void OnEnable()
        {
            propItemName = serializedObject.FindProperty("itemName");
            propIconWorld = serializedObject.FindProperty("iconWorld");
            propIconInventory = serializedObject.FindProperty("iconInventory");
            propDescription = serializedObject.FindProperty("description");
            propItemType = serializedObject.FindProperty("itemType");
            propMaxAmount = serializedObject.FindProperty("maxAmount");
            propIsDroppable = serializedObject.FindProperty("isDroppable");
            propIsStackable = serializedObject.FindProperty("isStackable");

            propGroups = serializedObject.FindProperty("groups");        // <-- updated name
            propItemActions = serializedObject.FindProperty("itemActions");

            propIncreaseHealth = serializedObject.FindProperty("increaseHealth");
            propIncreaseHunger = serializedObject.FindProperty("increaseHunger");
            propIncreaseThirst = serializedObject.FindProperty("increaseThirst");

            propDamage = serializedObject.FindProperty("damage");        // <-- new
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Base fields
            EditorGUILayout.PropertyField(propItemName);
            EditorGUILayout.PropertyField(propIconWorld);
            EditorGUILayout.PropertyField(propIconInventory);
            EditorGUILayout.PropertyField(propDescription);

            // Groups (List<GroupData>)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Groups", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propGroups, true);

            EditorGUILayout.PropertyField(propItemType);

            // Stackable / Max Amount logic
            EditorGUILayout.PropertyField(propIsStackable);
            if (!propIsStackable.boolValue)
            {
                propMaxAmount.intValue = 1;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(new GUIContent("Max Amount"), 1);
                }
            }
            else
            {
                int currentMax = Mathf.Max(1, propMaxAmount.intValue);
                currentMax = EditorGUILayout.IntField(new GUIContent("Max Amount"), currentMax);
                propMaxAmount.intValue = Mathf.Max(1, currentMax);
            }

            EditorGUILayout.PropertyField(propIsDroppable);

            // Actions list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propItemActions, true);

            // Type-specific fields
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
                if (propDamage.intValue < 0) propDamage.intValue = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
