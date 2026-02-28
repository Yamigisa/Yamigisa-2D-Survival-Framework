using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(
        fileName = "Sleep",
        menuName = "Yamigisa/Actions/Sleep",
        order = 50)]
    public class ActionSleep : ActionBase
    {
        public override bool CanDoAction(Component context = null)
        {
            if (context == null)
                return false;

            Bed bed = context.GetComponent<Bed>();
            if (bed == null)
                return false;

            if (Character.instance == null)
                return false;

            if (Character.instance.CharacterIsBusy())
                return false;

            return bed.CanSleepExternally();
        }

        public override void DoAction(Character character, Component context)
        {
            Bed bed = context.GetComponent<Bed>();
            if (bed == null)
                return;

            if (!bed.GetComponent<InteractiveObject>()
                    .IsCharacterInRange(character))
                return;

            bed.TrySleep();
        }

        public override string GetActionName(Component context = null)
        {
            if (context == null)
                return title;

            Bed bed = context.GetComponent<Bed>();
            if (bed == null)
                return title;

            return title;
        }
    }
}