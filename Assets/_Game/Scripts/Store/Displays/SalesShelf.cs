using UnityEngine;

[DisallowMultipleComponent]
public class SalesShelf : MonoBehaviour
{
    [Header("Display Rules")]
    [SerializeField] private ProductDisplayType displayType = ProductDisplayType.StandardShelf;
    [SerializeField] private ProductStorageCondition storageCondition = ProductStorageCondition.Ambient;

    [Header("Customer")]
    [SerializeField] private Transform customerStandPoint;

    [Header("Access - Front Is Local Negative Z")]
    [Min(0f)] [SerializeField] private float frontSideTolerance = 0.10f;

    public ProductDisplayType DisplayType => displayType;
    public ProductStorageCondition StorageCondition => storageCondition;
    public Transform CustomerStandPoint => customerStandPoint;

    public bool CanDisplayProduct(ProductData productData)
    {
        if (productData == null)
            return false;

        return productData.CanBeDisplayedOn(displayType) &&
               productData.CanBeStoredIn(storageCondition);
    }

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