using UnityEngine;

namespace Yamigisa
{
    public class GameManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private TimeClock timeClockPrefab;
        [SerializeField] private Inventory inventoryPrefab;
        [SerializeField] private AttributeUI attributeUIPrefab;
        [SerializeField] private CraftingInterface craftingInterfacePrefab;
        [SerializeField] private WorldGenerator worldGeneratorPrefab;
        [SerializeField] private GridBuildingSystem gridBuildingSystemPrefab;
        [SerializeField] private SaveManager saveManagerPrefab;

        private TimeClock timeClock;
        private Inventory inventory;
        private AttributeUI attributeUI;
        private CraftingInterface craftingInterface;
        private WorldGenerator worldGenerator;
        private GridBuildingSystem gridBuildingSystem;
        private SaveManager saveManager;


        private void Start()
        {
            CreateObjects();

            timeClock.Setup();
            inventory.Setup();
            craftingInterface.Setup();
            worldGenerator.Setup();
            gridBuildingSystem.Setup();

            saveManager.LoadGame();
        }

        private void CreateObjects()
        {
            timeClock = Instantiate(timeClockPrefab, transform);
            inventory = Instantiate(inventoryPrefab, transform);
            attributeUI = Instantiate(attributeUIPrefab, transform);
            craftingInterface = Instantiate(craftingInterfacePrefab, transform);
            worldGenerator = Instantiate(worldGeneratorPrefab, transform);
            gridBuildingSystem = Instantiate(gridBuildingSystemPrefab, transform);
            saveManager = Instantiate(saveManagerPrefab, transform);
        }
    }
}