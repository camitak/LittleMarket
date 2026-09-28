using UnityEngine;

public class DailyStats : MonoBehaviour
{
    public float Revenue { get; private set; }

    public float StockSpending { get; private set; }

    public int ItemsSold { get; private set; }

    public int CustomersServed { get; private set; }

    public float NetCashFlow =>
        Revenue - StockSpending;

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

    public void ResetForNewDay()
    {
        Revenue = 0f;
        StockSpending = 0f;
        ItemsSold = 0;
        CustomersServed = 0;
    }
}