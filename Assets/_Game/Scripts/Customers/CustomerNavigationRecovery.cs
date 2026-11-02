using UnityEngine;
using UnityEngine.AI;

public static class CustomerNavigationRecovery
{
    private const float GroundIgnoreHeight = 0.12f;

    public static bool TryFindRecoveryPosition(NavMeshAgent agent,
        Transform customerRoot,
        Vector3 origin,
        float maxSearchRadius,
        float maxVerticalOffset,
        out Vector3 recoveryPosition)
    {
        recoveryPosition = origin;

        if (agent == null || customerRoot == null)
        {
            return false;
        }

        NavMeshQueryFilter filter = new() { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };

        float searchStep = Mathf.Max(0.3f, agent.radius);

        float sampleRadius = Mathf.Max(0.25f, agent.radius);

        if (TrySampleCandidate(agent, customerRoot, origin, origin, sampleRadius, maxVerticalOffset, filter,
                out recoveryPosition))
        {
            return true;
        }

        float maximumRadius = Mathf.Max(searchStep, maxSearchRadius);

        for (float radius = searchStep; radius <= maximumRadius + 0.001f; radius += searchStep)
        {
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * radius / searchStep), 8, 32);

            bool foundInRing = false;

            Vector3 bestPosition = Vector3.zero;

            float bestDistanceSquared = float.MaxValue;

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i / (float)sampleCount * Mathf.PI * 2f;

                Vector3 queryPosition = origin + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (!TrySampleCandidate(agent, customerRoot, queryPosition, origin, sampleRadius, maxVerticalOffset,
                        filter, out Vector3 candidate))
                {
                    continue;
                }

                Vector3 horizontalDifference = candidate - origin;

                horizontalDifference.y = 0f;

                float distanceSquared = horizontalDifference.sqrMagnitude;

                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;

                bestPosition = candidate;

                foundInRing = true;
            }

            if (!foundInRing)
                continue;

            recoveryPosition = bestPosition;

            return true;
        }

        return false;
    }

    private static bool TrySampleCandidate(NavMeshAgent agent,
        Transform customerRoot,
        Vector3 queryPosition,
        Vector3 originalPosition,
        float sampleRadius,
        float maxVerticalOffset,
        NavMeshQueryFilter filter,
        out Vector3 candidate)
    {
        candidate = Vector3.zero;

        if (!NavMesh.SamplePosition(queryPosition, out NavMeshHit hit, sampleRadius, filter))
        {
            return false;
        }

        if (Mathf.Abs(hit.position.y - originalPosition.y) > maxVerticalOffset)
        {
            return false;
        }

        if (!HasPhysicalClearance(agent, customerRoot, hit.position))
        {
            return false;
        }

        candidate = hit.position;

        return true;
    }

    private static bool HasPhysicalClearance(NavMeshAgent agent, Transform customerRoot, Vector3 position)
    {
        float radius = Mathf.Max(0.05f, agent.radius);

        float height = Mathf.Max(agent.height, radius * 2f);

        Vector3 bottom = position + Vector3.up * radius;

        Vector3 top = position + Vector3.up * (height - radius);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.transform == customerRoot || hit.transform.IsChildOf(customerRoot))
            {
                continue;
            }

            if (hit.bounds.max.y <= position.y + GroundIgnoreHeight)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}