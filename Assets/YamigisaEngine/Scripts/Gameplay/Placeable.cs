using UnityEngine;

namespace Yamigisa
{
    [ExecuteAlways]
    public class Placeable : MonoBehaviour
    {
        public bool Placed;

        [Header("Make sure Z is > 0 to be rendered above the ground tiles")]
        public BoundsInt area;
        private BoundsInt placedArea;

        [Header("For non-grid placement mode")]
        [SerializeField] private Color cantPlaceColor = Color.red;
        [SerializeField] private Color defaultColor = Color.white;
        private SpriteRenderer spriteRenderer;

        private void OnValidate()
        {
            if (area.size == Vector3Int.zero)
                return;

            // Editor-only safety
            if (Application.isPlaying)
                return;
        }

        private void Awake()
        {
            if (area.size == Vector3Int.zero)
            {
                area = new BoundsInt(Vector3Int.zero, Vector3Int.one);
            }

            // temporary force for chest-sized placeables
            area = new BoundsInt(Vector3Int.zero, new Vector3Int(1, 1, 1));

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnDestroy()
        {
            if (!Placed) return;
            if (PlaceableSystem.instance == null) return;

            // NEW: only release area if grid mode is used
            if (!PlaceableSystem.instance.useGrid) return;

            PlaceableSystem.instance.ReleaseArea(placedArea);
        }

        public void Place()
        {
            // NEW: if not using grid, do not snap or take area
            if (PlaceableSystem.instance != null && !PlaceableSystem.instance.useGrid)
            {
                Placed = true;
                return;
            }

            Vector3Int positionInt =
     PlaceableSystem.instance.gridLayout.WorldToCell(transform.position);

            BoundsInt areaTemp = area;
            areaTemp.position = positionInt;

            Vector3 worldPos = PlaceableSystem.instance.gridLayout.CellToWorld(positionInt);

            if (area.size.x <= 1 && area.size.y <= 1)
            {
                worldPos += new Vector3(
                    PlaceableSystem.instance.gridLayout.cellSize.x * 0.5f,
                    PlaceableSystem.instance.gridLayout.cellSize.y * 0.5f,
                    0f
                );
            }
            else
            {
                worldPos += new Vector3(
                    PlaceableSystem.instance.gridLayout.cellSize.x * area.size.x * 0.5f,
                    PlaceableSystem.instance.gridLayout.cellSize.y * area.size.y * 0.5f,
                    0f
                );
            }

            transform.position = worldPos;

            Placed = true;
            placedArea = areaTemp;

            PlaceableSystem.instance.TakeArea(areaTemp);
        }

        public void SetSpriteColor(bool canPlace)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null)
                return;

            spriteRenderer.color = canPlace ? defaultColor : cantPlaceColor;
        }
    }
}