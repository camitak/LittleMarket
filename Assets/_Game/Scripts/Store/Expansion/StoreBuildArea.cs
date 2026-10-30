using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreBuildArea : MonoBehaviour
{
    [SerializeField] private StoreBuildZone[] buildZones;

    public bool TryGetPlacementSurface(
        Ray ray,
        float maxDistance,
        StoreAreaType requiredArea,
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
            StoreBuildZone zone = FindAvailableZone(hit.collider);

            if (zone == null || zone.AreaType != requiredArea)
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
        Vector3 halfExtents,
        StoreAreaType requiredArea)
    {
        float halfX = Mathf.Max(0.01f, halfExtents.x);
        float halfZ = Mathf.Max(0.01f, halfExtents.z);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 localSample = offset + new Vector3(
                    x * halfX,
                    0f,
                    z * halfZ
                );

                Vector3 worldSample = position + rotation * localSample;

                if (!ContainsHorizontalPoint(worldSample, requiredArea))
                    return false;
            }
        }

        return true;
    }

    public bool IsBuildFloorCollider(Collider candidate)
    {
        return FindAvailableZone(candidate) != null;
    }

    private bool ContainsHorizontalPoint(
        Vector3 worldPoint,
        StoreAreaType requiredArea)
    {
        if (buildZones == null)
            return false;

        foreach (StoreBuildZone zone in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable ||
                zone.AreaType != requiredArea)
            {
                continue;
            }

            Collider floor = zone.FloorCollider;

            if (floor == null ||
                !floor.enabled ||
                !floor.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = floor.bounds;

            if (worldPoint.x >= bounds.min.x &&
                worldPoint.x <= bounds.max.x &&
                worldPoint.z >= bounds.min.z &&
                worldPoint.z <= bounds.max.z)
            {
                return true;
            }
        }

        return false;
    }

    private StoreBuildZone FindAvailableZone(Collider candidate)
    {
        if (candidate == null || buildZones == null)
            return null;

        foreach (StoreBuildZone zone in buildZones)
        {
            if (zone == null || !zone.IsAvailable)
                continue;

            if (zone.FloorCollider == candidate)
                return zone;
        }

        return null;
    }
}