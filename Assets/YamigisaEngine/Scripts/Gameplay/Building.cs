using UnityEngine;

public class Building : MonoBehaviour
{
    public bool Placed;

    [Header("Make sure Z is > 0 to be rendered above the ground tiles")]
    public BoundsInt area;
    private void Awake()
    {
        if (area.size == Vector3Int.zero)
        {
            area = new BoundsInt(Vector3Int.zero, Vector3Int.one);
        }
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
        GridBuildingSystem.instance.TakeArea(areaTemp);
    }
}
