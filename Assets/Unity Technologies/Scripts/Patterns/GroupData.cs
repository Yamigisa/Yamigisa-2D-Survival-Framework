using UnityEngine;

namespace Yamigisa
{

    [CreateAssetMenu(fileName = "GroupData", menuName = "Yamigisa/GroupData", order = 1)]
    public class GroupData : ScriptableObject
    {
        public string title;
        public Sprite icon;
    }
}