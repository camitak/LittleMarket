using UnityEngine;

public static class InteractionTargetFinder
{
    public static IInteractable Find(
        Camera playerCamera,
        float interactionDistance,
        float interactionAssistRadius
    )
    {
        if (playerCamera == null)
        {
            return null;
        }

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        bool hasCenterHit =
            Physics.Raycast(
                ray,
                out RaycastHit centerHit,
                interactionDistance
            );

        if (hasCenterHit)
        {
            IInteractable directInteractable =
                FindInteractableOnCollider(
                    centerHit.collider
                );

            if (directInteractable != null)
            {
                return directInteractable;
            }
        }

        RaycastHit[] assistedHits =
            Physics.SphereCastAll(
                ray,
                interactionAssistRadius,
                interactionDistance
            );

        IInteractable bestInteractable =
            null;

        float bestScore =
            float.MaxValue;

        float maximumAllowedDistance =
            interactionDistance;

        if (hasCenterHit)
        {
            maximumAllowedDistance =
                centerHit.distance
                + interactionAssistRadius
                * 2f;
        }

        for (int i = 0;
             i < assistedHits.Length;
             i++)
        {
            RaycastHit assistedHit =
                assistedHits[i];

            if (assistedHit.collider == null)
            {
                continue;
            }

            if (assistedHit.distance
                > maximumAllowedDistance)
            {
                continue;
            }

            IInteractable interactable =
                FindInteractableOnCollider(
                    assistedHit.collider
                );

            if (interactable == null)
            {
                continue;
            }

            Vector3 toCenter =
                assistedHit.collider
                    .bounds.center
                - ray.origin;

            float forwardDistance =
                Vector3.Dot(
                    toCenter,
                    ray.direction
                );

            if (forwardDistance < 0f)
            {
                continue;
            }

            Vector3 closestPointOnRay =
                ray.origin
                + ray.direction
                * forwardDistance;

            float distanceFromCrosshair =
                Vector3.SqrMagnitude(
                    assistedHit.collider
                        .bounds.center
                    - closestPointOnRay
                );

            float score =
                distanceFromCrosshair
                + assistedHit.distance
                * 0.0001f;

            if (score >= bestScore)
            {
                continue;
            }

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
        if (targetCollider == null)
        {
            return null;
        }

        Transform currentTransform =
            targetCollider.transform;

        while (currentTransform != null)
        {
            MonoBehaviour[] behaviours =
                currentTransform
                    .GetComponents<
                        MonoBehaviour
                    >();

            for (int i = 0;
                 i < behaviours.Length;
                 i++)
            {
                MonoBehaviour behaviour =
                    behaviours[i];

                if (behaviour
                    is IInteractable interactable)
                {
                    return interactable;
                }
            }

            currentTransform =
                currentTransform.parent;
        }

        return null;
    }
}