using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class NewInteractiveObject : MonoBehaviour
    {
        public float interactRange = 2f;

        [Header("Actions")]
        public List<ActionBase> Actions = new List<ActionBase>();

        [Header("Groups")]
        public List<GroupData> Groups = new List<GroupData>();

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;

        private Character cachedCharacter;

        private void Start()
        {
            SetOutline(false);
            cachedCharacter = FindObjectOfType<Character>();
        }

        private void OnMouseEnter()
        {
            TextTooltip.Instance.ShowInteractiveObjectText(Actions);
            SetOutline(true);
        }

        private void OnMouseExit()
        {
            TextTooltip.Instance.CloseInteractiveObjectTexts();
            SetOutline(false);
        }

        private void OnMouseDown()
        {
            if (IsPointerOverAnyUI()) return;
            if (cachedCharacter == null) return;

            if (IsCharacterInRange(cachedCharacter))
            {
                InteractObject(cachedCharacter);
            }
            else
            {
                cachedCharacter.characterMovement
                    .MoveTo(transform.position, interactRange);

                cachedCharacter.SetPendingInteraction(this);
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
