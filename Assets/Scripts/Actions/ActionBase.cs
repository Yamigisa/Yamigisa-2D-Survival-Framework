using UnityEngine;

namespace Yamigisa
{
    public class ActionBase : ScriptableObject
    {
        public string title;

        public virtual void DoItemAction(Character character, InventoryItem slot)
        {

        }
    }
}