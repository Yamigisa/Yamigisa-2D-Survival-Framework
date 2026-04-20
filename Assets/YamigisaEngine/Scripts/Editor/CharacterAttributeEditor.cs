#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Yamigisa
{
    [CustomEditor(typeof(CharacterAttribute))]
    public class CharacterAttributeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);
            GUILayout.Label("Attribute UI Tools", EditorStyles.boldLabel);

            CharacterAttribute attribute = (CharacterAttribute)target;

            if (GUILayout.Button("Initialize Attribute UI"))
            {
                attribute.EditorInitializeUI();
            }
        }
    }
}
#endif