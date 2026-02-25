using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class InteractiveObject : MonoBehaviour, ISavable
    {
        [Header("Save ID (DO NOT CHANGE FOR PLACED OBJECTS)")]
        [SerializeField] private string id;

        public float interactRange = 2f;

        [Header("Actions")]
        public List<ActionBase> Actions = new();

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;

        private bool pickedUp = false;

        private static InteractiveObject hovered;

        private void Awake()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString();
#endif
        }

        private void Start()
        {
            SetOutline(false);
        }

        private void Update()
        {
            HandleMouseHover();
            HandleMouseClick();
        }

        void HandleMouseHover()
        {
            // 🔒 HARD BLOCK interaction while building
            if (PlaceableSystem.instance != null)
            {
                if (PlaceableSystem.instance.IsInBuildMode ||
                    PlaceableSystem.instance.IsInteractionBlocked)
                    return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity,
                Character.instance.interactObjectLayer
            );

            InteractiveObject current =
                hit.collider ? hit.collider.GetComponent<InteractiveObject>() : null;

            if (hovered == current)
                return;

            if (hovered != null)
            {
                hovered.SetOutline(false);
                TextTooltip.Instance.CloseInteractiveObjectTexts();
            }

            hovered = current;

            if (hovered != null && !Character.instance.IsBusy)
            {
                hovered.SetOutline(true);
                TextTooltip.Instance.ShowInteractiveObjectText(hovered);
            }
        }

        void HandleMouseClick()
        {
            if (PlaceableSystem.instance != null)
            {
                if (PlaceableSystem.instance.IsInBuildMode ||
                    PlaceableSystem.instance.IsInteractionBlocked)
                    return;
            }

            if (hovered == null) return;
            if (Character.instance.IsBusy) return;
            if (IsPointerOverAnyUI()) return;

            Character character = Character.instance.GetCharacter();
            CharacterControls controls = Character.instance.characterControls;

            for (int i = 0; i < hovered.Actions.Count && i < 4; i++)
            {
                bool triggered = false;

                switch (i)
                {
                    case 0:
                        triggered = controls.IsPressedDown(controls.interaction1);
                        break;

                    case 1:
                        triggered = controls.IsPressedDown(controls.interaction2);
                        break;
                }

                if (!triggered) continue;

                if (hovered.IsCharacterInRange(character))
                {
                    hovered.Actions[i].DoAction(character, hovered);
                }
                else
                {
                    character.characterMovement.MoveTo(
                        hovered.transform.position,
                        hovered.interactRange
                    );

                    character.SetPendingInteraction(hovered);
                }

                TextTooltip.Instance.CloseInteractiveObjectTexts();
                return;
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
            if (Actions == null || Actions.Count == 0)
                return;

            Actions[0].DoAction(character, this);

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

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveInteractiveObjects)
                return;
            data.interactiveObjects.Add(new InteractiveObjectSaveData
            {
                id = id,
                position = transform.position,
                rotation = transform.rotation,
                active = gameObject.activeSelf,
                pickedUp = pickedUp
            });
        }

        public void Load(SaveGameData data)
        {
            var saved = data.interactiveObjects.Find(o => o.id == id);

            if (saved == null)
            {
                return;
            }

            if (saved.pickedUp)
            {
                Destroy(gameObject);
                return;
            }

            transform.SetPositionAndRotation(saved.position, saved.rotation);
            gameObject.SetActive(saved.active);
            pickedUp = false;
        }

        public void MarkPickedUp()
        {
            pickedUp = true;
        }

        public void SaveToList(List<InteractiveObjectSaveData> list)
        {
            list.Add(new InteractiveObjectSaveData
            {
                id = id,
                position = transform.position,
                rotation = transform.rotation,
                active = gameObject.activeSelf,
                pickedUp = pickedUp
            });
        }

        public bool IdMatches(string otherId)
        {
            return id == otherId;
        }
    }
}
