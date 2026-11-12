
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class StoreRouteAudit : MonoBehaviour
{
    [Header("Route Origins")]
    [SerializeField] private Transform customerEntranceInsidePoint;
    [SerializeField] private Transform staffDoorSalesSide;

    [Header("NavMesh")]
    [Min(0.1f)]
    [SerializeField] private float sampleRadius = 0.65f;

    [Header("Feedback")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_Text warningMessageText;
    [Min(1f)] [SerializeField] private float warningDurationSeconds = 12f;

    private NavMeshPath reusablePath;
    private float warningExpiresAt;

    private void Awake()
    {
        reusablePath = new NavMeshPath();
        DismissWarning();
    }

    private void Update()
    {
        if (warningPanel == null || !warningPanel.activeSelf)
        {
            return;
        }

        if (Time.unscaledTime >= warningExpiresAt)
        {
            DismissWarning();
        }
    }

    public void CheckRoutes()
    {
        DismissWarning();

        if (customerEntranceInsidePoint == null || staffDoorSalesSide == null)
        {
            ShowWarning("Store route origins are not configured.");
            return;
        }

        if (!TrySamplePosition(customerEntranceInsidePoint.position, out Vector3 customerStart))
        {
            ShowWarning("Cannot verify customer routes: entrance marker is off the NavMesh.");
            return;
        }

        if (!TrySamplePosition(staffDoorSalesSide.position, out Vector3 staffStart))
        {
            ShowWarning("Cannot verify staff routes: staff-door marker is off the NavMesh.");
            return;
        }

        CheckoutQueue[] queues = FindObjectsByType<CheckoutQueue>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (queues.Length == 0)
        {
            ShowWarning("No active checkout was found. Customers cannot complete purchases.");
            return;
        }

        foreach (CheckoutQueue queue in queues)
        {
            if (queue == null)
            {
                continue;
            }

            if (queue.QueuePointCount == 0)
            {
                ShowWarning($"{queue.name} has no configured queue positions.");
                return;
            }

            for (int i = 0; i < queue.QueuePointCount; i++)
            {
                Transform queuePoint = queue.GetQueuePoint(i);

                if (queuePoint == null)
                {
                    ShowWarning($"{queue.name}: queue position {i + 1} is missing.");
                    return;
                }

                if (!TrySamplePosition(queuePoint.position, out Vector3 customerTarget))
                {
                    ShowWarning($"{queue.name}: queue position {i + 1} is off the walkable NavMesh.");
                    return;
                }

                if (!HasCompleteRoute(customerStart, customerTarget))
                {
                    ShowWarning(
                        $"{queue.name}: customers cannot reach queue position {i + 1}.\n" +
                        "Re-enter Store Design (L) and clear a connecting aisle.");
                    return;
                }
            }

            CheckoutWorkArea workArea = queue.GetComponent<CheckoutWorkArea>();

            if (workArea == null || workArea.CashierStandPoint == null)
            {
                ShowWarning($"{queue.name}: cashier standing point is missing.");
                return;
            }

            if (!TrySamplePosition(workArea.CashierStandPoint.position, out Vector3 staffTarget))
            {
                ShowWarning($"{queue.name}: cashier standing point is off the walkable NavMesh.");
                return;
            }

            if (!HasCompleteRoute(staffStart, staffTarget))
            {
                ShowWarning(
                    $"{queue.name}: cashier access is disconnected from the staff doorway.\n" +
                    "Re-enter Store Design (L) and clear a connecting aisle.");
                return;
            }
        }

        Debug.Log("Little Market: all checkout queue and cashier routes passed the NavMesh audit.", this);
    }

    public void DismissWarning()
    {
        warningExpiresAt = 0f;

        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    private bool TrySamplePosition(Vector3 requestedPosition, out Vector3 sampledPosition)
    {
        sampledPosition = Vector3.zero;

        if (!NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit,
                sampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        sampledPosition = hit.position;
        return true;
    }

    private bool HasCompleteRoute(Vector3 start, Vector3 destination)
    {
        bool calculated = NavMesh.CalculatePath(
            start, destination, NavMesh.AllAreas, reusablePath);

        return calculated && reusablePath.status == NavMeshPathStatus.PathComplete;
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning($"Little Market route audit: {message}", this);

        if (warningMessageText != null)
        {
            warningMessageText.text = message;
        }

        warningExpiresAt = Time.unscaledTime + warningDurationSeconds;

        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
        }
    }
}
