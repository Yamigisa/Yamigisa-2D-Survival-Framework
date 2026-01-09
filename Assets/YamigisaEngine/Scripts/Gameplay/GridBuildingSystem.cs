using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GridBuildingSystem : MonoBehaviour
{
    public GridLayout gridLayout;
    public Tilemap mainTilemap;
    public Tilemap TempTilemap;

    [Header("Tiles (Assign from Inspector)")]
    [SerializeField] private TileBase whiteTile;
    [SerializeField] private TileBase greenTile;
    [SerializeField] private TileBase redTile;

    private static Dictionary<TileType, TileBase> tileBases = new Dictionary<TileType, TileBase>();

    private Building temp;
    private Vector3 prevPos;
    private BoundsInt prevArea;

    public static GridBuildingSystem instance;
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        tileBases.Clear();
        tileBases.Add(TileType.Empty, null);
        tileBases.Add(TileType.White, whiteTile);
        tileBases.Add(TileType.Green, greenTile);
        tileBases.Add(TileType.Red, redTile);
    }

    void Update()
    {
        if (!temp || temp.Placed)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // === FOLLOW CURSOR ALWAYS ===
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = gridLayout.WorldToCell(mouseWorld);

        if (prevPos != cellPos)
        {
            temp.transform.localPosition =
                gridLayout.CellToLocalInterpolated(cellPos + new Vector3(.5f, .5f, 0f));

            prevPos = cellPos;
            FollowBuilding();
        }

        // === PLACE WITH LEFT CLICK ===
        if (Input.GetMouseButtonDown(0))
        {
            // IMPORTANT: prevent overwrite
            if (!CanTakeArea(prevArea))
            {
                Debug.Log("Cannot place here (area blocked)");
                return;
            }

            temp.Place();               // should set Placed=true on the building
            TakeArea(prevArea);         // marks occupied + clears preview
            temp = null;                // stop preview mode
            return;
        }

        // === CANCEL ===
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearArea();
            Destroy(temp.gameObject);
            temp = null;
        }
    }


    public void InitializeWithBuilding(GameObject building)
    {
        temp = Instantiate(building, Vector3.zero, Quaternion.identity).GetComponent<Building>();
        FollowBuilding();
    }

    private void ClearArea()
    {
        if (prevArea.size == Vector3Int.zero) return;

        TileBase[] toClear = new TileBase[prevArea.size.x * prevArea.size.y * prevArea.size.z];
        FillTiles(toClear, TileType.Empty);
        TempTilemap.SetTilesBlock(prevArea, toClear);
    }

    private void FollowBuilding()
    {
        ClearArea();

        temp.area.position = gridLayout.WorldToCell(temp.gameObject.transform.position);
        BoundsInt buildingArea = temp.area;

        TileBase[] baseArray = GetTilesBlock(buildingArea, mainTilemap);

        int size = baseArray.Length;
        TileBase[] tileArray = new TileBase[size];

        for (int i = 0; i < baseArray.Length; i++)
        {
            if (baseArray[i] == tileBases[TileType.White])
            {
                tileArray[i] = tileBases[TileType.Green];
            }
            else
            {
                FillTiles(tileArray, TileType.Red);
                break;
            }
        }

        TempTilemap.SetTilesBlock(buildingArea, tileArray);
        prevArea = buildingArea;
    }

    public bool CanTakeArea(BoundsInt area)
    {
        TileBase[] baseArray = GetTilesBlock(area, mainTilemap);
        foreach (var tile in baseArray)
        {
            if (tile != tileBases[TileType.White])
            {
                Debug.Log("Cannot take area");
                return false;
            }
        }

        return true;
    }

    public void TakeArea(BoundsInt area)
    {
        // SetTilesBlock(area, TileType.Empty, mainTilemap);
        // SetTilesBlock(area, TileType.Green, TempTilemap);

        // Mark occupied on main tilemap by removing the "White/free" tiles
        SetTilesBlock(area, TileType.Empty, mainTilemap);

        // Clear preview tiles so they don't stay forever
        SetTilesBlock(area, TileType.Empty, TempTilemap);
    }
    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
    {
        TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
        int index = 0;

        foreach (var pos in area.allPositionsWithin)
        {
            Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
            array[index] = tilemap.GetTile(tilePos);
            index++;
        }

        return array;
    }

    private static void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
    {
        int size = area.size.x * area.size.y * area.size.z;

        TileBase[] tileArray = new TileBase[size];
        FillTiles(tileArray, type);
        tilemap.SetTilesBlock(area, tileArray);
    }

    private static void FillTiles(TileBase[] arr, TileType type)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = tileBases[type];
        }
    }

    public enum TileType
    {
        Empty,
        White,
        Green,
        Red
    }
}
