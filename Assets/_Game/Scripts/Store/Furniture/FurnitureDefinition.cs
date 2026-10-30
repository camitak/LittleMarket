using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_Furniture_",
    menuName = "Little Market/Furniture/Furniture Definition"
)]
public class FurnitureDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string furnitureID = "furniture_id";
    [SerializeField] private string displayName = "Furniture";
    [TextArea(2, 4)] [SerializeField] private string description = "";

    [Header("Purchase")]
    [Min(0f)] [SerializeField] private float purchasePrice = 40f;
    [SerializeField] private ShelfIdentity prefab;

    [Header("Placement - Body")]
    [SerializeField] private Vector3 bodyHalfExtents = new(1.30f, 1.05f, 0.52f);
    [SerializeField] private Vector3 bodyCenter = new(0f, 1.10f, 0f);

    [Header("Placement - Front Access")]
    [SerializeField] private Vector3 accessHalfExtents = new(1.25f, 0.75f, 0.50f);
    [SerializeField] private Vector3 accessCenter = new(0f, 0.80f, -1.02f);

    public string FurnitureID => furnitureID;
    public string DisplayName => displayName;
    public string Description => description;
    public float PurchasePrice => Mathf.Max(0f, purchasePrice);
    public ShelfIdentity Prefab => prefab;

    public Vector3 BodyHalfExtents => bodyHalfExtents;
    public Vector3 BodyCenter => bodyCenter;
    public Vector3 AccessHalfExtents => accessHalfExtents;
    public Vector3 AccessCenter => accessCenter;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(furnitureID) &&
        prefab != null;
}