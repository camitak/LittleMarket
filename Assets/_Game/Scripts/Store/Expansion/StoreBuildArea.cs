using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreBuildArea : MonoBehaviour
{
    [Header("Base Store")]
    [SerializeField] private Collider baseFloor;

    [Header("Expansion Zones")]
    [SerializeField] private StoreExpansionZone[] expansionZones;

    public bool TryGetPlacementSurface(
        Ray ray,
        float maxDistance,
        out Vector3 surfacePoint)
    {
        surfacePoint = Vector3.zero;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (!IsBuildFloorCollider(hit.collider))
                continue;

            surfacePoint = hit.point;
            surfacePoint.y = hit.collider.bounds.max.y;
            return true;
        }

        return false;
    }

    public bool IsFootprintInside(
        Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents)
    {
        float halfX = Mathf.Max(0.01f, halfExtents.x);
        float halfZ = Mathf.Max(0.01f, halfExtents.z);

        // Nine samples instead of only four corners.
        // This also handles furniture crossing the seam between two
        // contiguous unlocked floor zones.
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 localSample = offset + new Vector3(
                    x * halfX,
                    0f,
                    z * halfZ
                );

                Vector3 worldSample =
                    position + rotation * localSample;

                if (!ContainsHorizontalPoint(worldSample))
                    return false;
            }
        }

        return true;
    }

    public bool IsBuildFloorCollider(Collider candidate)
    {
        if (candidate == null)
            return false;

        if (candidate == baseFloor)
            return true;

        if (expansionZones == null)
            return false;

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone == null || !zone.IsUnlocked)
                continue;

            if (candidate == zone.BuildFloor)
                return true;
        }

        return false;
    }

    private bool ContainsHorizontalPoint(Vector3 worldPoint)
    {
        if (ContainsPoint(baseFloor, worldPoint))
            return true;

        if (expansionZones == null)
            return false;

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone == null || !zone.IsUnlocked)
                continue;

            if (ContainsPoint(zone.BuildFloor, worldPoint))
                return true;
        }

        return false;
    }

    private static bool ContainsPoint(
        Collider floor,
        Vector3 worldPoint)
    {
        if (floor == null ||
            !floor.enabled ||
            !floor.gameObject.activeInHierarchy)
        {
            return false;
        }

        Bounds bounds = floor.bounds;

        return worldPoint.x >= bounds.min.x &&
               worldPoint.x <= bounds.max.x &&
               worldPoint.z >= bounds.min.z &&
               worldPoint.z <= bounds.max.z;
    }
}