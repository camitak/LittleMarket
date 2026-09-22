using System.Collections.Generic;
using UnityEngine;

public class CheckoutQueue : MonoBehaviour
{
    [Header("Queue")]
    [SerializeField] private Transform[] queuePoints;

    private List<CustomerController> customers =
        new List<CustomerController>();

    public bool HasSpace
    {
        get
        {
            if (queuePoints == null)
            {
                return false;
            }

            return customers.Count < queuePoints.Length;
        }
    }

    public bool TryJoinQueue(CustomerController customer)
    {
        if (customer == null)
        {
            return false;
        }

        if (customers.Contains(customer))
        {
            return true;
        }

        if (!HasSpace)
        {
            return false;
        }

        Transform nextPoint =
            queuePoints[customers.Count];

        if (nextPoint == null)
        {
            return false;
        }

        customers.Add(customer);

        UpdateCustomerDestinations();

        return true;
    }

    public void LeaveQueue(CustomerController customer)
    {
        if (customer == null)
        {
            return;
        }

        bool removed =
            customers.Remove(customer);

        if (!removed)
        {
            return;
        }

        UpdateCustomerDestinations();
    }

    private void UpdateCustomerDestinations()
    {
        for (int i = 0; i < customers.Count; i++)
        {
            CustomerController customer =
                customers[i];

            Transform queuePoint =
                queuePoints[i];

            if (customer == null)
            {
                continue;
            }

            if (queuePoint == null)
            {
                continue;
            }

            customer.SetQueueDestination(
                queuePoint
            );
        }
    }
}