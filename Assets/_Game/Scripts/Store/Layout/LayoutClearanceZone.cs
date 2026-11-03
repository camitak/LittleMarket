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

    public bool BlocksFootprint(StoreAreaType placementArea,
        Vector3 furniturePosition,
        Quaternion furnitureRotation,
        Vector3 furnitureOffset,
        Vector3 furnitureHalfExtents,
        bool isAccessFootprint)
    {
        if (placementArea != areaType)
            return false;

        if (isAccessFootprint && !blockFurnitureAccess)
        {
            return false;
        }

        if (!isAccessFootprint && !blockFurnitureBody)
        {
            return false;
        }

        Vector3 zoneCenter3D = transform.TransformPoint(center);

        Vector3 furnitureCenter3D = furniturePosition + furnitureRotation * furnitureOffset;

        Vector2 zoneCenter = new(zoneCenter3D.x, zoneCenter3D.z);

        Vector2 furnitureCenter = new(furnitureCenter3D.x, furnitureCenter3D.z);

        Vector3 scale = transform.lossyScale;

        Vector2 zoneHalf = new(halfExtents.x * Mathf.Abs(scale.x), halfExtents.z * Mathf.Abs(scale.z));

        Vector2 furnitureHalf = new(Mathf.Abs(furnitureHalfExtents.x), Mathf.Abs(furnitureHalfExtents.z));

        Vector2 zoneRight = ToHorizontalAxis(transform.right);

        Vector2 zoneForward = ToHorizontalAxis(transform.forward);

        Vector2 furnitureRight = ToHorizontalAxis(furnitureRotation * Vector3.right);

        Vector2 furnitureForward = ToHorizontalAxis(furnitureRotation * Vector3.forward);

        Vector2 difference = furnitureCenter - zoneCenter;

        if (IsSeparated(difference, zoneRight, zoneRight, zoneForward, zoneHalf, furnitureRight, furnitureForward,
                furnitureHalf))
        {
            return false;
        }

        if (IsSeparated(difference, zoneForward, zoneRight, zoneForward, zoneHalf, furnitureRight, furnitureForward,
                furnitureHalf))
        {
            return false;
        }

        if (IsSeparated(difference, furnitureRight, zoneRight, zoneForward, zoneHalf, furnitureRight, furnitureForward,
                furnitureHalf))
        {
            return false;
        }

        if (IsSeparated(difference, furnitureForward, zoneRight, zoneForward, zoneHalf, furnitureRight,
                furnitureForward, furnitureHalf))
        {
            return false;
        }

        return true;
    }

    private static bool IsSeparated(Vector2 difference,
        Vector2 axis,
        Vector2 firstRight,
        Vector2 firstForward,
        Vector2 firstHalf,
        Vector2 secondRight,
        Vector2 secondForward,
        Vector2 secondHalf)
    {
        float centerDistance = Mathf.Abs(Vector2.Dot(difference, axis));

        float firstRadius = firstHalf.x * Mathf.Abs(Vector2.Dot(firstRight, axis)) +
                            firstHalf.y * Mathf.Abs(Vector2.Dot(firstForward, axis));

        float secondRadius = secondHalf.x * Mathf.Abs(Vector2.Dot(secondRight, axis)) +
                             secondHalf.y * Mathf.Abs(Vector2.Dot(secondForward, axis));

        return centerDistance > firstRadius + secondRadius;
    }

    private static Vector2 ToHorizontalAxis(Vector3 direction)
    {
        Vector2 axis = new(direction.x, direction.z);

        if (axis.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        return axis.normalized;
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