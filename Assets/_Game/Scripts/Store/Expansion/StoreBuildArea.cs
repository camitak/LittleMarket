using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreBuildArea : MonoBehaviour
{
    [SerializeField]
    private StoreBuildZone[] buildZones;

    public bool TryGetPlacementSurface(
        Ray ray,
        float maxDistance,
        StoreAreaType requiredArea,
        out Vector3 surfacePoint)
    {
        surfacePoint = Vector3.zero;

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                maxDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        foreach (RaycastHit hit in hits)
        {
            StoreBuildZone zone =
                FindAvailableZone(
                    hit.collider
                );

            if (zone == null ||
                zone.AreaType != requiredArea)
            {
                continue;
            }

            surfacePoint =
                hit.point;

            surfacePoint.y =
                hit.collider.bounds.max.y;

            return true;
        }

        return false;
    }

    public bool TryGetAvailableBounds(
        StoreAreaType requiredArea,
        out Bounds bounds)
    {
        bounds = default;

        if (buildZones == null)
        {
            return false;
        }

        bool hasBounds = false;

        foreach (StoreBuildZone zone
                 in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable ||
                zone.AreaType != requiredArea)
            {
                continue;
            }

            Collider floor =
                zone.FloorCollider;

            if (!IsUsableFloor(floor))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds =
                    floor.bounds;

                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(
                floor.bounds
            );
        }

        return hasBounds;
    }

    /*
     * Returns the most relevant LOCAL available
     * build zone for the supplied world position.
     *
     * This is deliberately different from
     * TryGetAvailableBounds(), which combines
     * every zone of one StoreAreaType.
     *
     * Store Design camera entry should use this
     * local-zone method so store growth does not
     * continually force the camera farther away.
     */
    public bool TryGetWorkingZoneBounds(
        Vector3 worldPoint,
        out Bounds bounds,
        out StoreAreaType areaType)
    {
        return TryGetWorkingZoneBoundsInternal(
            worldPoint,
            null,
            out bounds,
            out areaType
        );
    }

    public bool TryGetWorkingZoneBounds(
        Vector3 worldPoint,
        StoreAreaType requiredArea,
        out Bounds bounds)
    {
        return TryGetWorkingZoneBoundsInternal(
            worldPoint,
            requiredArea,
            out bounds,
            out _
        );
    }

    public bool IsPointInsideAvailableArea(
        Vector3 worldPoint,
        StoreAreaType requiredArea)
    {
        return ContainsHorizontalPoint(
            worldPoint,
            requiredArea
        );
    }

    private bool TryGetWorkingZoneBoundsInternal(
        Vector3 worldPoint,
        StoreAreaType? requiredArea,
        out Bounds bounds,
        out StoreAreaType areaType)
    {
        bounds = default;

        areaType =
            requiredArea ??
            StoreAreaType.SalesFloor;

        if (buildZones == null)
        {
            return false;
        }

        StoreBuildZone bestZone = null;
        Bounds bestBounds = default;

        float bestHorizontalDistance =
            float.PositiveInfinity;

        float bestCenterDistance =
            float.PositiveInfinity;

        foreach (StoreBuildZone zone
                 in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable)
            {
                continue;
            }

            if (requiredArea.HasValue &&
                zone.AreaType !=
                requiredArea.Value)
            {
                continue;
            }

            Collider floor =
                zone.FloorCollider;

            if (!IsUsableFloor(floor))
            {
                continue;
            }

            Bounds candidateBounds =
                floor.bounds;

            float horizontalDistance =
                HorizontalDistanceSquaredToBounds(
                    worldPoint,
                    candidateBounds
                );

            Vector3 horizontalCenter =
                new(
                    candidateBounds.center.x,
                    worldPoint.y,
                    candidateBounds.center.z
                );

            float centerDistance =
                (
                    worldPoint -
                    horizontalCenter
                ).sqrMagnitude;

            bool closerToBounds =
                horizontalDistance <
                bestHorizontalDistance -
                0.0001f;

            bool sameBoundsDistance =
                Mathf.Abs(
                    horizontalDistance -
                    bestHorizontalDistance
                ) <= 0.0001f;

            bool betterCenterTieBreak =
                sameBoundsDistance &&
                centerDistance <
                bestCenterDistance;

            if (!closerToBounds &&
                !betterCenterTieBreak)
            {
                continue;
            }

            bestZone = zone;
            bestBounds = candidateBounds;

            bestHorizontalDistance =
                horizontalDistance;

            bestCenterDistance =
                centerDistance;
        }

        if (bestZone == null)
        {
            return false;
        }

        bounds = bestBounds;
        areaType = bestZone.AreaType;

        return true;
    }

    public void CollectAvailableFloorBounds(
        StoreAreaType requiredArea,
        List<Bounds> results)
    {
        if (results == null ||
            buildZones == null)
        {
            return;
        }

        foreach (StoreBuildZone zone
                 in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable ||
                zone.AreaType != requiredArea)
            {
                continue;
            }

            Collider floor =
                zone.FloorCollider;

            if (!IsUsableFloor(floor))
            {
                continue;
            }

            results.Add(
                floor.bounds
            );
        }
    }

    public bool IsFootprintInside(
        Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents,
        StoreAreaType requiredArea)
    {
        float halfX =
            Mathf.Max(
                0.01f,
                halfExtents.x
            );

        float halfZ =
            Mathf.Max(
                0.01f,
                halfExtents.z
            );

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 localSample =
                    offset +
                    new Vector3(
                        x * halfX,
                        0f,
                        z * halfZ
                    );

                Vector3 worldSample =
                    position +
                    rotation * localSample;

                if (!ContainsHorizontalPoint(
                        worldSample,
                        requiredArea
                    ))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsBuildFloorCollider(
        Collider candidate)
    {
        return
            FindAvailableZone(
                candidate
            ) != null;
    }

    private bool ContainsHorizontalPoint(
        Vector3 worldPoint,
        StoreAreaType requiredArea)
    {
        if (buildZones == null)
        {
            return false;
        }

        foreach (StoreBuildZone zone
                 in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable ||
                zone.AreaType != requiredArea)
            {
                continue;
            }

            Collider floor =
                zone.FloorCollider;

            if (!IsUsableFloor(floor))
            {
                continue;
            }

            Bounds bounds =
                floor.bounds;

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

    private StoreBuildZone FindAvailableZone(
        Collider candidate)
    {
        if (candidate == null ||
            buildZones == null)
        {
            return null;
        }

        foreach (StoreBuildZone zone
                 in buildZones)
        {
            if (zone == null ||
                !zone.IsAvailable)
            {
                continue;
            }

            if (zone.FloorCollider ==
                candidate)
            {
                return zone;
            }
        }

        return null;
    }

    private static bool IsUsableFloor(
        Collider floor)
    {
        return
            floor != null &&
            floor.enabled &&
            floor.gameObject.activeInHierarchy;
    }

    private static float
        HorizontalDistanceSquaredToBounds(
            Vector3 point,
            Bounds bounds)
    {
        float deltaX = 0f;

        if (point.x < bounds.min.x)
        {
            deltaX =
                bounds.min.x -
                point.x;
        }
        else if (point.x > bounds.max.x)
        {
            deltaX =
                point.x -
                bounds.max.x;
        }

        float deltaZ = 0f;

        if (point.z < bounds.min.z)
        {
            deltaZ =
                bounds.min.z -
                point.z;
        }
        else if (point.z > bounds.max.z)
        {
            deltaZ =
                point.z -
                bounds.max.z;
        }

        return
            deltaX * deltaX +
            deltaZ * deltaZ;
    }
}