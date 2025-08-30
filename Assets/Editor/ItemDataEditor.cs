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

            var itemName = serializedObject.FindProperty("itemName");
            var iconWorld = serializedObject.FindProperty("iconWorld");
            var iconInventory = serializedObject.FindProperty("iconInventory");
            var description = serializedObject.FindProperty("description");
            var itemType = serializedObject.FindProperty("itemType");
            var maxAmount = serializedObject.FindProperty("maxAmount");
            var isDroppable = serializedObject.FindProperty("isDroppable");
            var isStackable = serializedObject.FindProperty("isStackable");
            var itemActions = serializedObject.FindProperty("itemActions");   // <-- add this

            var increaseMaxHealth = serializedObject.FindProperty("increaseMaxHealth");
            var increaseMaxHunger = serializedObject.FindProperty("increaseMaxHunger");
            var increaseMaxThirst = serializedObject.FindProperty("increaseMaxThirst");

            var increaseHealth = serializedObject.FindProperty("increaseHealth");
            var increaseHunger = serializedObject.FindProperty("increaseHunger");
            var increaseThirst = serializedObject.FindProperty("increaseThirst");

            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(iconWorld);
            EditorGUILayout.PropertyField(iconInventory);
            EditorGUILayout.PropertyField(description);
            EditorGUILayout.PropertyField(itemType);
            EditorGUILayout.PropertyField(maxAmount);
            EditorGUILayout.PropertyField(isDroppable);
            EditorGUILayout.PropertyField(isStackable);

            // Draw the actions list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(itemActions, includeChildren: true);     // <-- draw it

            var type = (ItemType)itemType.enumValueIndex;
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
