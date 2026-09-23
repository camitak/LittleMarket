using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] carrySlots;

    [SerializeField] private Transform visual;

    private NavMeshAgent agent;

    private ProductData[] shoppingList;
    private ShelfRegistry shelfRegistry;
    private CheckoutQueue checkoutQueue;
    private Transform exitPoint;

    private ShelfSlot targetShelfSlot;

    private CustomerState currentState;

    private List<PickupItem> carriedItems =
        new List<PickupItem>();

    private int currentShoppingIndex;

    private Transform assignedQueuePoint;

    private bool isConfigured;

    private Vector3 visualBaseScale;

    private Coroutine happyReactionRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (visual != null)
        {
            visualBaseScale =
                visual.localScale;
        }
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

            case CustomerState.WaitingForCheckoutSpace:
                TryJoinCheckoutQueue();
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
        ProductData[] newShoppingList,
        ShelfRegistry newShelfRegistry,
        CheckoutQueue newCheckoutQueue,
        Transform newExitPoint
    )
    {
        shoppingList = newShoppingList;
        shelfRegistry = newShelfRegistry;
        checkoutQueue = newCheckoutQueue;
        exitPoint = newExitPoint;

        currentShoppingIndex = 0;

        isConfigured = true;

        SearchForNextProduct();
    }

    private void SearchForNextProduct()
    {
        currentState =
            CustomerState.SearchingForProduct;

        if (shoppingList == null)
        {
            BeginLeaving();
            return;
        }

        if (shoppingList.Length == 0)
        {
            BeginLeaving();
            return;
        }

        if (currentShoppingIndex
            >= shoppingList.Length)
        {
            BeginCheckout();
            return;
        }

        ProductData desiredProduct =
            shoppingList[currentShoppingIndex];

        if (desiredProduct == null)
        {
            currentShoppingIndex++;

            SearchForNextProduct();

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
            // Product is unavailable.
            // Skip it and continue shopping.
            currentShoppingIndex++;

            SearchForNextProduct();

            return;
        }

        targetShelfSlot = foundSlot;

        Transform shoppingPoint =
            targetShelfSlot.CustomerStandPoint;

        if (shoppingPoint == null)
        {
            currentShoppingIndex++;

            SearchForNextProduct();

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
            SearchForNextProduct();
            return;
        }

        Transform carrySlot =
            GetNextCarrySlot();

        if (carrySlot == null)
        {
            BeginCheckout();
            return;
        }

        bool tookProduct =
            targetShelfSlot.TryTakeItemForCustomer(
                carrySlot,
                out PickupItem item
            );

        if (!tookProduct)
        {
            // Another customer may have taken it.
            // Search again for the SAME shopping-list item.
            SearchForNextProduct();
            return;
        }

        carriedItems.Add(item);

        currentShoppingIndex++;

        SearchForNextProduct();
    }

    private Transform GetNextCarrySlot()
    {
        if (carrySlots == null)
        {
            return null;
        }

        int nextIndex =
            carriedItems.Count;

        if (nextIndex >= carrySlots.Length)
        {
            return null;
        }

        return carrySlots[nextIndex];
    }

    private void BeginCheckout()
    {
        if (carriedItems.Count == 0)
        {
            BeginLeaving();
            return;
        }

        if (checkoutQueue == null)
        {
            BeginLeaving();
            return;
        }

        TryJoinCheckoutQueue();
    }

    private void TryJoinCheckoutQueue()
    {
        if (checkoutQueue == null)
        {
            BeginLeaving();
            return;
        }

        bool joinedQueue =
            checkoutQueue.TryJoinQueue(this);

        if (!joinedQueue)
        {
            currentState =
                CustomerState.WaitingForCheckoutSpace;

            return;
        }

        // CheckoutQueue calls SetQueueDestination()
        // when the customer successfully joins.
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

    public int GetCarriedItemCount()
    {
        return carriedItems.Count;
    }

    public float GetCheckoutTotal()
    {
        float total = 0f;

        for (int i = 0;
             i < carriedItems.Count;
             i++)
        {
            PickupItem item =
                carriedItems[i];

            if (item == null)
            {
                continue;
            }

            ProductData productData =
                item.GetProductData();

            if (productData == null)
            {
                continue;
            }

            total +=
                productData.SellPrice;
        }

        return total;
    }

    public bool CompleteCheckout()
    {
        if (!IsReadyForCheckout())
        {
            return false;
        }

        if (carriedItems.Count == 0)
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

        for (int i = 0;
             i < carriedItems.Count;
             i++)
        {
            PickupItem item =
                carriedItems[i];

            if (item == null)
            {
                continue;
            }

            Destroy(item.gameObject);
        }

        carriedItems.Clear();

        assignedQueuePoint = null;

        PlayHappyReaction();

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

        for (int i = 0;
             i < carriedItems.Count;
             i++)
        {
            PickupItem item =
                carriedItems[i];

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        carriedItems.Clear();

        Destroy(gameObject);
    }

    private void PlayHappyReaction()
    {
        if (visual == null)
        {
            return;
        }

        if (happyReactionRoutine != null)
        {
            StopCoroutine(
                happyReactionRoutine
            );
        }

        happyReactionRoutine =
            StartCoroutine(
                HappyBounceRoutine()
            );
    }

    private IEnumerator HappyBounceRoutine()
    {
        float duration = 0.35f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float scaleMultiplier =
                1f
                + 0.12f
                * Mathf.Sin(
                    t * Mathf.PI
                );

            visual.localScale =
                visualBaseScale
                * scaleMultiplier;

            yield return null;
        }

        visual.localScale =
            visualBaseScale;

        happyReactionRoutine = null;
    }
}