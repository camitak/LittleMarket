using System.Collections.Generic;
using UnityEngine;

public class CheckoutQueue : MonoBehaviour
{
    [Header("Queue")] [SerializeField] private Transform[] queuePoints;

    private List<CustomerController> customers = new();

    public bool HasSpace
    {
        get
        {
            if (queuePoints == null) return false;

            return customers.Count < queuePoints.Length;
        }
    }

    public bool TryJoinQueue(CustomerController customer)
    {
        if (customer == null) return false;

        if (customers.Contains(customer)) return true;

        if (!HasSpace) return false;

        var nextPoint =
            queuePoints[customers.Count];

        if (nextPoint == null) return false;

        customers.Add(customer);

        UpdateCustomerDestinations();

        return true;
    }

    public void LeaveQueue(CustomerController customer)
    {
        if (customer == null) return;

        var removed =
            customers.Remove(customer);

        if (!removed) return;

        UpdateCustomerDestinations();
    }

    public CustomerController GetFrontCustomer()
    {
        if (customers.Count == 0) return null;

        return customers[0];
    }

    private void UpdateCustomerDestinations()
    {
        for (var i = 0; i < customers.Count; i++)
        {
            var customer =
                customers[i];

            var queuePoint =
                queuePoints[i];

            if (customer == null) continue;

            if (queuePoint == null) continue;

            customer.SetQueueDestination(
                queuePoint
            );
        }
    }
}