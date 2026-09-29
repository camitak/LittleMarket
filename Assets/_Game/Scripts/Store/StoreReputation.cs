using TMPro;
using UnityEngine;

public class StoreReputation : MonoBehaviour
{
    [Header("Starting Reputation")]
    [Range(0f, 100f)]
    [SerializeField]
    private float startingReputation = 50f;

    [Header("HUD")]
    [SerializeField]
    private TMP_Text reputationText;

    private float currentReputation;

    private float lastDailyChange;

    public float CurrentReputation =>
        currentReputation;

    public float LastDailyChange =>
        lastDailyChange;

    public float CustomerArrivalIntervalMultiplier
    {
        get
        {
            if (currentReputation <= 50f)
            {
                float t =
                    currentReputation / 50f;

                return Mathf.Lerp(
                    1.5f,
                    1f,
                    t
                );
            }

            float highReputationT =
                (currentReputation - 50f)
                / 50f;

            return Mathf.Lerp(
                1f,
                0.65f,
                highReputationT
            );
        }
    }

    private void Awake()
    {
        currentReputation =
            Mathf.Clamp(
                startingReputation,
                0f,
                100f
            );

        lastDailyChange = 0f;

        UpdateReputationUI();
    }

    public void ApplyDailySatisfaction(
        float averageSatisfaction,
        int customersVisited
    )
    {
        if (customersVisited <= 0)
        {
            lastDailyChange = 0f;

            UpdateReputationUI();

            return;
        }

        float clampedSatisfaction =
            Mathf.Clamp(
                averageSatisfaction,
                0f,
                100f
            );

        float requestedChange =
            (clampedSatisfaction - 70f)
            * 0.2f;

        requestedChange =
            Mathf.Clamp(
                requestedChange,
                -8f,
                6f
            );

        float previousReputation =
            currentReputation;

        currentReputation =
            Mathf.Clamp(
                previousReputation
                + requestedChange,
                0f,
                100f
            );

        currentReputation =
            RoundToOneDecimal(
                currentReputation
            );

        lastDailyChange =
            RoundToOneDecimal(
                currentReputation
                - previousReputation
            );

        UpdateReputationUI();
    }

    private float RoundToOneDecimal(
        float value
    )
    {
        return Mathf.Round(
            value * 10f
        ) / 10f;
    }

    private void UpdateReputationUI()
    {
        if (reputationText == null)
        {
            return;
        }

        reputationText.text =
            "REP "
            + currentReputation
                .ToString("0");
    }
}