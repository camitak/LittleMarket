using UnityEngine;

[DisallowMultipleComponent]
public class StoreExpansionZone : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private StoreExpansionDefinition definition;

    [Header("Scene Geometry")]
    [SerializeField] private GameObject unlockedGeometryRoot;
    [SerializeField] private GameObject lockedBarrier;
    [SerializeField] private Collider buildFloor;

    public StoreExpansionDefinition Definition => definition;
    public Collider BuildFloor => buildFloor;
    public bool IsUnlocked { get; private set; }

    public bool IsConfigured =>
        definition != null &&
        definition.IsConfigured &&
        unlockedGeometryRoot != null &&
        buildFloor != null;

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

        if (buildFloor != null)
            buildFloor.enabled = unlocked;
    }
}