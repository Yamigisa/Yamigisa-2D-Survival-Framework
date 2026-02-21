using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class GameManager : MonoBehaviour
    {
        [Header("Manager Settings")]
        [Header("If no child in managersParent, GameManager will instantiate prefabs.")]
        [SerializeField] private Transform managersParent;

        [SerializeField] private TimeClock timeClockPrefab;
        [SerializeField] private Inventory inventoryPrefab;
        [SerializeField] private AttributeUI attributeUIPrefab;
        [SerializeField] private CraftingInterface craftingInterfacePrefab;
        [SerializeField] private WorldGenerator worldGeneratorPrefab;
        [SerializeField] private PlaceableSystem placeableSystemPrefab;
        [SerializeField] private SaveManager saveManagerPrefab;

        [Header("Pause Settings")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button quitButton;

        // Pause 
        private bool isPaused;

        [Header("Death Settings")]
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private Button deathLoadButton;
        [SerializeField] private Button deathQuitButton;

        // Manager Instances
        private TimeClock timeClock;
        private Inventory inventory;
        private AttributeUI attributeUI;
        private CraftingInterface craftingInterface;
        private WorldGenerator worldGenerator;
        private PlaceableSystem PlaceableSystem;
        private SaveManager saveManager;

        public static GameManager instance;

        private void Awake()
        {
            instance = this;
            StartGame();
        }

        private void SetButtons()
        {
            // Pause Buttons
            resumeButton.onClick.AddListener(TogglePause);
            pauseButton.onClick.AddListener(TogglePause);
            saveButton.onClick.AddListener(saveManager.SaveGame);
            loadButton.onClick.AddListener(saveManager.LoadGame);
            quitButton.onClick.AddListener(Application.Quit);

            // Death Buttons
            deathLoadButton.onClick.AddListener(saveManager.LoadGame);
            deathQuitButton.onClick.AddListener(Application.Quit);
        }

        private void Update()
        {
            if (Character.instance.characterControls.IsPressed(
                Character.instance.characterControls.pause) && !Character.instance.CharacterIsBusy())
            {
                TogglePause();
            }
        }

        private void StartGame()
        {
            CreateObjects();
            PrepareGame();

            saveManager.LoadGame();
            SetButtons();
        }

        private void CreateObjects()
        {
            // If managersParent already has children, use them
            if (managersParent.childCount > 0)
            {
                timeClock = managersParent.GetComponentInChildren<TimeClock>();
                inventory = managersParent.GetComponentInChildren<Inventory>();
                attributeUI = managersParent.GetComponentInChildren<AttributeUI>();
                craftingInterface = managersParent.GetComponentInChildren<CraftingInterface>();
                worldGenerator = managersParent.GetComponentInChildren<WorldGenerator>();
                PlaceableSystem = managersParent.GetComponentInChildren<PlaceableSystem>();
                saveManager = managersParent.GetComponentInChildren<SaveManager>();

                return;
            }

            // Otherwise instantiate dynamically
            timeClock = Instantiate(timeClockPrefab, managersParent);
            inventory = Instantiate(inventoryPrefab, managersParent);
            attributeUI = Instantiate(attributeUIPrefab, managersParent);
            craftingInterface = Instantiate(craftingInterfacePrefab, managersParent);
            worldGenerator = Instantiate(worldGeneratorPrefab, managersParent);
            PlaceableSystem = Instantiate(placeableSystemPrefab, managersParent);
            saveManager = Instantiate(saveManagerPrefab, managersParent);
        }

        private void PrepareGame()
        {
            timeClock.Setup();
            inventory.Setup();
            craftingInterface.Setup();
            worldGenerator.Setup();
            PlaceableSystem.Setup();
        }

        private void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                Time.timeScale = 0;
                pausePanel.SetActive(true);
            }
            else
            {
                Time.timeScale = 1;
                pausePanel.SetActive(false);
            }
        }

        public void OnCharacterDeath()
        {
            Time.timeScale = 0;
            Character.instance.SetCharacterBusy(true);
            deathPanel.SetActive(true);
            timeClock.StopSystem();
        }

        public void OnGameStart()
        {
            Time.timeScale = 1;
            Character.instance.SetCharacterBusy(false);
            pausePanel.SetActive(false);
            deathPanel.SetActive(false);
        }

        public bool IsPaused { get { return isPaused; } }
    }
}