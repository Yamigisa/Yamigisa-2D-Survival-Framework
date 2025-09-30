using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CraftSelection : MonoBehaviour
    {
        [Header("Build From Resources")]
        [Tooltip("Optional subfolder inside a Resources/… path. Leave blank to scan all Resources.")]
        [SerializeField] private string resourcesSubfolder = "Resources/Groups/Craft";

        private List<GroupData> groupCrafts = new List<GroupData>();

        void Start()
        {
            BuildCraftSelection();
        }

        private void BuildCraftSelection()
        {
            GroupData[] groups = Resources.LoadAll<GroupData>(resourcesSubfolder);
            foreach (GroupData group in groups)
            {
                groupCrafts.Add(group);
            }
        }
    }
}