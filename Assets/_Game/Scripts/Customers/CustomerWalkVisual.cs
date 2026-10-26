using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class CustomerWalkVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform visual;

    [Header("Walking Animation")]
    [Min(0f)] [SerializeField] private float walkBounceHeight = 0.055f;
    [Min(0f)] [SerializeField] private float walkSideSway = 0.025f;
    [Min(0f)] [SerializeField] private float walkCycleSpeed = 8f;
    [Min(0f)] [SerializeField] private float movingSpeedThreshold = 0.15f;
    [Min(0.1f)] [SerializeField] private float blendSpeed = 7f;

    [Header("Idle Animation")]
    [Min(0f)] [SerializeField] private float idleBreathHeight = 0.008f;
    [Min(0f)] [SerializeField] private float idleBreathSpeed = 1.5f;

    private Vector3 visualRestPosition;
    private float walkPhase;
    private float movementBlend;
    private bool hasRestPosition;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (visual == null)
        {
            Debug.LogWarning(
                "CustomerWalkVisual needs the customer Visual child assigned.",
                this
            );
            return;
        }

        visualRestPosition = visual.localPosition;
        hasRestPosition = true;
    }

    private void LateUpdate()
    {
        if (!hasRestPosition || agent == null)
            return;

        bool moving = agent.enabled &&
                      agent.isOnNavMesh &&
                      agent.velocity.sqrMagnitude >
                      movingSpeedThreshold * movingSpeedThreshold;

        float targetBlend = moving ? 1f : 0f;

        movementBlend = Mathf.MoveTowards(
            movementBlend,
            targetBlend,
            blendSpeed * Time.deltaTime
        );

        if (moving)
            walkPhase += Time.deltaTime * walkCycleSpeed;

        float bounce =
            (Mathf.Sin(walkPhase * 2f) + 1f) *
            0.5f * walkBounceHeight;

        float sway = Mathf.Sin(walkPhase) * walkSideSway;

        float breathing =
            Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathHeight;

        float verticalOffset = Mathf.Lerp(
            breathing,
            bounce,
            movementBlend
        );

        visual.localPosition = visualRestPosition +
                               Vector3.up * verticalOffset +
                               Vector3.right * (sway * movementBlend);
    }

    private void OnDisable()
    {
        if (hasRestPosition && visual != null)
            visual.localPosition = visualRestPosition;
    }
}
