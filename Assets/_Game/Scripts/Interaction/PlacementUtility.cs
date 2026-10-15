using UnityEngine;

public static class PlacementUtility
{
    public static void AlignAnchorToTarget(
        Transform objectRoot,
        Transform objectAnchor,
        Transform target
    )
    {
        if (target == null)
        {
            return;
        }

        AlignAnchorToPose(
            objectRoot,
            objectAnchor,
            target.position,
            target.rotation
        );
    }

    public static void AlignAnchorToPose(
        Transform objectRoot,
        Transform objectAnchor,
        Vector3 targetPosition,
        Quaternion targetRotation
    )
    {
        if (objectRoot == null)
        {
            return;
        }

        /*
         * Objects without an explicit anchor
         * fall back to their root transform.
         */
        if (objectAnchor == null)
        {
            objectRoot.SetPositionAndRotation(
                targetPosition,
                targetRotation
            );

            return;
        }

        /*
         * First make the anchor's orientation
         * match the target orientation.
         */
        Quaternion anchorRotationRelativeToRoot =
            Quaternion.Inverse(
                objectRoot.rotation
            )
            *
            objectAnchor.rotation;

        objectRoot.rotation =
            targetRotation
            *
            Quaternion.Inverse(
                anchorRotationRelativeToRoot
            );

        /*
         * Rotating the object moves the anchor,
         * so calculate position correction after
         * the rotation has been applied.
         */
        Vector3 positionCorrection =
            targetPosition
            -
            objectAnchor.position;

        objectRoot.position +=
            positionCorrection;
    }

    public static void DrawFrameGizmo(
        Transform frame,
        float size = 0.12f
    )
    {
        if (frame == null)
        {
            return;
        }

        Vector3 origin =
            frame.position;

        /*
         * X = red
         */
        Gizmos.color =
            Color.red;

        Gizmos.DrawLine(
            origin,
            origin + frame.right * size
        );

        /*
         * Y = green
         */
        Gizmos.color =
            Color.green;

        Gizmos.DrawLine(
            origin,
            origin + frame.up * size
        );

        /*
         * Z = blue
         *
         * For Little Market this is FRONT.
         */
        Gizmos.color =
            Color.blue;

        Gizmos.DrawLine(
            origin,
            origin + frame.forward * size
        );

        Gizmos.color =
            Color.white;

        Gizmos.DrawWireSphere(
            origin,
            size * 0.12f
        );
    }
}