using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_Expansion_",
    menuName = "Little Market/Store/Expansion Definition"
)]
public class StoreExpansionDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string expansionID = "expansion_id";
    [SerializeField] private string displayName = "Store Expansion";

    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Header("Purchase")]
    [Min(0f)]
    [SerializeField] private float purchasePrice = 100f;

    [Min(1)]
    [SerializeField] private int requiredStoreLevel = 1;

    public string ExpansionID => expansionID;
    public string DisplayName => displayName;
    public string Description => description;
    public float PurchasePrice => Mathf.Max(0f, purchasePrice);
    public int RequiredStoreLevel => Mathf.Max(1, requiredStoreLevel);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(expansionID) &&
        !string.IsNullOrWhiteSpace(displayName);
}