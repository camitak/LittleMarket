using UnityEngine;

[DisallowMultipleComponent]
public class StoreExpansionZone : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private StoreExpansionDefinition definition;

    [Header("Scene")]
    [SerializeField] private GameObject unlockedGeometryRoot;
    [SerializeField] private GameObject lockedBarrier;
    [SerializeField] private StoreBuildZone buildZone;

    public StoreExpansionDefinition Definition => definition;
    public StoreBuildZone BuildZone => buildZone;
    public bool IsUnlocked { get; private set; }

    public bool IsConfigured =>
        definition != null &&
        definition.IsConfigured &&
        unlockedGeometryRoot != null &&
        buildZone != null;

    private void Awake()
    {
        SetUnlocked(false);
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;

        if (lockedBarrier != null)
            lockedBarrier.SetActive(!unlocked);

        if (unlockedGeometryRoot != null)
            unlockedGeometryRoot.SetActive(unlocked);

        if (buildZone != null)
            buildZone.SetAvailable(unlocked);
    }
}