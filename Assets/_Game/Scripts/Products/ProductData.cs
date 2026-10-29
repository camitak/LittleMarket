using UnityEngine;

[CreateAssetMenu(
    fileName = "NewProduct",
    menuName = "Little Market/Product"
)]
public class ProductData : ScriptableObject
{
    [Header("Basic Information")] 
    [SerializeField] private string productName = "New Product";
    [SerializeField] private string productID = "product_id";
    [SerializeField] private ProductCategory category;
    [SerializeField] private Sprite icon;
    [TextArea] [SerializeField] private string description = "";

    [Header("World")] 
    [SerializeField] private PickupItem worldPrefab;

    [Header("Storage")] 
    [SerializeField] private ProductStorageCondition requiredStorageCondition = ProductStorageCondition.Ambient;

    [Header("Sales Shelf Display")] 
    [Min(1)] [SerializeField] private int shelfColumns = 1;
    [Min(1)] [SerializeField] private int shelfVerticalLayers = 1;
    [Min(1)] [SerializeField] private int shelfDepthRows = 1;
    [SerializeField] private Vector3 shelfItemSpacing =
        new(
            0.2f,
            0.2f,
            0.2f
        );
    [SerializeField] private Vector3 shelfDisplayOffset = Vector3.zero;

    [Header("Economy")] 
    [Min(0f)] [SerializeField] private float buyPrice = 1f;
    [Min(0f)] [SerializeField] private float sellPrice = 2f;
    
    [Header("Progression")] 
    [Range(0f, 100f)] [SerializeField] private float requiredReputation = 0f;
    [Min(1)] [SerializeField] private int requiredStoreLevel = 1;

    public string ProductName => productName;

    public string ProductID => productID;

    public ProductCategory Category => category;

    public Sprite Icon => icon;

    public string Description => description;

    public PickupItem WorldPrefab => worldPrefab;

    public ProductStorageCondition RequiredStorageCondition => requiredStorageCondition;

    public int SalesShelfCapacity =>
        Mathf.Max(1, shelfColumns)
        *
        Mathf.Max(1, shelfVerticalLayers)
        *
        Mathf.Max(1, shelfDepthRows);

    public float BuyPrice => buyPrice;

    public float SellPrice => sellPrice;

    public float RequiredReputation => requiredReputation;

    public int RequiredStoreLevel =>
        Mathf.Max(
            1,
            requiredStoreLevel
        );

    public bool CanBeStoredIn(ProductStorageCondition storageCondition)
    {
        switch (requiredStorageCondition)
        {
            case ProductStorageCondition.Ambient:
                /*
                 * Chilled equipment can safely satisfy
                 * an ambient product's requirement too.
                 */
                return true;

            case ProductStorageCondition.Chilled:
                return storageCondition == ProductStorageCondition.Chilled;

            default:
                return false;
        }
    }

    public Vector3 GetSalesShelfLocalPosition(int index)
    {
        var columns =
            Mathf.Max(
                1,
                shelfColumns
            );

        var verticalLayers =
            Mathf.Max(
                1,
                shelfVerticalLayers
            );

        var depthRows =
            Mathf.Max(
                1,
                shelfDepthRows
            );

        var capacity =
            columns
            * verticalLayers
            * depthRows;

        var safeIndex =
            Mathf.Clamp(
                index,
                0,
                capacity - 1
            );

        /*
         * Fill order:
         *
         * across first,
         * then front/back,
         * then upward.
         */
        var column = safeIndex % columns;

        var remaining = safeIndex / columns;

        var depth = remaining % depthRows;

        var vertical = remaining / depthRows;

        /*
         * X and Z are centred around the ShelfSlot.
         *
         * Vertical layers always build upward
         * from the shelf surface.
         */
        var centeredColumn = column - (columns - 1) * 0.5f;

        var centeredDepth = depth - (depthRows - 1) * 0.5f;

        var position = shelfDisplayOffset;

        position.x += centeredColumn * shelfItemSpacing.x;

        position.y += vertical * shelfItemSpacing.y;

        position.z += centeredDepth * shelfItemSpacing.z;

        return position;
    }
}