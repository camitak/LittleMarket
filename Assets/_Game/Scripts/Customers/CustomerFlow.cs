using System.Collections.Generic;
using UnityEngine;

public class CustomerFlow : MonoBehaviour
{
    [Header("Customer")] [SerializeField] private CustomerController customerPrefab;

    [Header("Spawn Timing")] [Min(0f)] [SerializeField]
    private float initialSpawnDelay = 3f;

    [Min(0.5f)] [SerializeField] private float minSpawnInterval = 6f;

    [Min(0.5f)] [SerializeField] private float maxSpawnInterval = 10f;

    [Header("Store Capacity")] [Min(1)] [SerializeField]
    private int maxCustomersInStore = 3;

    [Header("Shopping List")] [Range(1, 3)] [SerializeField]
    private int minItemsPerCustomer = 1;

    [Range(1, 3)] [SerializeField] private int maxItemsPerCustomer = 2;

    [Header("Store References")] [SerializeField]
    private StoreClock storeClock;

    [SerializeField] private DailyStats dailyStats;

    [SerializeField] private StoreReputation storeReputation;

    [SerializeField] private StoreProgression storeProgression;

    [SerializeField] private ShelfRegistry shelfRegistry;

    [SerializeField] private CheckoutQueue checkoutQueue;

    [SerializeField] private Transform customerSpawnPoint;

    [SerializeField] private Transform customerExitPoint;

    private List<CustomerController> activeCustomers = new();

    private float spawnTimer;

    public int ActiveCustomerCount =>
        activeCustomers.Count;

    private void Start()
    {
        ResetSpawnTimerForNewDay();
    }

    private void Update()
    {
        RemoveDestroyedCustomers();

        if (!HasValidConfiguration()) return;

        if (!storeClock.IsStoreOpen) return;

        if (activeCustomers.Count
            >= maxCustomersInStore)
            return;

        spawnTimer -=
            Time.deltaTime;

        if (spawnTimer > 0f) return;

        SpawnCustomer();

        ResetSpawnTimer();
    }

    public void ResetForNewDay()
    {
        RemoveDestroyedCustomers();

        ResetSpawnTimerForNewDay();
    }

    private void SpawnCustomer()
    {
        var shoppingList =
            CreateShoppingList();

        if (shoppingList == null) return;

        if (shoppingList.Length == 0) return;

        var customer =
            Instantiate(
                customerPrefab,
                customerSpawnPoint.position,
                customerSpawnPoint.rotation
            );

        customer.Configure(
            shoppingList,
            shelfRegistry,
            checkoutQueue,
            customerExitPoint,
            dailyStats
        );

        activeCustomers.Add(
            customer
        );
    }

    private ProductData[] CreateShoppingList()
    {
        var minimumItems =
            Mathf.Clamp(
                minItemsPerCustomer,
                1,
                3
            );

        var maximumItems =
            Mathf.Clamp(
                maxItemsPerCustomer,
                minimumItems,
                3
            );

        var itemCount =
            Random.Range(
                minimumItems,
                maximumItems + 1
            );

        var shoppingList =
            new ProductData[itemCount];

        for (var i = 0;
             i < shoppingList.Length;
             i++)
        {
            var product =
                GetRandomUnlockedProduct();

            if (product == null) return null;

            shoppingList[i] =
                product;
        }

        return shoppingList;
    }

    private ProductData GetRandomUnlockedProduct()
    {
        if (storeProgression == null) return null;

        var productCount =
            storeProgression.UnlockedProductCount;

        if (productCount <= 0) return null;

        var randomIndex =
            Random.Range(
                0,
                productCount
            );

        return storeProgression
            .GetUnlockedProduct(
                randomIndex
            );
    }

    private void RemoveDestroyedCustomers()
    {
        for (var i =
                 activeCustomers.Count - 1;
             i >= 0;
             i--)
        {
            if (activeCustomers[i] != null) continue;

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
        var minimumInterval =
            Mathf.Max(
                0.5f,
                minSpawnInterval
            );

        var maximumInterval =
            Mathf.Max(
                minimumInterval,
                maxSpawnInterval
            );

        var reputationMultiplier =
            storeReputation
                .CustomerArrivalIntervalMultiplier;

        minimumInterval *=
            reputationMultiplier;

        maximumInterval *=
            reputationMultiplier;

        spawnTimer =
            Random.Range(
                minimumInterval,
                maximumInterval
            );
    }

    private bool HasValidConfiguration()
    {
        if (customerPrefab == null) return false;

        if (storeClock == null) return false;

        if (dailyStats == null) return false;

        if (storeReputation == null) return false;

        if (storeProgression == null) return false;

        if (storeProgression
                .UnlockedProductCount <= 0)
            return false;

        if (shelfRegistry == null) return false;

        if (checkoutQueue == null) return false;

        if (customerSpawnPoint == null) return false;

        if (customerExitPoint == null) return false;

        return true;
    }
}