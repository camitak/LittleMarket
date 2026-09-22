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
    private CheckoutQueue checkoutQueue;
    private Transform exitPoint;

    private ShelfSlot targetShelfSlot;

    private CustomerState currentState;

    private PickupItem carriedItem;

    private Transform assignedQueuePoint;

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

            case CustomerState.WalkingToCheckout:
                UpdateWalkingToCheckout();
                break;

            case CustomerState.WaitingInCheckoutQueue:
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
        CheckoutQueue newCheckoutQueue,
        Transform newExitPoint
    )
    {
        desiredProduct = newDesiredProduct;
        shelfRegistry = newShelfRegistry;
        checkoutQueue = newCheckoutQueue;
        exitPoint = newExitPoint;

        isConfigured = true;

        SearchForProduct();
    }

    private void SearchForProduct()
    {
        currentState =
            CustomerState.SearchingForProduct;

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

        if (checkoutQueue == null)
        {
            BeginLeaving();
            return;
        }

        if (!checkoutQueue.HasSpace)
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

        if (checkoutQueue == null)
        {
            BeginLeaving();
            return;
        }

        if (!checkoutQueue.HasSpace)
        {
            BeginLeaving();
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

        bool joinedQueue =
            checkoutQueue.TryJoinQueue(this);

        if (!joinedQueue)
        {
            carriedItem.Drop();
            carriedItem = null;

            BeginLeaving();
        }
    }

    public void SetQueueDestination(
        Transform queuePoint
    )
    {
        if (queuePoint == null)
        {
            return;
        }

        assignedQueuePoint = queuePoint;

        currentState =
            CustomerState.WalkingToCheckout;

        bool destinationAccepted =
            agent.SetDestination(
                queuePoint.position
            );

        if (!destinationAccepted)
        {
            BeginLeaving();
        }
    }

    private void UpdateWalkingToCheckout()
    {
        if (!HasReachedDestination())
        {
            return;
        }

        agent.ResetPath();

        if (assignedQueuePoint != null)
        {
            transform.rotation =
                assignedQueuePoint.rotation;
        }

        currentState =
            CustomerState.WaitingInCheckoutQueue;
    }

    public bool IsReadyForCheckout()
    {
        return currentState ==
               CustomerState.WaitingInCheckoutQueue;
    }

    public ProductData GetCarriedProductData()
    {
        if (carriedItem == null)
        {
            return null;
        }

        return carriedItem.GetProductData();
    }

    public bool CompleteCheckout()
    {
        if (!IsReadyForCheckout())
        {
            return false;
        }

        if (carriedItem == null)
        {
            return false;
        }

        if (checkoutQueue != null)
        {
            CheckoutQueue previousQueue =
                checkoutQueue;

            checkoutQueue = null;

            previousQueue.LeaveQueue(this);
        }

        Destroy(carriedItem.gameObject);

        carriedItem = null;
        assignedQueuePoint = null;

        BeginLeaving();

        return true;
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
        currentState =
            CustomerState.Finished;

        if (checkoutQueue != null)
        {
            checkoutQueue.LeaveQueue(this);
        }

        if (carriedItem != null)
        {
            Destroy(carriedItem.gameObject);
        }

        Destroy(gameObject);
    }
}