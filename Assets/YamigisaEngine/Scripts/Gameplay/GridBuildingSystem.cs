using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class GridBuildingSystem : MonoBehaviour
    {
        public GridLayout gridLayout;
        public Tilemap mainTilemap;
        public Tilemap TempTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase whiteTile;
        [SerializeField] private TileBase greenTile;
        [SerializeField] private TileBase redTile;

        [Header("Grid Size")]
        [SerializeField] private Vector2Int buildGridSize = new Vector2Int(10, 10);

        [SerializeField] private LayerMask blockingLayers;

        private bool buildMode;
        private BoundsInt buildBounds;

        private Transform buildAnchor;

        private static Dictionary<TileType, TileBase> tileBases = new Dictionary<TileType, TileBase>();

        private Building temp;
        private Vector3 prevPos;
        private BoundsInt prevArea;

        private Vector3Int lastAnchorCell;
        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

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

            // GRID OFF INITIALLY
            mainTilemap.gameObject.SetActive(false);
            TempTilemap.gameObject.SetActive(false);

            buildAnchor = Character.instance.transform;
        }

        void Update()
        {
            if (!buildMode)
                return;

            Vector3Int currentAnchorCell = gridLayout.WorldToCell(buildAnchor.position);

            if (currentAnchorCell != lastAnchorCell)
            {
                lastAnchorCell = currentAnchorCell;
                RebuildGrid(currentAnchorCell);

                prevArea = new BoundsInt(); // reset preview footprint
            }

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

                temp.Place();      // Building.Place() already calls TakeArea(areaTemp)
                temp = null;
                ExitBuildMode();
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


        public void InitializeBuilding(GameObject building)
        {
            if (buildMode)
                ExitBuildMode();

            EnterBuildMode();

            temp = Instantiate(building, Vector3.zero, Quaternion.identity)
                .GetComponent<Building>();

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

            temp.area.position = gridLayout.WorldToCell(temp.transform.position);
            BoundsInt buildingArea = temp.area;

            bool canPlace = CanTakeArea(buildingArea);

            int size = buildingArea.size.x * buildingArea.size.y * buildingArea.size.z;
            TileBase[] tileArray = new TileBase[size];

            if (canPlace)
            {
                FillTiles(tileArray, TileType.Green);
            }
            else
            {
                FillTiles(tileArray, TileType.Red);
            }

            TempTilemap.SetTilesBlock(buildingArea, tileArray);
            prevArea = buildingArea;
        }

        public bool CanTakeArea(BoundsInt area)
        {
            // optional: keep placement only inside current grid zone
            if (!buildBounds.Contains(area.min) || !buildBounds.Contains(area.max - Vector3Int.one))
                return false;

            foreach (var pos in area.allPositionsWithin)
            {
                if (occupiedCells.Contains(new Vector3Int(pos.x, pos.y, 0)))
                    return false;
            }

            if (HasBlockingCollider(area))
                return false;

            return true;
        }

        public void TakeArea(BoundsInt area)
        {
            foreach (var pos in area.allPositionsWithin)
            {
                occupiedCells.Add(new Vector3Int(pos.x, pos.y, 0));
            }

            // clear preview tiles so they don't stay forever
            SetTilesBlock(area, TileType.Empty, TempTilemap);
        }


        public void EnterBuildMode()
        {
            Character.instance.IsBusy = true;

            buildMode = true;

            mainTilemap.gameObject.SetActive(true);
            TempTilemap.gameObject.SetActive(true);

            lastAnchorCell = gridLayout.WorldToCell(buildAnchor.position);
            RebuildGrid(lastAnchorCell);
        }

        private void RebuildGrid(Vector3Int anchorCell)
        {
            if (buildBounds.size != Vector3Int.zero)
            {
                SetTilesBlock(buildBounds, TileType.Empty, TempTilemap);
            }

            Vector3Int size = new Vector3Int(buildGridSize.x, buildGridSize.y, 1);
            Vector3Int offset = new Vector3Int(-size.x / 2, -size.y / 2, 0);

            buildBounds = new BoundsInt(anchorCell + offset, size);

            FillWhiteOnlyOnEmpty(buildBounds);

            prevArea = new BoundsInt();
        }

        private void FillWhiteOnlyOnEmpty(BoundsInt area)
        {
            TileBase[] tiles = GetTilesBlock(area, mainTilemap);

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                    tiles[i] = tileBases[TileType.White];
            }

            mainTilemap.SetTilesBlock(area, tiles);
        }

        public void ExitBuildMode()
        {
            Character.instance.IsBusy = false;

            buildMode = false;

            SetTilesBlock(buildBounds, TileType.Empty, mainTilemap);
            SetTilesBlock(buildBounds, TileType.Empty, TempTilemap);

            mainTilemap.gameObject.SetActive(false);
            TempTilemap.gameObject.SetActive(false);

            buildBounds = new BoundsInt();
            prevArea = new BoundsInt();
        }

        public void ReleaseArea(BoundsInt area)
        {
            foreach (var pos in area.allPositionsWithin)
            {
                occupiedCells.Remove(new Vector3Int(pos.x, pos.y, 0));
            }
        }

        private bool HasBlockingCollider(BoundsInt area)
        {
            Vector3 center = gridLayout.CellToWorld(area.position)
                + Vector3.Scale(gridLayout.cellSize, (Vector3)area.size) * 0.5f;

            Vector3 sizeWorld = Vector3.Scale(gridLayout.cellSize, (Vector3)area.size);

            // ---- 2D CHECK (Collider2D) ----
            var filter = new ContactFilter2D();
            filter.useTriggers = false;

            // If you assigned blockingLayers, use it. If not, it will check everything.
            if (blockingLayers.value != 0)
            {
                filter.useLayerMask = true;
                filter.layerMask = blockingLayers;
            }

            Collider2D[] hits2D = new Collider2D[32];
            int count2D = Physics2D.OverlapBox(center, sizeWorld, 0f, filter, hits2D);

            for (int i = 0; i < count2D; i++)
            {
                var hit = hits2D[i];
                if (hit == null) continue;

                // ignore preview
                if (temp != null && hit.transform.IsChildOf(temp.transform)) continue;

                return true;
            }

            // ---- 3D CHECK (Collider) ----
            // if your world uses 3D colliders, THIS is the missing piece.
            Collider[] hits3D = Physics.OverlapBox(center, sizeWorld * 0.5f, Quaternion.identity);

            for (int i = 0; i < hits3D.Length; i++)
            {
                var hit = hits3D[i];
                if (hit == null) continue;

                // ignore preview
                if (temp != null && hit.transform.IsChildOf(temp.transform)) continue;

                // if you want to filter 3D layers too, check blockingLayers here:
                if (blockingLayers.value != 0 && ((blockingLayers.value & (1 << hit.gameObject.layer)) == 0))
                    continue;

                return true;
            }

            return false;
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
}