using UnityEngine;

[CreateAssetMenu(
    fileName = "NewProduct",
    menuName = "Little Market/Product"
)]
public class ProductData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField]
    private string productName = "New Product";

    [SerializeField]
    private string productID = "product_id";

    [SerializeField]
    private ProductCategory category;

    [SerializeField]
    private Sprite icon;

    [TextArea]
    [SerializeField]
    private string description = "";

    [Header("World")]
    [SerializeField]
    private PickupItem worldPrefab;

    [Header("Sales Shelf Display")]
    [Min(1)]
    [SerializeField]
    private int shelfColumns = 1;

    [Min(1)]
    [SerializeField]
    private int shelfVerticalLayers = 1;

    [Min(1)]
    [SerializeField]
    private int shelfDepthRows = 1;

    [SerializeField]
    private Vector3 shelfItemSpacing =
        new Vector3(
            0.2f,
            0.2f,
            0.2f
        );

    [SerializeField]
    private Vector3 shelfDisplayOffset =
        Vector3.zero;

    [Header("Economy")]
    [Min(0f)]
    [SerializeField]
    private float buyPrice = 1f;

    [Min(0f)]
    [SerializeField]
    private float sellPrice = 2f;

    [Header("Progression")]
    [Range(0f, 100f)]
    [SerializeField]
    private float requiredReputation = 0f;

    [Min(1)]
    [SerializeField]
    private int requiredStoreLevel = 1;

    public string ProductName =>
        productName;

    public string ProductID =>
        productID;

    public ProductCategory Category =>
        category;

    public Sprite Icon =>
        icon;

    public string Description =>
        description;

    public PickupItem WorldPrefab =>
        worldPrefab;

    public int SalesShelfCapacity =>
        Mathf.Max(1, shelfColumns)
        *
        Mathf.Max(1, shelfVerticalLayers)
        *
        Mathf.Max(1, shelfDepthRows);

    public float BuyPrice =>
        buyPrice;

    public float SellPrice =>
        sellPrice;

    public float RequiredReputation =>
        requiredReputation;

    public int RequiredStoreLevel =>
        Mathf.Max(
            1,
            requiredStoreLevel
        );

    public Vector3 GetSalesShelfLocalPosition(
        int index
    )
    {
        int columns =
            Mathf.Max(
                1,
                shelfColumns
            );

        int verticalLayers =
            Mathf.Max(
                1,
                shelfVerticalLayers
            );

        int depthRows =
            Mathf.Max(
                1,
                shelfDepthRows
            );

        int capacity =
            columns
            * verticalLayers
            * depthRows;

        int safeIndex =
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
        int column =
            safeIndex % columns;

        int remaining =
            safeIndex / columns;

        int depth =
            remaining % depthRows;

        int vertical =
            remaining / depthRows;

        /*
         * X and Z are centred around the ShelfSlot.
         *
         * Vertical layers always build upward
         * from the shelf surface.
         */
        float centeredColumn =
            column
            - (columns - 1) * 0.5f;

        float centeredDepth =
            depth
            - (depthRows - 1) * 0.5f;

        Vector3 position =
            shelfDisplayOffset;

        position.x +=
            centeredColumn
            * shelfItemSpacing.x;

        position.y +=
            vertical
            * shelfItemSpacing.y;

        position.z +=
            centeredDepth
            * shelfItemSpacing.z;

        return position;
    }
}