using UnityEngine;

namespace Yamigisa
{
    public class ActionBase : ScriptableObject
    {
        public string title;

        // Inventory Related
        public virtual void DoAction(Character character, ItemSlot slot) { }

        // Character Related
        public virtual void DoAction(Character character, Component context) { }

        public virtual bool CanDoAction(Component context = null)
        {
            return true;
        }
    }
}