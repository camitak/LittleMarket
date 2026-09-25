using System.Collections.Generic;
using UnityEngine;

public class CustomerFlow : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField]
    private CustomerController customerPrefab;

    [SerializeField]
    private ProductData[] availableProducts;

    [Header("Spawn Timing")]
    [Min(0f)]
    [SerializeField]
    private float initialSpawnDelay = 3f;

    [Min(0.5f)]
    [SerializeField]
    private float minSpawnInterval = 6f;

    [Min(0.5f)]
    [SerializeField]
    private float maxSpawnInterval = 10f;

    [Header("Store Capacity")]
    [Min(1)]
    [SerializeField]
    private int maxCustomersInStore = 3;

    [Header("Shopping List")]
    [Range(1, 3)]
    [SerializeField]
    private int minItemsPerCustomer = 1;

    [Range(1, 3)]
    [SerializeField]
    private int maxItemsPerCustomer = 2;

    [Header("Store References")]
    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private CheckoutQueue checkoutQueue;

    [SerializeField]
    private Transform customerSpawnPoint;

    [SerializeField]
    private Transform customerExitPoint;

    private List<CustomerController> activeCustomers =
        new List<CustomerController>();

    private float spawnTimer;

    private void Start()
    {
        spawnTimer =
            Mathf.Max(
                0f,
                initialSpawnDelay
            );
    }

    private void Update()
    {
        RemoveDestroyedCustomers();

        if (!HasValidConfiguration())
        {
            return;
        }

        if (activeCustomers.Count >= maxCustomersInStore)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnCustomer();

        ResetSpawnTimer();
    }

    private void SpawnCustomer()
    {
        ProductData[] shoppingList =
            CreateShoppingList();

        if (shoppingList == null)
        {
            return;
        }

        if (shoppingList.Length == 0)
        {
            return;
        }

        CustomerController customer =
            Instantiate(
                customerPrefab,
                customerSpawnPoint.position,
                customerSpawnPoint.rotation
            );

        customer.Configure(
            shoppingList,
            shelfRegistry,
            checkoutQueue,
            customerExitPoint
        );

        activeCustomers.Add(
            customer
        );
    }

    private ProductData[] CreateShoppingList()
    {
        int minimumItems =
            Mathf.Clamp(
                minItemsPerCustomer,
                1,
                3
            );

        int maximumItems =
            Mathf.Clamp(
                maxItemsPerCustomer,
                minimumItems,
                3
            );

        int itemCount =
            Random.Range(
                minimumItems,
                maximumItems + 1
            );

        ProductData[] shoppingList =
            new ProductData[itemCount];

        for (int i = 0;
             i < shoppingList.Length;
             i++)
        {
            ProductData product =
                GetRandomAvailableProduct();

            if (product == null)
            {
                return null;
            }

            shoppingList[i] =
                product;
        }

        return shoppingList;
    }

    private ProductData GetRandomAvailableProduct()
    {
        if (availableProducts == null)
        {
            return null;
        }

        if (availableProducts.Length == 0)
        {
            return null;
        }

        int startIndex =
            Random.Range(
                0,
                availableProducts.Length
            );

        for (int offset = 0;
             offset < availableProducts.Length;
             offset++)
        {
            int index =
                (startIndex + offset)
                % availableProducts.Length;

            ProductData product =
                availableProducts[index];

            if (product != null)
            {
                return product;
            }
        }

        return null;
    }

    private void RemoveDestroyedCustomers()
    {
        for (int i = activeCustomers.Count - 1;
             i >= 0;
             i--)
        {
            if (activeCustomers[i] != null)
            {
                continue;
            }

            activeCustomers.RemoveAt(i);
        }
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

        spawnTimer =
            Random.Range(
                minimumInterval,
                maximumInterval
            );
    }

    private bool HasValidConfiguration()
    {
        if (customerPrefab == null)
        {
            return false;
        }

        if (!HasAtLeastOneProduct())
        {
            return false;
        }

        if (shelfRegistry == null)
        {
            return false;
        }

        if (checkoutQueue == null)
        {
            return false;
        }

        if (customerSpawnPoint == null)
        {
            return false;
        }

        if (customerExitPoint == null)
        {
            return false;
        }

        return true;
    }

    private bool HasAtLeastOneProduct()
    {
        if (availableProducts == null)
        {
            return false;
        }

        for (int i = 0;
             i < availableProducts.Length;
             i++)
        {
            if (availableProducts[i] != null)
            {
                return true;
            }
        }

        return false;
    }
}