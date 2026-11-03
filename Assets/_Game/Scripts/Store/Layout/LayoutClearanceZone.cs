using UnityEngine;

[DisallowMultipleComponent]
public class LayoutClearanceZone : MonoBehaviour
{
    [Header("Area")] 
    [SerializeField] private StoreAreaType areaType = StoreAreaType.SalesFloor;

    [Header("Local Footprint")] 
    [SerializeField] private Vector3 center = new(0f, 0.75f, 0f);
    [SerializeField] private Vector3 halfExtents = new(0.5f, 0.75f, 0.5f);

    [Header("Placement Rules")] 
    [SerializeField] private bool blockFurnitureBody = true;
    [SerializeField] private bool blockFurnitureAccess = true;

    public StoreAreaType AreaType => areaType;
    public Vector3 Center => center;
    public Vector3 HalfExtents => halfExtents;
    public bool BlocksFurnitureBody => blockFurnitureBody;
    public bool BlocksFurnitureAccess => blockFurnitureAccess;

    public bool BlocksFootprint(StoreAreaType placementArea,
        Vector3 furniturePosition,
        Quaternion furnitureRotation,
        Vector3 furnitureOffset,
        Vector3 furnitureHalfExtents,
        bool isAccessFootprint)
    {
        if (placementArea != areaType)
        {
            return false;
        }

        if (isAccessFootprint && !blockFurnitureAccess)
        {
            return false;
        }

        if (!isAccessFootprint && !blockFurnitureBody)
        {
            return false;
        }

        Vector3 zoneWorldCenter = transform.TransformPoint(center);

        Vector3 zoneScale = transform.lossyScale;

        Vector2 zoneHalf = new(halfExtents.x * Mathf.Abs(zoneScale.x), halfExtents.z * Mathf.Abs(zoneScale.z));

        Vector3 furnitureWorldCenter = furniturePosition + furnitureRotation * furnitureOffset;

        Vector2 furnitureHalf = new(Mathf.Abs(furnitureHalfExtents.x), Mathf.Abs(furnitureHalfExtents.z));

        return LayoutFootprintUtility.OverlapsHorizontal(zoneWorldCenter, transform.rotation, zoneHalf,
            furnitureWorldCenter, furnitureRotation, furnitureHalf);
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);

        Gizmos.DrawWireCube(center, halfExtents * 2f);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}