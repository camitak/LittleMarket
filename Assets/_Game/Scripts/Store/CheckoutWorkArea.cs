using UnityEngine;

[DisallowMultipleComponent] public class CheckoutWorkArea : MonoBehaviour
{
    [Header("Cashier Position")]
    [SerializeField] private Transform cashierStandPoint;
    [Min(0.1f)] [SerializeField] private float workingRadius = 0.75f;
    [Min(0f)] [SerializeField] private float minimumStaffSideZ = 0.45f;

    public bool CanServeFrom(Vector3 playerWorldPosition)
    {
        if (cashierStandPoint == null) return false;

        Vector3 localPosition = transform.InverseTransformPoint(playerWorldPosition);

        if (localPosition.z < minimumStaffSideZ)
        {
            return false;
        }

        return IsWithinWorkingRadius(localPosition);
    }

    public string GetPositionPrompt(Vector3 playerWorldPosition)
    {
        if (cashierStandPoint == null)
        {
            return "Checkout cashier position not configured";
        }

        Vector3 localPosition = transform.InverseTransformPoint(playerWorldPosition);

        if (localPosition.z < minimumStaffSideZ)
        {
            return "Cashier position: behind counter, opposite the customer queue";
        }

        if (!IsWithinWorkingRadius(localPosition))
        {
            return "Move into the cashier area behind the counter";
        }

        return "";
    }

    private bool IsWithinWorkingRadius(Vector3 localPosition)
    {
        Vector3 localStandPoint = transform.InverseTransformPoint(cashierStandPoint.position);

        float differenceX = localPosition.x - localStandPoint.x;
        float differenceZ = localPosition.z - localStandPoint.z;

        float squaredDistance = differenceX * differenceX + differenceZ * differenceZ;

        return squaredDistance <= workingRadius * workingRadius;
    }

    private void OnDrawGizmosSelected()
    {
        if (cashierStandPoint == null) return;

        Gizmos.color = new Color(0.40f, 0.85f, 0.65f, 1f);

        Vector3 center = cashierStandPoint.position + Vector3.up * 0.03f;
        Vector3 previous = center + transform.right * workingRadius;

        const int segments = 32;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 offset = (transform.right * Mathf.Cos(angle) + transform.forward * Mathf.Sin(angle)) *
                             workingRadius;

            Vector3 current = center + offset;

            Gizmos.DrawLine(previous, current);
            previous = current;
        }

        Gizmos.DrawLine(center, center + transform.forward * 0.30f);
    }
}
