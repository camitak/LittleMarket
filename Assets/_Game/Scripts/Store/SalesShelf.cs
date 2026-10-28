using UnityEngine;

[DisallowMultipleComponent]
public class SalesShelf : MonoBehaviour
{
    [Header("Shelf Rules")]
    [SerializeField] private ProductCategory allowedCategory = ProductCategory.Cereal;

    [Header("Customer")]
    [SerializeField] private Transform customerStandPoint;

    [Header("Access - Front Is Local Negative Z")]
    [Min(0f)] [SerializeField] private float frontSideTolerance = 0.10f;

    public ProductCategory AllowedCategory => allowedCategory;
    public Transform CustomerStandPoint => customerStandPoint;

    // PF_Shelf's slots and Customer_Point are on local -Z.
    // Check a horizontal plane so camera height never changes shelf access.
    public bool IsWorldPositionInFront(Vector3 worldPosition)
    {
        Vector3 towardActor = worldPosition - transform.position;
        towardActor.y = 0f;
        Vector3 front = -transform.forward;
        front.y = 0f;
        return Vector3.Dot(towardActor, front.normalized) > frontSideTolerance;
    }

    private void OnDrawGizmosSelected()
    {
        if (customerStandPoint == null)
            return;
        PlacementUtility.DrawFrameGizmo(customerStandPoint, 0.2f);
    }
}