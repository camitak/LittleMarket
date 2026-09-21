using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("Shopping")]
    [SerializeField] private ShelfSlot targetShelfSlot;
    [SerializeField] private Transform shoppingPoint;

    [Header("Exit")]
    [SerializeField] private Transform exitPoint;

    [Header("References")]
    [SerializeField] private Transform carryPoint;

    private NavMeshAgent agent;

    private CustomerState currentState;

    private PickupItem carriedItem;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        BeginShopping();
    }

    private void Update()
    {
        switch (currentState)
        {
            case CustomerState.WalkingToProduct:
                UpdateWalkingToProduct();
                break;

            case CustomerState.TakingProduct:
                TakeProduct();
                break;

            case CustomerState.WalkingToExit:
                UpdateWalkingToExit();
                break;

            case CustomerState.Finished:
                break;
        }
    }

    private void BeginShopping()
    {
        currentState = CustomerState.WalkingToProduct;

        agent.SetDestination(
            shoppingPoint.position
        );
    }

    private void UpdateWalkingToProduct()
    {
        if (!HasReachedDestination())
        {
            return;
        }

        currentState = CustomerState.TakingProduct;
    }

    private void TakeProduct()
    {
        bool tookProduct =
            targetShelfSlot.TryTakeItemForCustomer(
                carryPoint,
                out PickupItem item
            );

        if (tookProduct)
        {
            carriedItem = item;
        }

        BeginLeaving();
    }

    private void BeginLeaving()
    {
        currentState = CustomerState.WalkingToExit;

        agent.SetDestination(
            exitPoint.position
        );
    }

    private void UpdateWalkingToExit()
    {
        if (!HasReachedDestination())
        {
            return;
        }

        FinishVisit();
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending)
        {
            return false;
        }

        if (agent.remainingDistance
            > agent.stoppingDistance)
        {
            return false;
        }

        return true;
    }

    private void FinishVisit()
    {
        currentState = CustomerState.Finished;

        if (carriedItem != null)
        {
            Destroy(carriedItem.gameObject);
        }

        Destroy(gameObject);
    }
}