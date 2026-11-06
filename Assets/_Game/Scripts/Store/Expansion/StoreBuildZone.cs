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
        {
            floorCollider.enabled = available;
        }
    }

    /*
     * Returns the authored world-space floor bounds
     * even when this build zone is currently locked.
     *
     * Collider.bounds cannot be relied on while the
     * collider or its parent hierarchy is disabled.
     *
     * Our build floors are BoxColliders, so derive
     * the bounds from the authored box geometry
     * directly.
     */
    public bool TryGetAuthoredBounds(out Bounds bounds)
    {
        bounds = default;

        if (floorCollider == null)
        {
            return false;
        }

        if (floorCollider is BoxCollider box)
        {
            Transform boxTransform = box.transform;

            Vector3 half = box.size * 0.5f;

            bool initialized = false;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = box.center + new Vector3(half.x * x, half.y * y, half.z * z);

                        Vector3 worldCorner = boxTransform.TransformPoint(localCorner);

                        if (!initialized)
                        {
                            bounds = new Bounds(worldCorner, Vector3.zero);

                            initialized = true;

                            continue;
                        }

                        bounds.Encapsulate(worldCorner);
                    }
                }
            }

            return initialized;
        }

        /*
         * Fallback for future collider types that
         * happen to be currently active.
         */
        if (!floorCollider.enabled || !floorCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        bounds = floorCollider.bounds;

        return bounds.size.sqrMagnitude > 0.0001f;
    }
}