using UnityEngine;

[DisallowMultipleComponent]
public class StoreBuildZone : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private StoreAreaType areaType = StoreAreaType.SalesFloor;

    [Header("Floor")]
    [SerializeField] private Collider floorCollider;
    [SerializeField] private bool availableAtStart = true;

    public StoreAreaType AreaType => areaType;
    public Collider FloorCollider => floorCollider;
    public bool IsAvailable { get; private set; }

    private void Awake()
    {
        SetAvailable(availableAtStart);
    }

    public void SetAvailable(bool available)
    {
        IsAvailable = available;

        if (floorCollider != null)
            floorCollider.enabled = available;
    }
}