using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class InteractiveObject : MonoBehaviour
    {
        public float interactRange = 2f;

        [Header("Actions")]
        public List<ActionBase> Actions = new List<ActionBase>();

        [Header("Groups")]
        public List<GroupData> Groups = new List<GroupData>();

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;


        private void Start()
        {
            SetOutline(false);
        }

        private void OnMouseEnter()
        {
            if (Character.instance.IsBusy)
                return;

            TextTooltip.Instance.ShowInteractiveObjectText(this);
            SetOutline(true);
        }

        private void OnMouseExit()
        {
            TextTooltip.Instance.CloseInteractiveObjectTexts();
            SetOutline(false);
        }

        private void OnMouseDown()
        {
            if (Character.instance.IsBusy)
                return;

            if (IsPointerOverAnyUI()) return;

            if (IsCharacterInRange(Character.instance.GetCharacter()))
            {
                InteractObject(Character.instance.GetCharacter());
            }
            else
            {
                Character.instance.GetCharacter().characterMovement
                    .MoveTo(transform.position, interactRange);

                Character.instance.GetCharacter().SetPendingInteraction(this);
            }
        }

        public bool IsCharacterInRange(Character character)
        {
            float sqrDistance =
                (character.transform.position - transform.position).sqrMagnitude;

            return sqrDistance <= interactRange * interactRange;
        }

        public void InteractObject(Character character)
        {
            foreach (ActionBase action in Actions)
            {
                action.DoAction(character, this);
            }

            TextTooltip.Instance.CloseInteractiveObjectTexts();
        }

        private bool IsPointerOverAnyUI()
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void SetOutline(bool on)
        {
            if (outlineObject) outlineObject.SetActive(on);
        }
    }
}
