using UnityEngine;

namespace Yamigisa
{
    public class Building : MonoBehaviour
    {
        public bool Placed;

        [Header("Make sure Z is > 0 to be rendered above the ground tiles")]
        public BoundsInt area;
        private BoundsInt placedArea;

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
            Vector3Int positionInt = GridBuildingSystem.instance.gridLayout.LocalToCell(transform.position);
            BoundsInt areaTemp = area;
            areaTemp.position = positionInt;

            Placed = true;
            placedArea = areaTemp;

            GridBuildingSystem.instance.TakeArea(areaTemp);
        }
    }
}