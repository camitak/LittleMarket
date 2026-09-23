using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField]
    private CustomerController customerPrefab;

    [SerializeField]
    private ProductData[] testShoppingList;

    [Header("Store")]
    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private CheckoutQueue checkoutQueue;

    [SerializeField]
    private Transform customerSpawnPoint;

    [SerializeField]
    private Transform customerExitPoint;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null)
        {
            return;
        }

        if (testShoppingList == null)
        {
            return;
        }

        if (testShoppingList.Length == 0)
        {
            return;
        }

        if (shelfRegistry == null)
        {
            return;
        }

        if (checkoutQueue == null)
        {
            return;
        }

        if (customerSpawnPoint == null)
        {
            return;
        }

        if (customerExitPoint == null)
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
            testShoppingList,
            shelfRegistry,
            checkoutQueue,
            customerExitPoint
        );
    }
}