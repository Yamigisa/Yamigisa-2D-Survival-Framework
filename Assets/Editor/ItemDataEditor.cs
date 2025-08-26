using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    [CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty itemName = serializedObject.FindProperty("itemName");
            SerializedProperty iconWorld = serializedObject.FindProperty("iconWorld");
            SerializedProperty iconInventory = serializedObject.FindProperty("iconInventory");
            SerializedProperty description = serializedObject.FindProperty("description");
            SerializedProperty itemType = serializedObject.FindProperty("itemType");
            SerializedProperty maxAmount = serializedObject.FindProperty("maxAmount");
            SerializedProperty isDroppable = serializedObject.FindProperty("isDroppable");
            SerializedProperty isStackable = serializedObject.FindProperty("isStackable");

            SerializedProperty increaseMaxHealth = serializedObject.FindProperty("increaseMaxHealth");
            SerializedProperty increaseMaxHunger = serializedObject.FindProperty("increaseMaxHunger");
            SerializedProperty increaseMaxThirst = serializedObject.FindProperty("increaseMaxThirst");

            SerializedProperty increaseHealth = serializedObject.FindProperty("increaseHealth");
            SerializedProperty increaseHunger = serializedObject.FindProperty("increaseHunger");
            SerializedProperty increaseThirst = serializedObject.FindProperty("increaseThirst");

            // Draw base fields
            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(iconWorld);
            EditorGUILayout.PropertyField(iconInventory);
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(itemType);
            EditorGUILayout.PropertyField(maxAmount);
            EditorGUILayout.PropertyField(isDroppable);
            EditorGUILayout.PropertyField(isStackable);

            // Conditional effect fields
            ItemType type = (ItemType)itemType.enumValueIndex;

            if (type == ItemType.Equipment)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Equipment Effect", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(increaseMaxHealth);
                EditorGUILayout.PropertyField(increaseMaxHunger);
                EditorGUILayout.PropertyField(increaseMaxThirst);
            }
            else if (type == ItemType.Consumable)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Consumable Effect", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(increaseHealth);
                EditorGUILayout.PropertyField(increaseHunger);
                EditorGUILayout.PropertyField(increaseThirst);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
