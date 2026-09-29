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
    private string description;

    [Header("World")]
    [SerializeField]
    private PickupItem worldPrefab;

    [Header("Economy")]
    [SerializeField]
    private float buyPrice = 1f;

    [SerializeField]
    private float sellPrice = 2f;

    [Header("Progression")]
    [Range(0f, 100f)]
    [SerializeField]
    private float requiredReputation = 0f;

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

    public float BuyPrice =>
        buyPrice;

    public float SellPrice =>
        sellPrice;

    public float RequiredReputation =>
        requiredReputation;
}