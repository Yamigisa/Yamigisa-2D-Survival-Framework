using UnityEngine;

namespace Yamigisa
{
    [ExecuteAlways]
    public class Building : MonoBehaviour
    {
        public bool Placed;

        [Header("Make sure Z is > 0 to be rendered above the ground tiles")]
        public BoundsInt area;
        private BoundsInt placedArea;

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
        }

        private void OnDestroy()
        {
            if (!Placed) return;
            if (GridBuildingSystem.instance == null) return;

            GridBuildingSystem.instance.ReleaseArea(placedArea);
        }

        public bool CanBePlaced()
        {
            Vector3Int positionInt = GridBuildingSystem.instance.gridLayout.LocalToCell(transform.position);
            BoundsInt areaTemp = area;
            areaTemp.position = positionInt;

            if (GridBuildingSystem.instance.CanTakeArea(areaTemp))
            {
                return true;
            }

            return false;
        }

        public void Place()
        {
            Vector3Int positionInt =
                GridBuildingSystem.instance.gridLayout.LocalToCell(transform.position);

            BoundsInt areaTemp = area;
            areaTemp.position = positionInt;

            Vector3 worldPos =
                GridBuildingSystem.instance.gridLayout.CellToWorld(positionInt)
                + new Vector3(
                    area.size.x * 0.5f,
                    area.size.y * 0.5f,
                    0f
                );

            transform.position = worldPos;

            Placed = true;
            placedArea = areaTemp;

            GridBuildingSystem.instance.TakeArea(areaTemp);
        }

        private void OnDrawGizmos()
        {
            if (area.size == Vector3Int.zero) return;

            GridLayout grid = null;

            // Play mode → use singleton
            if (Application.isPlaying && GridBuildingSystem.instance != null)
            {
                grid = GridBuildingSystem.instance.gridLayout;
            }
            // Edit mode → find grid in scene
            else
            {
                grid = FindObjectOfType<GridLayout>();
            }

            if (grid == null) return;

            Vector3Int baseCell = grid.WorldToCell(transform.position);

            Gizmos.color = Color.cyan;

            for (int x = 0; x < area.size.x; x++)
            {
                for (int y = 0; y < area.size.y; y++)
                {
                    Vector3Int cell = baseCell + new Vector3Int(x, y, 0);

                    Vector3 cellWorld = grid.CellToWorld(cell);
                    Vector3 cellSize = grid.cellSize;

                    Gizmos.DrawWireCube(
                        cellWorld + cellSize * 0.5f,
                        cellSize
                    );
                }
            }
        }
    }
}