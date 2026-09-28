using UnityEngine;

public class DailyStats : MonoBehaviour
{
    public float Revenue { get; private set; }

    public float StockSpending { get; private set; }

    public int ItemsSold { get; private set; }

    public int CustomersServed { get; private set; }

    public int CustomersVisited { get; private set; }

    public int CustomersLeftWithoutBuying { get; private set; }

    public int MissedProducts { get; private set; }

    private float totalSatisfaction;

    public float NetCashFlow =>
        Revenue - StockSpending;

    public float AverageSatisfaction
    {
        get
        {
            if (CustomersVisited <= 0)
            {
                return 0f;
            }

            return totalSatisfaction
                   / CustomersVisited;
        }
    }

    public void RecordStockSpending(
        float amount
    )
    {
        if (amount <= 0f)
        {
            return;
        }

        StockSpending += amount;
    }

    public void RecordSale(
        float amount,
        int itemCount
    )
    {
        if (amount <= 0f)
        {
            return;
        }

        if (itemCount <= 0)
        {
            return;
        }

        Revenue += amount;

        ItemsSold += itemCount;

        CustomersServed++;
    }

    public void RecordCustomerVisit(
        float satisfaction,
        int missedProducts,
        bool completedPurchase
    )
    {
        CustomersVisited++;

        totalSatisfaction +=
            Mathf.Clamp(
                satisfaction,
                0f,
                100f
            );

        MissedProducts +=
            Mathf.Max(
                0,
                missedProducts
            );

        if (!completedPurchase)
        {
            CustomersLeftWithoutBuying++;
        }
    }

    public void ResetForNewDay()
    {
        Revenue = 0f;

        StockSpending = 0f;

        ItemsSold = 0;

        CustomersServed = 0;

        CustomersVisited = 0;

        CustomersLeftWithoutBuying = 0;

        MissedProducts = 0;

        totalSatisfaction = 0f;
    }
}