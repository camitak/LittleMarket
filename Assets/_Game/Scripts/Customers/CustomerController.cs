using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform carryPoint;

    private NavMeshAgent agent;

    private ProductData desiredProduct;
    private ShelfRegistry shelfRegistry;
    private Transform exitPoint;

    private ShelfSlot targetShelfSlot;

    private CustomerState currentState;

    private PickupItem carriedItem;

    private bool isConfigured;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!isConfigured)
        {
            return;
        }

        switch (currentState)
        {
            case CustomerState.SearchingForProduct:
                break;

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

    public void Configure(
        ProductData newDesiredProduct,
        ShelfRegistry newShelfRegistry,
        Transform newExitPoint
    )
    {
        desiredProduct = newDesiredProduct;
        shelfRegistry = newShelfRegistry;
        exitPoint = newExitPoint;

        isConfigured = true;

        SearchForProduct();
    }

    private void SearchForProduct()
    {
        currentState = CustomerState.SearchingForProduct;

        if (desiredProduct == null)
        {
            BeginLeaving();
            return;
        }

        if (shelfRegistry == null)
        {
            BeginLeaving();
            return;
        }

        bool foundProduct =
            shelfRegistry.TryFindStockedSlot(
                desiredProduct,
                out ShelfSlot foundSlot
            );

        if (!foundProduct)
        {
            BeginLeaving();
            return;
        }

        targetShelfSlot = foundSlot;

        Transform shoppingPoint =
            targetShelfSlot.CustomerStandPoint;

        if (shoppingPoint == null)
        {
            BeginLeaving();
            return;
        }

        currentState =
            CustomerState.WalkingToProduct;

        bool destinationAccepted =
            agent.SetDestination(
                shoppingPoint.position
            );

        if (!destinationAccepted)
        {
            BeginLeaving();
        }
    }

    private void UpdateWalkingToProduct()
    {
        if (!HasReachedDestination())
        {
            return;
        }

        currentState =
            CustomerState.TakingProduct;
    }

    private void TakeProduct()
    {
        if (targetShelfSlot == null)
        {
            SearchForProduct();
            return;
        }

        bool tookProduct =
            targetShelfSlot.TryTakeItemForCustomer(
                carryPoint,
                out PickupItem item
            );

        if (!tookProduct)
        {
            SearchForProduct();
            return;
        }

        carriedItem = item;

        BeginLeaving();
    }

    private void BeginLeaving()
    {
        currentState =
            CustomerState.WalkingToExit;

        if (exitPoint == null)
        {
            FinishVisit();
            return;
        }

        bool destinationAccepted =
            agent.SetDestination(
                exitPoint.position
            );

        if (!destinationAccepted)
        {
            FinishVisit();
        }
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