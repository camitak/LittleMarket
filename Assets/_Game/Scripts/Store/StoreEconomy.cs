using TMPro;
using UnityEngine;

public class StoreEconomy : MonoBehaviour
{
    [Header("Money")]
    [SerializeField] private float startingMoney = 100f;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;

    private float currentMoney;

    public float CurrentMoney => currentMoney;

    private void Awake()
    {
        currentMoney = startingMoney;

        UpdateMoneyUI();
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0f)
        {
            return false;
        }

        if (currentMoney < amount)
        {
            return false;
        }

        currentMoney -= amount;

        UpdateMoneyUI();

        return true;
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentMoney += amount;

        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text = "£" + currentMoney.ToString("0.00");
    }
}