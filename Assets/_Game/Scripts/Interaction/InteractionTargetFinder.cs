using UnityEngine;

public static class InteractionTargetFinder
{
    public static IInteractable Find(
        Camera playerCamera,
        float interactionDistance,
        float interactionAssistRadius
    )
    {
        if (playerCamera == null) return null;

        var ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        var hasCenterHit =
            Physics.Raycast(
                ray,
                out var centerHit,
                interactionDistance
            );

        if (hasCenterHit)
        {
            var directInteractable =
                FindInteractableOnCollider(
                    centerHit.collider
                );

            if (directInteractable != null) return directInteractable;
        }

        var assistedHits =
            Physics.SphereCastAll(
                ray,
                interactionAssistRadius,
                interactionDistance
            );

        IInteractable bestInteractable =
            null;

        var bestScore =
            float.MaxValue;

        var maximumAllowedDistance =
            interactionDistance;

        if (hasCenterHit)
            maximumAllowedDistance =
                centerHit.distance
                + interactionAssistRadius
                * 2f;

        for (var i = 0;
             i < assistedHits.Length;
             i++)
        {
            var assistedHit =
                assistedHits[i];

            if (assistedHit.collider == null) continue;

            if (assistedHit.distance
                > maximumAllowedDistance)
                continue;

            var interactable =
                FindInteractableOnCollider(
                    assistedHit.collider
                );

            if (interactable == null) continue;

            var toCenter =
                assistedHit.collider
                    .bounds.center
                - ray.origin;

            var forwardDistance =
                Vector3.Dot(
                    toCenter,
                    ray.direction
                );

            if (forwardDistance < 0f) continue;

            var closestPointOnRay =
                ray.origin
                + ray.direction
                * forwardDistance;

            var distanceFromCrosshair =
                Vector3.SqrMagnitude(
                    assistedHit.collider
                        .bounds.center
                    - closestPointOnRay
                );

            var score =
                distanceFromCrosshair
                + assistedHit.distance
                * 0.0001f;

            if (score >= bestScore) continue;

            bestScore =
                score;

            bestInteractable =
                interactable;
        }

        return bestInteractable;
    }

    private static IInteractable
        FindInteractableOnCollider(
            Collider targetCollider
        )
    {
        if (targetCollider == null) return null;

        var currentTransform =
            targetCollider.transform;

        while (currentTransform != null)
        {
            var behaviours =
                currentTransform
                    .GetComponents<
                        MonoBehaviour
                    >();

            for (var i = 0;
                 i < behaviours.Length;
                 i++)
            {
                var behaviour =
                    behaviours[i];

                if (behaviour
                    is IInteractable interactable)
                    return interactable;
            }

            currentTransform =
                currentTransform.parent;
        }

        return null;
    }
}