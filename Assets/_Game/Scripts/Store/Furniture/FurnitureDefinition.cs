
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Furniture_", menuName = "Little Market/Furniture/Furniture Definition")]
public class FurnitureDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string furnitureID = "furniture_id";
    [SerializeField] private string displayName = "Furniture";
    [TextArea(2, 4)] [SerializeField] private string description = "";

    [Header("Catalog")]
    [SerializeField] private FurnitureCatalogCategory catalogCategory = FurnitureCatalogCategory.Fixtures;

    [Header("Availability")]
    [SerializeField] private bool purchasable = true;
    [SerializeField] private bool sellable = true;
    [Min(0)] [SerializeField] private int maxOwned;

    [Header("Purchase")]
    [Min(0f)] [SerializeField] private float purchasePrice = 40f;
    [SerializeField] private FurnitureIdentity prefab;

    [Header("Placement")]
    [SerializeField] private StoreAreaType placementArea = StoreAreaType.SalesFloor;
    [SerializeField] private FurnitureAccessRequirement accessRequirement = FurnitureAccessRequirement.FrontOperational;

    [Header("Placement - Body")]
    [SerializeField] private Vector3 bodyHalfExtents = new(1.30f, 1.05f, 0.52f);
    [SerializeField] private Vector3 bodyCenter = new(0f, 1.10f, 0f);

    [Header("Placement - Front Access")]
    [SerializeField] private Vector3 accessHalfExtents = new(1.25f, 0.75f, 0.50f);
    [SerializeField] private Vector3 accessCenter = new(0f, 0.80f, -1.02f);

    [Header("Placement - Staff Access")]
    [SerializeField] private bool requiresStaffAccess;
    [SerializeField] private Vector3 staffAccessCenter = new(0f, 0.75f, 0.95f);
    [SerializeField] private Vector3 staffAccessHalfExtents = new(0.85f, 0.75f, 0.55f);

    public string FurnitureID => furnitureID;
    public string DisplayName => displayName;
    public string Description => description;
    public FurnitureCatalogCategory CatalogCategory => catalogCategory;

    public bool Purchasable => purchasable;
    public bool Sellable => sellable;
    public int MaxOwned => Mathf.Max(0, maxOwned);

    public float PurchasePrice => Mathf.Max(0f, purchasePrice);
    public FurnitureIdentity Prefab => prefab;

    public StoreAreaType PlacementArea => placementArea;
    public FurnitureAccessRequirement AccessRequirement => accessRequirement;
    public bool RequiresOperationalAccess => accessRequirement == FurnitureAccessRequirement.FrontOperational;

    public Vector3 BodyHalfExtents => bodyHalfExtents;
    public Vector3 BodyCenter => bodyCenter;
    public Vector3 AccessHalfExtents => accessHalfExtents;
    public Vector3 AccessCenter => accessCenter;

    public bool RequiresStaffAccess => requiresStaffAccess;
    public Vector3 StaffAccessCenter => staffAccessCenter;
    public Vector3 StaffAccessHalfExtents => staffAccessHalfExtents;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(furnitureID) && prefab != null;
}
