using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace Yamigisa
{
    public class PlaceableSystem : MonoBehaviour
    {
        [Header("Mode")]
        public bool useGrid = true; // <==== NEW: if false = free placement anywhere

        [Header("Grid")]
        public GridLayout gridLayout;

        [Header("Tilemap")]
        public Tilemap mainTilemap;
        public Tilemap TempTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase whiteTile;
        [SerializeField] private TileBase greenTile;
        [SerializeField] private TileBase redTile;

        [Header("Grid Size")]
        [SerializeField] private Vector2Int buildGridSize = new Vector2Int(10, 10);

        //[SerializeField] private LayerMask blockingLayers;

        private float interactionBlockTimer = 0f;
        [SerializeField] private float interactionBlockDuration = 0.2f;
        private bool buildMode;
        private BoundsInt buildBounds;

        private Transform buildAnchor;

        private static Dictionary<TileType, TileBase> tileBases = new Dictionary<TileType, TileBase>();

        private Placeable temp;
        private Vector3 prevPos;
        private BoundsInt prevArea;

        private Vector3Int lastAnchorCell;
        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

        public bool IsInBuildMode => buildMode;
        public bool IsInteractionBlocked => interactionBlockTimer > 0f;
        public static PlaceableSystem instance;
        public bool IsPlacingObject => temp != null;
        private ItemSlot sourceBuildSlot;
        private void Awake()
        {
            instance = this;
        }

        public void Setup()
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
            if (buildMode &&
    Character.instance.characterControls.IsPressedDown(
        Character.instance.characterControls.cancel))
            {
                CancelBuild();
                return;
            }

            // 🔥 TIMER MUST RUN ALWAYS
            if (interactionBlockTimer > 0f)
                interactionBlockTimer -= Time.deltaTime;

            if (!buildMode)
                return;

            // ================= FREE PLACEMENT =================
            if (!useGrid)
            {
                if (!temp || temp.Placed)
                    return;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = temp.transform.position.z;
                temp.transform.position = mouseWorld;

                bool canPlace = !HasBlockingColliderWorld(temp);
                temp.SetSpriteColor(canPlace);

                if (Input.GetMouseButtonDown(0))
                {
                    if (!canPlace)
                    {
                        Debug.Log("Cannot place here (blocked by collider)");
                        return;
                    }

                    temp.Place();
                    ConsumeBuildItemIfNeeded();
                    temp = null;

                    interactionBlockTimer = interactionBlockDuration;

                    ExitBuildMode();
                    return;
                }

                if (Character.instance.characterControls.IsPressedDown(
           Character.instance.characterControls.cancel))
                {
                    ClearArea();

                    if (temp != null)
                        Destroy(temp.gameObject);

                    temp = null;

                    ExitBuildMode();
                    return;
                }

                return;
            }

            // ================= GRID MODE =================

            Vector3Int currentAnchorCell = gridLayout.WorldToCell(buildAnchor.position);

            if (currentAnchorCell != lastAnchorCell)
            {
                lastAnchorCell = currentAnchorCell;
                RebuildGrid(currentAnchorCell);
                prevArea = new BoundsInt();
            }

            if (!temp || temp.Placed)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mouseWorldGrid = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = gridLayout.WorldToCell(mouseWorldGrid);

            if (prevPos != cellPos)
            {
                Vector3 worldPos =
                    gridLayout.CellToWorld(cellPos) +
                    new Vector3(
                        gridLayout.cellSize.x * 0.5f,
                        gridLayout.cellSize.y * 0.5f,
                        0f
                    );

                temp.transform.position = worldPos;

                prevPos = cellPos;
                FollowBuilding();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (!CanTakeArea(prevArea))
                {
                    Debug.Log("Cannot place here (area blocked)");
                    return;
                }

                temp.Place();
                ConsumeBuildItemIfNeeded();
                temp = null;

                interactionBlockTimer = interactionBlockDuration;

                ExitBuildMode();
                return;
            }
        }

        private void CancelBuild()
        {
            if (temp != null)
            {
                Destroy(temp.gameObject);
                temp = null;
            }

            // no refund needed, item was never removed
            sourceBuildSlot = null;

            ExitBuildMode();
        }

        public void InitializeBuilding(GameObject Placeable, ItemSlot sourceSlot)
        {
            sourceBuildSlot = sourceSlot;  // remember which slot triggered build
            InitializeBuilding(Placeable); // call your existing method
            GameManager.instance.SetCanPause(false);
        }

        private void ConsumeBuildItemIfNeeded()
        {
            if (sourceBuildSlot == null) return;

            // Consume ONE item only after successful placement
            Inventory.Instance.ReduceSlotAmount(sourceBuildSlot);

            sourceBuildSlot = null;
        }

        public void InitializeBuilding(GameObject Placeable)
        {
            if (buildMode)
                ExitBuildMode();

            EnterBuildMode();

            temp = Instantiate(
      Placeable,
      Vector3.zero,
      Quaternion.identity
  ).GetComponent<Placeable>();

            temp.transform.position = buildAnchor.position;

            // Only do grid preview if using grid
            if (useGrid)
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
            // If not using grid, do nothing (no preview tiles)
            if (!useGrid) return;

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
            // If not using grid, grid rules are not applied
            if (!useGrid)
                return true;

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
            // If not using grid, do nothing
            if (!useGrid)
                return;

            foreach (var pos in area.allPositionsWithin)
            {
                occupiedCells.Add(new Vector3Int(pos.x, pos.y, 0));
            }

            // clear preview tiles so they don't stay forever
            SetTilesBlock(area, TileType.Empty, TempTilemap);
        }


        public void EnterBuildMode()
        {
            Character.instance.SetCharacterBusy(true);
            GameManager.instance.SetCanPause(false);
            Inventory.Instance.HideInventory();

            buildMode = true;

            // If not using grid -> don't show tilemaps / don't rebuild grid bounds
            if (!useGrid)
                return;

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
            buildMode = false;

            // If not using grid -> just ensure maps are off and reset vars
            if (!useGrid)
            {
                mainTilemap.gameObject.SetActive(false);
                TempTilemap.gameObject.SetActive(false);

                buildBounds = new BoundsInt();
                prevArea = new BoundsInt();
                return;
            }

            SetTilesBlock(buildBounds, TileType.Empty, mainTilemap);
            SetTilesBlock(buildBounds, TileType.Empty, TempTilemap);

            mainTilemap.gameObject.SetActive(false);
            TempTilemap.gameObject.SetActive(false);

            buildBounds = new BoundsInt();
            prevArea = new BoundsInt();

            interactionBlockTimer = interactionBlockDuration;
            GameManager.instance.SetCanPause(true);
            Character.instance.SetCharacterBusy(false);
        }

        public void ReleaseArea(BoundsInt area)
        {
            // If not using grid, occupiedCells isn't used
            if (!useGrid)
                return;

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
            Collider[] hits3D = Physics.OverlapBox(center, sizeWorld * 0.5f, Quaternion.identity);

            for (int i = 0; i < hits3D.Length; i++)
            {
                var hit = hits3D[i];
                if (hit == null) continue;

                // ignore preview
                if (temp != null && hit.transform.IsChildOf(temp.transform)) continue;

                return true;
            }

            return false;
        }

        // ===== NEW: World-space collider check for free placement =====
        private bool HasBlockingColliderWorld(Placeable b)
        {
            if (b == null) return false;

            // Use any collider found in children (3D)
            Collider col3D = b.GetComponentInChildren<Collider>();
            if (col3D != null)
            {
                Bounds bounds = col3D.bounds;
                Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, col3D.transform.rotation);

                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit == null) continue;

                    // ignore self
                    if (hit.transform.IsChildOf(b.transform)) continue;

                    return true;
                }
            }

            // Also support 2D colliders
            Collider2D col2D = b.GetComponentInChildren<Collider2D>();
            if (col2D != null)
            {
                Bounds bounds = col2D.bounds;
                var filter = new ContactFilter2D();
                filter.useTriggers = false;

                Collider2D[] hits2D = new Collider2D[32];
                int count2D = Physics2D.OverlapBox((Vector2)bounds.center, (Vector2)bounds.size, 0f, filter, hits2D);

                for (int i = 0; i < count2D; i++)
                {
                    var hit = hits2D[i];
                    if (hit == null) continue;

                    // ignore self
                    if (hit.transform.IsChildOf(b.transform)) continue;

                    return true;
                }
            }

            return false;
        }

        public Vector3 GetCellCenterWorld(BoundsInt area)
        {
            Vector3Int cellPos = area.position;

            Vector3 basePos = gridLayout.CellToWorld(cellPos);

            Vector3 offset = new Vector3(
                area.size.x * 0.5f,
                area.size.y * 0.5f,
                0f
            );

            return basePos + offset;
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