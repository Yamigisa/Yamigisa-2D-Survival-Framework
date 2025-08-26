using UnityEngine;

namespace Yamigisa
{
    public abstract class ActionSelectable : ScriptableObject
    {
        public string title;

        public abstract void DoAction(GameObject caller);
        public abstract bool CanDoAction(GameObject caller);
    }
}