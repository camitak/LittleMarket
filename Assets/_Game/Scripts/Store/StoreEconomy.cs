using TMPro;
using UnityEngine;

public class StoreEconomy : MonoBehaviour
{
    [Header("Money")]
    [Min(0f)]
    [SerializeField]
    private float startingMoney = 100f;

    [Header("UI")]
    [SerializeField]
    private TMP_Text moneyText;

    private float currentMoney;

    public float CurrentMoney =>
        currentMoney;

    private void Awake()
    {
        currentMoney =
            Mathf.Max(
                0f,
                startingMoney
            );

        UpdateMoneyUI();
    }

    public bool TrySpend(
        float amount
    )
    {
        if (amount <= 0f)
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

    public void AddMoney(
        float amount
    )
    {
        if (amount <= 0f)
        {
            return;
        }

        currentMoney += amount;

        UpdateMoneyUI();
    }

    public void SetMoney(
        float newMoney
    )
    {
        currentMoney =
            Mathf.Max(
                0f,
                newMoney
            );

        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text =
            "£"
            + currentMoney.ToString("0.00");
    }
}