using UnityEngine;

public static class LayoutFootprintUtility
{
    public static bool OverlapsHorizontal(Vector3 firstWorldCenter,
        Quaternion firstRotation,
        Vector2 firstHalfExtents,
        Vector3 secondWorldCenter,
        Quaternion secondRotation,
        Vector2 secondHalfExtents)
    {
        Vector2 firstCenter = new(firstWorldCenter.x, firstWorldCenter.z);

        Vector2 secondCenter = new(secondWorldCenter.x, secondWorldCenter.z);

        Vector2 firstRight = ToHorizontalAxis(firstRotation * Vector3.right);

        Vector2 firstForward = ToHorizontalAxis(firstRotation * Vector3.forward);

        Vector2 secondRight = ToHorizontalAxis(secondRotation * Vector3.right);

        Vector2 secondForward = ToHorizontalAxis(secondRotation * Vector3.forward);

        Vector2 difference = secondCenter - firstCenter;

        Vector2 safeFirstHalf = new(Mathf.Abs(firstHalfExtents.x), Mathf.Abs(firstHalfExtents.y));

        Vector2 safeSecondHalf = new(Mathf.Abs(secondHalfExtents.x), Mathf.Abs(secondHalfExtents.y));

        if (IsSeparated(difference, firstRight, firstRight, firstForward, safeFirstHalf, secondRight, secondForward,
                safeSecondHalf))
        {
            return false;
        }

        if (IsSeparated(difference, firstForward, firstRight, firstForward, safeFirstHalf, secondRight, secondForward,
                safeSecondHalf))
        {
            return false;
        }

        if (IsSeparated(difference, secondRight, firstRight, firstForward, safeFirstHalf, secondRight, secondForward,
                safeSecondHalf))
        {
            return false;
        }

        if (IsSeparated(difference, secondForward, firstRight, firstForward, safeFirstHalf, secondRight, secondForward,
                safeSecondHalf))
        {
            return false;
        }

        return true;
    }

    private static bool IsSeparated(Vector2 difference,
        Vector2 axis,
        Vector2 firstRight,
        Vector2 firstForward,
        Vector2 firstHalf,
        Vector2 secondRight,
        Vector2 secondForward,
        Vector2 secondHalf)
    {
        float centerDistance = Mathf.Abs(Vector2.Dot(difference, axis));

        float firstRadius = firstHalf.x * Mathf.Abs(Vector2.Dot(firstRight, axis)) +
                            firstHalf.y * Mathf.Abs(Vector2.Dot(firstForward, axis));

        float secondRadius = secondHalf.x * Mathf.Abs(Vector2.Dot(secondRight, axis)) +
                             secondHalf.y * Mathf.Abs(Vector2.Dot(secondForward, axis));

        return centerDistance > firstRadius + secondRadius;
    }

    private static Vector2 ToHorizontalAxis(Vector3 direction)
    {
        Vector2 axis = new(direction.x, direction.z);

        if (axis.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        return axis.normalized;
    }
}