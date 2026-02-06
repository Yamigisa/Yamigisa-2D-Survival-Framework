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

        private void OnMouseEnter()
        {
            if (Character.instance.IsBusy) return;
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
            if (Character.instance.IsBusy) return;
            if (IsPointerOverAnyUI()) return;

            if (IsCharacterInRange(Character.instance.GetCharacter()))
                InteractObject(Character.instance.GetCharacter());
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
