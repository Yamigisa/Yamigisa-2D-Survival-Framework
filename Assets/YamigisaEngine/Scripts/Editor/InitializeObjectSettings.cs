using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(
        fileName = "InitializeSceneSettings",
        menuName = "Yamigisa/Initialize Scene Settings",
        order = 101)]
    public class InitializeSceneSettings : ScriptableObject
    {
        [Header("Scene Creation")]
        public string defaultSceneFolder = "Assets/YamigisaEngine/Scenes";

        [Header("Required Scene Objects")]
        [Tooltip("Prefabs or scene objects that must exist in a scene.")]
        public List<GameObject> requiredObjects = new();
    }
}