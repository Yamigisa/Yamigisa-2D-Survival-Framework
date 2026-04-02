using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class InteractiveObject : MonoBehaviour, ISavable
    {
        [Header("Save ID (DO NOT CHANGE FOR PLACED OBJECTS)")]
        [HideInInspector][SerializeField] private string id;

        public float interactRange = 0.1f;

        [Header("Actions")]
        public List<ActionBase> Actions = new();

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;
        private SpriteRenderer targetSpriteRenderer;

        private bool pickedUp = false;
        private bool isRegrowing = false;
        private int currentGrowthStageIndex = -1;
        private float remainingRealSeconds = 0f;
        private int remainingGameMinutes = 0;

        private Sprite originalSprite;
        private ItemData cachedItemData;
        private Collider2D[] cachedColliders;

        private static InteractiveObject hovered;

        public bool IsRegrowing => isRegrowing;

        private void Awake()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString();
#endif

            Item item = GetComponent<Item>();
            cachedItemData = item != null ? item.itemData : null;

            cachedColliders = GetComponentsInChildren<Collider2D>(true);

            if (targetSpriteRenderer == null)
                targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (targetSpriteRenderer != null)
                originalSprite = targetSpriteRenderer.sprite;
        }

        private void Start()
        {
            SetOutline(false);
            SubscribeGrowthTime();
        }

        private void Update()
        {
            HandleMouseHover();
            HandleMouseClick();

            if (!isRegrowing || cachedItemData == null)
                return;

            if (cachedItemData.growthTimeMode == GrowthTimeMode.RealSeconds)
                UpdateRealSecondGrowth();
        }

        private ItemData GetRegrowthItemData()
        {
            return cachedItemData;
        }

        void HandleMouseHover()
        {
            if (PlaceableSystem.instance != null)
            {
                if (PlaceableSystem.instance.IsInBuildMode ||
                    PlaceableSystem.instance.IsInteractionBlocked)
                {
                    Debug.Log("[Hover] Blocked by PlaceableSystem");
                    return;
                }
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity,
                Character.instance.interactObjectLayer
            );

            if (hit.collider != null)
            {
                Debug.Log("[Hover] Hit collider: " + hit.collider.name);
            }
            else
            {
                Debug.Log("[Hover] No collider hit");
            }

            InteractiveObject current =
                hit.collider ? hit.collider.GetComponent<InteractiveObject>() : null;

            if (current == null && hit.collider != null)
            {
                current = hit.collider.GetComponentInParent<InteractiveObject>();
                if (current != null)
                    Debug.Log("[Hover] Found InteractiveObject in parent: " + current.name);
            }

            if (hovered == current)
                return;

            if (hovered != null)
            {
                Debug.Log("[Hover] Removing hover from: " + hovered.name);
                hovered.SetOutline(false);
                TextTooltip.Instance.CloseInteractiveObjectTexts();
            }

            hovered = current;

            if (hovered != null && !Character.instance.IsBusy)
            {
                Debug.Log("[Hover] Now hovering: " + hovered.name);
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
                {
                    return;
                }
            }

            if (hovered == null)
                return;

            if (Character.instance.IsBusy)
                return;

            if (IsPointerOverAnyUI())
                return;

            if (hovered.isRegrowing)
                return;

            Character character = Character.instance.GetCharacter();
            CharacterControls controls = Character.instance.characterControls;

            int actionIndex = controls.GetPressedInteractionActionIndexDown();
            if (actionIndex < 0)
                return;

            if (hovered.Actions == null || actionIndex >= hovered.Actions.Count)
                return;

            ActionBase action = hovered.Actions[actionIndex];
            if (action == null)
                return;

            if (hovered.IsCharacterInRange(character))
            {
                action.DoAction(character, hovered);
            }
            else
            {
                character.characterMovement.MoveTo(
                    hovered.transform.position,
                    hovered.interactRange
                );

                character.SetPendingInteraction(hovered, actionIndex);
            }

            TextTooltip.Instance.CloseInteractiveObjectTexts();
        }

        public bool IsCharacterInRange(Character character)
        {
            float sqrDistance =
                (character.transform.position - transform.position).sqrMagnitude;

            float range = interactRange * interactRange;

            Debug.Log($"[Range] Distance: {sqrDistance} | Required: {range}");

            return sqrDistance <= range;
        }

        public void InteractObject(Character character, int actionIndex = 0)
        {
            if (Actions == null || Actions.Count == 0 || isRegrowing)
                return;

            if (actionIndex < 0 || actionIndex >= Actions.Count)
                return;

            if (Actions[actionIndex] == null)
                return;

            Actions[actionIndex].DoAction(character, this);
            TextTooltip.Instance.CloseInteractiveObjectTexts();
        }

        private bool IsPointerOverAnyUI()
        {
            if (EventSystem.current == null)
            {
                Debug.Log("[UI Check] No EventSystem");
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count == 0)
            {
                Debug.Log("[UI Check] No UI hit");
                return false;
            }

            Debug.Log("[UI Check] UI hits count: " + results.Count);

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                Debug.Log("[UI Check] Hit: " + r.gameObject.name);
            }

            return results.Count > 0;
        }

        public void SetOutline(bool on)
        {
            if (outlineObject) outlineObject.SetActive(on);
        }

        public void HandleHarvested()
        {
            if (CanStartRegrowth())
            {
                StartRegrowth();
            }
            else
            {
                Debug.Log("No regrowth for " + gameObject.name);
                MarkPickedUp();
                Destroy(gameObject);
            }
        }

        public bool CanStartRegrowth()
        {
            ItemData itemData = GetRegrowthItemData();

            return itemData != null &&
                   itemData.itemType == ItemType.Resource &&
                   itemData.growthStages != null &&
                   itemData.growthStages.Count > 0;
        }

        public void StartRegrowth()
        {
            if (!CanStartRegrowth())
                return;

            Debug.Log("starting regrowth for " + gameObject.name);

            isRegrowing = true;
            pickedUp = false;
            currentGrowthStageIndex = 0;
            remainingRealSeconds = 0f;
            remainingGameMinutes = 0;

            DisableHarvestInteraction();
            ApplyCurrentGrowthStage();
        }

        private void ApplyCurrentGrowthStage()
        {
            if (cachedItemData == null ||
                currentGrowthStageIndex < 0 ||
                currentGrowthStageIndex >= cachedItemData.growthStages.Count)
                return;

            ResourceGrowthStage stage = cachedItemData.growthStages[currentGrowthStageIndex];

            if (targetSpriteRenderer != null && stage.sprite != null)
                targetSpriteRenderer.sprite = stage.sprite;

            if (cachedItemData.growthTimeMode == GrowthTimeMode.RealSeconds)
            {
                remainingRealSeconds = stage.duration;
                remainingGameMinutes = 0;
            }
            else
            {
                remainingGameMinutes = Mathf.Max(1, Mathf.CeilToInt(stage.duration));
                remainingRealSeconds = 0f;
            }
        }

        private void UpdateRealSecondGrowth()
        {
            remainingRealSeconds -= Time.deltaTime;

            while (isRegrowing && remainingRealSeconds <= 0f)
            {
                AdvanceGrowthStage();
            }
        }

        private void HandleGameMinutePassed()
        {
            if (!isRegrowing || cachedItemData == null)
                return;

            if (cachedItemData.growthTimeMode != GrowthTimeMode.GameMinutes)
                return;

            remainingGameMinutes--;

            if (remainingGameMinutes <= 0)
                AdvanceGrowthStage();

            Debug.Log("remaining game minutes for " + gameObject.name + ": " + remainingGameMinutes);
        }

        private void AdvanceGrowthStage()
        {
            currentGrowthStageIndex++;

            if (currentGrowthStageIndex >= cachedItemData.growthStages.Count)
            {
                FinishRegrowth();
                return;
            }

            Debug.Log("Current stage growth" + currentGrowthStageIndex);
            ApplyCurrentGrowthStage();
        }

        private void FinishRegrowth()
        {
            isRegrowing = false;
            currentGrowthStageIndex = -1;
            remainingRealSeconds = 0f;
            remainingGameMinutes = 0;

            ItemData itemData = GetRegrowthItemData();

            if (targetSpriteRenderer != null)
            {
                if (itemData != null && itemData.iconWorld != null)
                    targetSpriteRenderer.sprite = itemData.iconWorld;
                else
                    targetSpriteRenderer.sprite = originalSprite;
            }

            EnableHarvestInteraction();

            Destroyable destroyable = GetComponent<Destroyable>();
            if (destroyable != null)
                destroyable.ResetDestroyableStateAfterRegrow();

            Debug.Log("finished regrowth for " + gameObject.name);
        }

        private void DisableHarvestInteraction()
        {
            SetOutline(false);

            if (cachedColliders == null) return;

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = false;
            }
        }

        private void EnableHarvestInteraction()
        {
            if (cachedColliders == null) return;

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = true;
            }
        }

        private void SubscribeGrowthTime()
        {
            if (TimeClock.Instance != null)
                TimeClock.Instance.OnMinuteChanged += HandleGameMinutePassed;
        }

        private void UnsubscribeGrowthTime()
        {
            if (TimeClock.Instance != null)
                TimeClock.Instance.OnMinuteChanged -= HandleGameMinutePassed;
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
                pickedUp = pickedUp,
                isRegrowing = isRegrowing,
                currentGrowthStageIndex = currentGrowthStageIndex,
                remainingRealSeconds = remainingRealSeconds,
                remainingGameMinutes = remainingGameMinutes
            });
        }

        public void Load(SaveGameData data)
        {
            var saved = data.interactiveObjects.Find(o => o.id == id);

            if (saved == null)
                return;

            transform.SetPositionAndRotation(saved.position, saved.rotation);
            gameObject.SetActive(saved.active);

            pickedUp = saved.pickedUp;
            isRegrowing = saved.isRegrowing;
            currentGrowthStageIndex = saved.currentGrowthStageIndex;
            remainingRealSeconds = saved.remainingRealSeconds;
            remainingGameMinutes = saved.remainingGameMinutes;

            if (pickedUp)
            {
                Destroy(gameObject);
                return;
            }

            ItemData itemData = GetRegrowthItemData();

            if (isRegrowing)
            {
                DisableHarvestInteraction();

                if (currentGrowthStageIndex >= 0 &&
                    itemData != null &&
                    itemData.growthStages != null &&
                    currentGrowthStageIndex < itemData.growthStages.Count)
                {
                    ResourceGrowthStage stage = itemData.growthStages[currentGrowthStageIndex];

                    if (targetSpriteRenderer != null && stage.sprite != null)
                        targetSpriteRenderer.sprite = stage.sprite;
                }
            }
            else
            {
                EnableHarvestInteraction();

                if (targetSpriteRenderer != null)
                {
                    if (itemData != null && itemData.iconWorld != null)
                        targetSpriteRenderer.sprite = itemData.iconWorld;
                    else
                        targetSpriteRenderer.sprite = originalSprite;
                }
            }
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
                pickedUp = pickedUp,
                isRegrowing = isRegrowing,
                currentGrowthStageIndex = currentGrowthStageIndex,
                remainingRealSeconds = remainingRealSeconds,
                remainingGameMinutes = remainingGameMinutes
            });
        }

        public bool IdMatches(string otherId)
        {
            return id == otherId;
        }
    }
}