using UnityEngine;

[DisallowMultipleComponent]
public class SalesShelf : MonoBehaviour
{
    [Header("Shelf Rules")]
    [SerializeField] private ProductCategory allowedCategory = ProductCategory.Cereal;

    [Header("Customer")]
    [SerializeField] private Transform customerStandPoint;

    public ProductCategory AllowedCategory => allowedCategory;
    public Transform CustomerStandPoint => customerStandPoint;

    private void OnDrawGizmosSelected()
    {
        if (customerStandPoint == null)
        {
            return;
        }

        PlacementUtility.DrawFrameGizmo(customerStandPoint, 0.2f);
    }
}