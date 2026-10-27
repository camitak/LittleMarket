using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] carrySlots;
    [SerializeField] private Transform checkoutBagCarryPoint;
    [SerializeField] private Transform visual;
    [SerializeField] private CustomerShoppingBasket customerShoppingBasket;

    [Header("Shelf Browsing")]
    [Min(0.05f)] [SerializeField] private float shelfBrowseDuration = 0.85f;
    [Min(0f)] [SerializeField] private float shelfTurnDegreesPerSecond = 360f;
    [Range(0f, 20f)] [SerializeField] private float shelfLeanDegrees = 10f;

    private NavMeshAgent agent;
    private ProductData[] shoppingList;
    private ShelfRegistry shelfRegistry;
    private CheckoutQueue checkoutQueue;
    private Transform entranceInsidePoint;
    private Transform exitInsidePoint;
    private Transform exitPoint;
    private DailyStats dailyStats;
    private CustomerBasketStation customerBasketStation;
    private float basketPickupElapsed;
    private float basketApproachElapsed;
    private ShelfSlot targetShelfSlot;
    private CustomerState currentState;
    private List<PickupItem> carriedItems = new();
    private int currentShoppingIndex;
    private Transform assignedQueuePoint;
    private bool isConfigured;
    private Vector3 visualBaseScale;
    private Quaternion visualBaseLocalRotation;
    private float shelfBrowseElapsed;
    private Coroutine happyReactionRoutine;

    // Satisfaction
    private float satisfactionScore = 100f;
    private int missedProductCount;
    private float checkoutWaitSeconds;
    private bool completedPurchase;
    private bool visitReported;
    private const float MissingProductPenalty = 20f;
    private const float CheckoutGraceSeconds = 5f;
    private const float CheckoutPenaltyPerSecond = 2f;
    private const float MaxCheckoutWaitPenalty = 30f;
    
    public bool IsWaitingAtCheckout =>
        isConfigured &&
        (currentState == CustomerState.WaitingForCheckoutSpace ||
         currentState == CustomerState.WaitingInCheckoutQueue);

    public float CurrentCheckoutWaitPenalty => CalculateCheckoutWaitPenalty();
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (visual != null)
        {
            visualBaseScale = visual.localScale;
            visualBaseLocalRotation = visual.localRotation;
        }

    }

    private void Update()
    {
        if (!isConfigured) return;

        switch (currentState)
        {
            case CustomerState.WalkingIntoStore:
                UpdateWalkingIntoStore();
                break;
            
            case CustomerState.WalkingToBasketStation:
                UpdateWalkingToBasketStation();
                break;

            case CustomerState.TakingBasket:
                UpdateTakingBasket();
                break;
            
            case CustomerState.SearchingForProduct:
                break;

            case CustomerState.WalkingToProduct:
                UpdateWalkingToProduct();
                break;

            case CustomerState.TakingProduct:
                UpdateShelfBrowsing();
                break;

            case CustomerState.WaitingForCheckoutSpace:
                UpdateCheckoutWaiting();
                TryJoinCheckoutQueue();
                break;

            case CustomerState.WalkingToCheckout:
                UpdateWalkingToCheckout();
                break;

            case CustomerState.WaitingInCheckoutQueue:
                UpdateCheckoutWaiting();
                break;

            case CustomerState.WalkingToExitApproach:
                UpdateWalkingToExitApproach();
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
        Transform newEntranceInsidePoint,
        Transform newExitInsidePoint,
        Transform newExitPoint,
        DailyStats newDailyStats,
        CustomerBasketStation newCustomerBasketStation
    )
    {
        shoppingList = newShoppingList;
        shelfRegistry = newShelfRegistry;
        checkoutQueue = newCheckoutQueue;
        entranceInsidePoint = newEntranceInsidePoint;
        exitInsidePoint = newExitInsidePoint;
        exitPoint = newExitPoint;
        dailyStats = newDailyStats;
        customerBasketStation = newCustomerBasketStation;
        currentShoppingIndex = 0;
        satisfactionScore = 100f;
        missedProductCount = 0;
        checkoutWaitSeconds = 0f;
        completedPurchase = false;
        visitReported = false;
        isConfigured = true;

        if (customerShoppingBasket != null)
        {
            int requestedItems = shoppingList != null
                ? shoppingList.Length
                : 0;

            customerShoppingBasket.ConfigureForShoppingList(requestedItems);
        }

        BeginEnteringStore();

    }
    
    private void BeginEnteringStore()
    {
        if (entranceInsidePoint == null)
        {
            BeginBasketPickupOrShopping();
            return;
        }

        currentState = CustomerState.WalkingIntoStore;

        bool accepted = agent.SetDestination(
            entranceInsidePoint.position
        );

        if (!accepted)
            BeginBasketPickupOrShopping();
    }

    private void UpdateWalkingIntoStore()
    {
        if (!HasReachedDestination())
            return;

        BeginBasketPickupOrShopping();
    }

    private void BeginBasketPickupOrShopping()
    {
        if (customerShoppingBasket == null ||
            !customerShoppingBasket.NeedsBasket)
        {
            SearchForNextProduct();
            return;
        }

        if (customerBasketStation == null ||
            customerBasketStation.PickupPoint == null)
        {
            Debug.LogWarning(
                "Customer basket station not configured. Continuing without a basket.",
                this
            );

            SearchForNextProduct();
            return;
        }

        currentState = CustomerState.WalkingToBasketStation;
        basketApproachElapsed = 0f;

        bool accepted = agent.SetDestination(
            customerBasketStation.PickupPoint.position
        );

        
        if (!accepted)
        {
            Debug.LogWarning(
                "Basket rack unreachable. Continuing without a basket.",
                this
            );

            SearchForNextProduct();
        }

    }

    private void UpdateWalkingToBasketStation()
    {
        basketApproachElapsed += Time.deltaTime;

        bool reached = HasReachedDestination();

        if (!reached && basketApproachElapsed < 8f)
            return;

        
        if (!reached)
        {
            Debug.LogWarning(
                "Basket rack approach timed out. Continuing without a basket.",
                this
            );

            SearchForNextProduct();
            return;
        }


        agent.ResetPath();

        basketPickupElapsed = 0f;
        currentState = CustomerState.TakingBasket;
    }

    private void UpdateTakingBasket()
    {
        basketPickupElapsed += Time.deltaTime;

        if (basketPickupElapsed < 0.45f)
            return;

        if (customerShoppingBasket != null)
            customerShoppingBasket.TakeBasket(customerBasketStation);

        SearchForNextProduct();
    }

    
    private void SearchForNextProduct()
    {
        currentState = CustomerState.SearchingForProduct;

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

        if (currentShoppingIndex >= shoppingList.Length)
        {
            BeginCheckout();
            return;
        }

        var desiredProduct =
            shoppingList[
                currentShoppingIndex
            ];

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

        var foundProduct =
            shelfRegistry.TryFindStockedSlot(
                desiredProduct,
                out var foundSlot
            );

        if (!foundProduct)
        {
            RecordMissingProduct();

            currentShoppingIndex++;

            SearchForNextProduct();

            return;
        }

        targetShelfSlot = foundSlot;

        var shoppingPoint = targetShelfSlot.CustomerStandPoint;

        if (shoppingPoint == null)
        {
            currentShoppingIndex++;

            SearchForNextProduct();

            return;
        }

        currentState =
            CustomerState.WalkingToProduct;

        var destinationAccepted =
            agent.SetDestination(
                shoppingPoint.position
            );

        if (!destinationAccepted) BeginLeaving();
    }

    
    private void UpdateWalkingToProduct()
    {
        if (!HasReachedDestination())
            return;

        agent.ResetPath();
        agent.updateRotation = false;

        shelfBrowseElapsed = 0f;

        currentState = CustomerState.TakingProduct;
    }

    private void UpdateShelfBrowsing()
    {
        if (targetShelfSlot == null)
        {
            FinishShelfBrowsing();
            return;
        }

        shelfBrowseElapsed += Time.deltaTime;

        // Turn toward the selected shelf slot.
        Vector3 shelfDirection =
            targetShelfSlot.transform.position - transform.position;

        shelfDirection.y = 0f;

        if (shelfDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(shelfDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                shelfTurnDegreesPerSecond * Time.deltaTime
            );
        }

        // Animate a small forward lean and return.
        float duration = Mathf.Max(0.05f, shelfBrowseDuration);

        float progress = Mathf.Clamp01(
            shelfBrowseElapsed / duration
        );

        if (visual != null)
        {
            float leanAngle =
                Mathf.Sin(progress * Mathf.PI) * shelfLeanDegrees;

            visual.localRotation =
                visualBaseLocalRotation *
                Quaternion.Euler(leanAngle, 0f, 0f);
        }

        if (shelfBrowseElapsed < duration)
            return;

        FinishShelfBrowsing();
    }

    private void FinishShelfBrowsing()
    {
        if (visual != null)
            visual.localRotation = visualBaseLocalRotation;

        agent.updateRotation = true;

        TakeProduct();
    }

    private void TakeProduct()
    {
        if (targetShelfSlot == null)
        {
            SearchForNextProduct();
            return;
        }

        var carrySlot = GetNextCarrySlot();

        if (carrySlot == null)
        {
            BeginCheckout();
            return;
        }

        var tookProduct =
            targetShelfSlot.TryTakeItemForCustomer(
                carrySlot,
                out var item
            );

        if (!tookProduct)
        {
            // Another customer may have taken
            // this exact item before we arrived.
            //
            // Search again for the SAME list item.
            // Don't count it as missed yet.
            SearchForNextProduct();

            return;
        }
        
        carriedItems.Add(item);

        if (customerShoppingBasket != null)
            customerShoppingBasket.FitCollectedItem(item);

        currentShoppingIndex++;
        SearchForNextProduct();
    }

    private void RecordMissingProduct()
    {
        missedProductCount++;

        satisfactionScore -=
            MissingProductPenalty;

        satisfactionScore =
            Mathf.Max(
                0f,
                satisfactionScore
            );
    }

    
    private Transform GetNextCarrySlot()
    {
        if (customerShoppingBasket != null &&
            customerShoppingBasket.UsesBasket)
        {
            return customerShoppingBasket.GetCarryPoint(
                carriedItems.Count
            );
        }

        // Preserve the existing handheld behavior.
        if (carrySlots == null)
            return null;

        int nextIndex = carriedItems.Count;

        if (nextIndex >= carrySlots.Length)
            return null;

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

        var joinedQueue =
            checkoutQueue.TryJoinQueue(
                this
            );

        if (!joinedQueue)
        {
            currentState =
                CustomerState.WaitingForCheckoutSpace;

            return;
        }

        // CheckoutQueue will call
        // SetQueueDestination().
    }

    public void SetQueueDestination(Transform queuePoint)
    {
        if (queuePoint == null) return;

        // If this customer is already assigned to this exact queue
        // position, do not restart their checkout-walking state.
        //
        // This is especially important for the front customer:
        // another customer joining the back of the queue should not
        // interrupt an active checkout transaction.
        if (assignedQueuePoint == queuePoint &&
            (currentState == CustomerState.WalkingToCheckout ||
             currentState == CustomerState.WaitingInCheckoutQueue))
            return;

        assignedQueuePoint = queuePoint;
        currentState = CustomerState.WalkingToCheckout;

        var destinationAccepted = agent.SetDestination(queuePoint.position);

        if (!destinationAccepted) BeginLeaving();
    }

    private void UpdateWalkingToCheckout()
    {
        if (!HasReachedDestination()) return;

        agent.ResetPath();

        if (assignedQueuePoint != null)
            transform.rotation =
                assignedQueuePoint.rotation;

        currentState =
            CustomerState.WaitingInCheckoutQueue;
    }

    private void UpdateCheckoutWaiting()
    {
        checkoutWaitSeconds +=
            Time.deltaTime;
    }

    public bool IsReadyForCheckout()
    {
        return currentState
               ==
               CustomerState.WaitingInCheckoutQueue;
    }

    public int GetCarriedItemCount()
    {
        return carriedItems.Count;
    }

    public PickupItem GetCarriedItem(
        int index
    )
    {
        if (index < 0 ||
            index >= carriedItems.Count)
            return null;

        return carriedItems[index];
    }
    
    public void RestoreBasketItemForCheckout(PickupItem item)
    {
        if (customerShoppingBasket != null)
            customerShoppingBasket.RestoreItemForCheckout(item);
    }
    
    public ProductData GetCarriedProductData(
        int index
    )
    {
        if (index < 0 ||
            index >= carriedItems.Count)
            return null;

        var item =
            carriedItems[index];

        if (item == null) return null;

        return item.GetProductData();
    }

    public float GetCheckoutTotal()
    {
        var total = 0f;

        for (var i = 0;
             i < carriedItems.Count;
             i++)
        {
            var item =
                carriedItems[i];

            if (item == null) continue;

            var productData =
                item.GetProductData();

            if (productData == null) continue;

            total +=
                productData.SellPrice;
        }

        return total;
    }

    public bool CompleteCheckout(ShoppingBag shoppingBag)
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
            CheckoutQueue previousQueue = checkoutQueue;
            checkoutQueue = null;

            previousQueue.LeaveQueue(this);
        }

        for (int i = 0; i < carriedItems.Count; i++)
        {
            PickupItem item = carriedItems[i];

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        carriedItems.Clear();
        assignedQueuePoint = null;
        completedPurchase = true;
        
        
        if (customerShoppingBasket != null)
        {
            customerShoppingBasket.ClearAfterCheckout();
        }


        if (shoppingBag != null)
        {
            shoppingBag.AttachToCustomer(GetCheckoutBagCarryPoint());
        }

        PlayHappyReaction();
        BeginLeaving();

        return true;
    }
    
    private Transform GetCheckoutBagCarryPoint()
    {
        if (checkoutBagCarryPoint != null)
        {
            return checkoutBagCarryPoint;
        }

        if (carrySlots != null &&
            carrySlots.Length > 0 &&
            carrySlots[0] != null)
        {
            return carrySlots[0];
        }

        return transform;
    }

    private void BeginLeaving()
    {
        if (!completedPurchase && customerShoppingBasket != null)
            customerShoppingBasket.DiscardBasket();
        
        if (exitInsidePoint != null)
        {
            currentState = CustomerState.WalkingToExitApproach;

            bool destinationAccepted =
                agent.SetDestination(exitInsidePoint.position);

            if (destinationAccepted)
            {
                return;
            }
        }

        BeginWalkingOutside();
    }

    private void UpdateWalkingToExitApproach()
    {
        if (!HasReachedDestination())
        {
            return;
        }

        BeginWalkingOutside();
    }

    private void BeginWalkingOutside()
    {
        currentState = CustomerState.WalkingToExit;

        if (exitPoint == null)
        {
            FinishVisit();
            return;
        }

        bool destinationAccepted =
            agent.SetDestination(exitPoint.position);

        if (!destinationAccepted)
        {
            FinishVisit();
        }
    }

    private void UpdateWalkingToExit()
    {
        if (!HasReachedDestination()) return;

        FinishVisit();
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) return false;

        if (agent.remainingDistance
            > agent.stoppingDistance)
            return false;

        return true;
    }

    private void FinishVisit()
    {
        currentState =
            CustomerState.Finished;

        if (checkoutQueue != null)
            checkoutQueue.LeaveQueue(
                this
            );

        for (var i = 0;
             i < carriedItems.Count;
             i++)
        {
            var item =
                carriedItems[i];

            if (item != null)
                Destroy(
                    item.gameObject
                );
        }

        carriedItems.Clear();

        ReportVisitIfNeeded();

        Destroy(
            gameObject
        );
    }

    private void ReportVisitIfNeeded()
    {
        if (visitReported) return;

        visitReported = true;

        if (dailyStats == null) return;

        var waitPenalty =
            CalculateCheckoutWaitPenalty();

        var finalSatisfaction =
            satisfactionScore
            - waitPenalty;

        finalSatisfaction =
            Mathf.Clamp(
                finalSatisfaction,
                0f,
                100f
            );

        dailyStats.RecordCustomerVisit(
            finalSatisfaction,
            missedProductCount,
            completedPurchase
        );
    }

    private float CalculateCheckoutWaitPenalty()
    {
        var penalizedSeconds =
            Mathf.Max(
                0f,
                checkoutWaitSeconds
                - CheckoutGraceSeconds
            );

        var penalty =
            penalizedSeconds
            * CheckoutPenaltyPerSecond;

        return Mathf.Min(
            MaxCheckoutWaitPenalty,
            penalty
        );
    }

    private void PlayHappyReaction()
    {
        if (visual == null) return;

        if (happyReactionRoutine != null)
            StopCoroutine(
                happyReactionRoutine
            );

        happyReactionRoutine =
            StartCoroutine(
                HappyBounceRoutine()
            );
    }

    private IEnumerator HappyBounceRoutine()
    {
        var duration = 0.35f;

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;

            var t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            var scaleMultiplier =
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