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
        // =====================
        // ID SAFETY
        // =====================
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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity,
                Character.instance.interactObjectLayer
            );

            InteractiveObject current =
                hit.collider ? hit.collider.GetComponent<InteractiveObject>() : null;

            if (hovered == current) return;

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
            if (hovered == null) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (Character.instance.IsBusy) return;
            if (IsPointerOverAnyUI()) return;

            Character character = Character.instance.GetCharacter();

            if (hovered.IsCharacterInRange(character))
                hovered.InteractObject(character);
            else
            {
                character.characterMovement.MoveTo(
                    hovered.transform.position,
                    hovered.interactRange
                );
                character.SetPendingInteraction(hovered);
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
                action.DoAction(character, this);

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

        // =====================
        // SAVE / LOAD
        // =====================
        public void Save(ref SaveGameData data)
        {
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
                return; // never saved before → keep it
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

        // =====================
        // PICKUP
        // =====================
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
