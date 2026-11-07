using System.Collections.Generic;
using UnityEngine;

public class CustomerFlow : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField] private CustomerController customerPrefab;

    [Header("Spawn Timing")]
    [Min(0f)] [SerializeField] private float initialSpawnDelay = 3f;
    [Min(0.5f)] [SerializeField] private float minSpawnInterval = 6f;
    [Min(0.5f)] [SerializeField] private float maxSpawnInterval = 10f;

    [Header("Store Capacity")]
    [Min(1)] [SerializeField] private int maxCustomersInStore = 3;

    [Header("Shopping List")]
    [Min(1)] [SerializeField] private int minItemsPerCustomer = 1;
    [Min(1)] [SerializeField] private int maxItemsPerCustomer = 4;

    [Header("Store References")]
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private StoreReputation storeReputation;
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private ShelfRegistry shelfRegistry;
    [SerializeField] private CheckoutQueue checkoutQueue;
    [SerializeField] private CustomerBasketStation customerBasketStation;
    [SerializeField] private Transform customerSpawnPoint;
    [SerializeField] private Transform customerEntranceInsidePoint;
    [SerializeField] private Transform customerExitInsidePoint;
    [SerializeField] private Transform customerExitPoint;

    private readonly List<CustomerController> activeCustomers = new();

    private float spawnTimer;
    private bool layoutSuspended;

    public int ActiveCustomerCount => activeCustomers.Count;
    public bool IsLayoutSuspended => layoutSuspended;
    
    private void RefreshOperationalFixtures()
    {
        checkoutQueue = FindFirstObjectByType<CheckoutQueue>();
        customerBasketStation = FindFirstObjectByType<CustomerBasketStation>();
    }
    
    public CustomerController GetActiveCustomer(int index)
    {
        RemoveDestroyedCustomers();

        if (index < 0 ||
            index >= activeCustomers.Count)
        {
            return null;
        }

        return activeCustomers[index];
    }

    private void Start()
    {
        ResetSpawnTimerForNewDay();
    }

    private void Update()
    {
        RemoveDestroyedCustomers();

        if (layoutSuspended)
            return;

        if (!HasValidConfiguration())
            return;

        if (!storeClock.IsStoreOpen)
            return;

        if (activeCustomers.Count >= maxCustomersInStore)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        SpawnCustomer();
        ResetSpawnTimer();
    }

    public void SuspendForLayout()
    {
        if (layoutSuspended)
            return;

        layoutSuspended = true;

        RemoveDestroyedCustomers();

        foreach (CustomerController customer in activeCustomers)
        {
            if (customer != null)
                customer.SuspendForLayout();
        }
    }

    public void ResumeFromLayout()
    {
        if (!layoutSuspended)
            return;

        RemoveDestroyedCustomers();
        RefreshOperationalFixtures();

        foreach (CustomerController customer in activeCustomers)
        {
            if (customer == null)
            {
                continue;
            }

            customer.RefreshOperationalFixtures(
                checkoutQueue,
                customerBasketStation
            );

            customer.ResumeFromLayout();
        }

        layoutSuspended = false;
    }
    
    public void ClearForLoad()
    {
        bool preserveLayoutSuspension =
            layoutSuspended;

        RemoveDestroyedCustomers();

        foreach (CustomerController customer
                 in activeCustomers)
        {
            if (customer != null)
            {
                customer.DiscardForLoad();
            }
        }

        activeCustomers.Clear();

        if (checkoutQueue != null)
        {
            checkoutQueue.ClearForLoad();
        }

        layoutSuspended =
            preserveLayoutSuspension;

        ResetSpawnTimerForNewDay();
    }


    public void ResetForNewDay()
    {
        RemoveDestroyedCustomers();
        ResetSpawnTimerForNewDay();
    }

    private void SpawnCustomer()
    {
        RefreshOperationalFixtures();
        
        ProductData[] shoppingList = CreateShoppingList();

        if (shoppingList == null ||
            shoppingList.Length == 0)
        {
            return;
        }

        CustomerController customer = Instantiate(
            customerPrefab,
            customerSpawnPoint.position,
            customerSpawnPoint.rotation
        );

        customer.Configure(
            shoppingList,
            shelfRegistry,
            checkoutQueue,
            customerEntranceInsidePoint,
            customerExitInsidePoint,
            customerExitPoint,
            dailyStats,
            customerBasketStation
        );

        activeCustomers.Add(customer);
    }

    private ProductData[] CreateShoppingList()
    {
        int minimumItems =
            Mathf.Max(1, minItemsPerCustomer);

        int maximumItems =
            Mathf.Max(minimumItems, maxItemsPerCustomer);

        int itemCount =
            Random.Range(
                minimumItems,
                maximumItems + 1
            );

        ProductData[] shoppingList =
            new ProductData[itemCount];

        for (int i = 0; i < shoppingList.Length; i++)
        {
            ProductData product =
                GetRandomUnlockedProduct();

            if (product == null)
                return null;

            shoppingList[i] = product;
        }

        return shoppingList;
    }

    private ProductData GetRandomUnlockedProduct()
    {
        if (storeProgression == null)
            return null;

        int productCount =
            storeProgression.UnlockedProductCount;

        if (productCount <= 0)
            return null;

        int randomIndex =
            Random.Range(
                0,
                productCount
            );

        return storeProgression.GetUnlockedProduct(
            randomIndex
        );
    }

    private void RemoveDestroyedCustomers()
    {
        for (int i = activeCustomers.Count - 1;
             i >= 0;
             i--)
        {
            if (activeCustomers[i] != null)
                continue;

            activeCustomers.RemoveAt(i);
        }
    }

    private void ResetSpawnTimerForNewDay()
    {
        spawnTimer =
            Mathf.Max(
                0f,
                initialSpawnDelay
            );
    }

    private void ResetSpawnTimer()
    {
        float minimumInterval =
            Mathf.Max(
                0.5f,
                minSpawnInterval
            );

        float maximumInterval =
            Mathf.Max(
                minimumInterval,
                maxSpawnInterval
            );

        float reputationMultiplier =
            storeReputation.CustomerArrivalIntervalMultiplier;

        minimumInterval *= reputationMultiplier;
        maximumInterval *= reputationMultiplier;

        spawnTimer =
            Random.Range(
                minimumInterval,
                maximumInterval
            );
    }

    private bool HasValidConfiguration()
    {
        if (customerPrefab == null)
            return false;

        if (storeClock == null)
            return false;

        if (dailyStats == null)
            return false;

        if (storeReputation == null)
            return false;

        if (storeProgression == null)
            return false;

        if (storeProgression.UnlockedProductCount <= 0)
            return false;

        if (shelfRegistry == null)
            return false;
        
        if (customerSpawnPoint == null)
            return false;

        if (customerEntranceInsidePoint == null)
            return false;

        if (customerExitInsidePoint == null)
            return false;

        if (customerExitPoint == null)
            return false;

        return true;
    }
}