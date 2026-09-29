using TMPro;
using UnityEngine;

public class StoreClock : MonoBehaviour
{
    [Header("Day")]
    [Min(1)]
    [SerializeField]
    private int startingDay = 1;

    [Header("Starting Time")]
    [Range(0, 23)]
    [SerializeField]
    private int startHour = 8;

    [Range(0, 59)]
    [SerializeField]
    private int startMinute = 0;

    [Header("Store Schedule")]
    [Range(0, 23)]
    [SerializeField]
    private int openingHour = 8;

    [Range(0, 59)]
    [SerializeField]
    private int openingMinute = 0;

    [Range(0, 23)]
    [SerializeField]
    private int closingHour = 21;

    [Range(0, 59)]
    [SerializeField]
    private int closingMinute = 0;

    [Range(0, 23)]
    [SerializeField]
    private int dayEndHour = 22;

    [Range(0, 59)]
    [SerializeField]
    private int dayEndMinute = 0;

    [Header("Time Speed")]
    [Min(0.1f)]
    [SerializeField]
    private float gameMinutesPerRealSecond = 2f;

    [Header("UI")]
    [SerializeField]
    private TMP_Text dayText;

    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private TMP_Text statusText;

    private float currentMinutes;

    private int currentDay;

    private bool hasDayEnded;

    private static readonly Color32 OpenStatusColor =
        new Color32(
            111,
            207,
            151,
            255
        );

    private static readonly Color32 ClosedStatusColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 EndedStatusColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    public bool IsStoreOpen
    {
        get
        {
            if (hasDayEnded)
            {
                return false;
            }

            return currentMinutes
                   >= OpeningTimeMinutes
                   &&
                   currentMinutes
                   < ClosingTimeMinutes;
        }
    }

    public bool HasDayEnded =>
        hasDayEnded;

    public int CurrentDay =>
        currentDay;

    public float CurrentMinutes =>
        currentMinutes;

    private float OpeningTimeMinutes =>
        openingHour * 60f
        + openingMinute;

    private float ClosingTimeMinutes =>
        closingHour * 60f
        + closingMinute;

    private float DayEndTimeMinutes =>
        dayEndHour * 60f
        + dayEndMinute;

    private void Awake()
    {
        currentDay =
            Mathf.Max(
                1,
                startingDay
            );

        ResetClockToStartTime();
    }

    private void Update()
    {
        if (hasDayEnded)
        {
            return;
        }

        currentMinutes +=
            gameMinutesPerRealSecond
            * Time.deltaTime;

        if (currentMinutes
            >= DayEndTimeMinutes)
        {
            currentMinutes =
                DayEndTimeMinutes;

            hasDayEnded =
                true;
        }

        UpdateClockUI();
    }

    public void StartNextDay()
    {
        currentDay++;

        ResetClockToStartTime();
    }

    public void LoadDay(
        int day
    )
    {
        currentDay =
            Mathf.Max(
                1,
                day
            );

        ResetClockToStartTime();
    }

    public void LoadState(
        int day,
        float savedCurrentMinutes
    )
    {
        currentDay =
            Mathf.Max(
                1,
                day
            );

        currentMinutes =
            Mathf.Clamp(
                savedCurrentMinutes,
                0f,
                1439f
            );

        hasDayEnded =
            false;

        if (currentMinutes
            >= DayEndTimeMinutes)
        {
            currentMinutes =
                DayEndTimeMinutes;

            hasDayEnded =
                true;
        }

        UpdateClockUI();
    }

    private void ResetClockToStartTime()
    {
        currentMinutes =
            startHour * 60f
            + startMinute;

        currentMinutes =
            Mathf.Clamp(
                currentMinutes,
                0f,
                1439f
            );

        hasDayEnded =
            false;

        if (currentMinutes
            >= DayEndTimeMinutes)
        {
            currentMinutes =
                DayEndTimeMinutes;

            hasDayEnded =
                true;
        }

        UpdateClockUI();
    }

    private void UpdateClockUI()
    {
        UpdateDayText();

        UpdateTimeText();

        UpdateStatusText();
    }

    private void UpdateDayText()
    {
        if (dayText == null)
        {
            return;
        }

        dayText.text =
            "DAY "
            + currentDay;
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
        {
            return;
        }

        int wholeMinutes =
            Mathf.FloorToInt(
                currentMinutes
            );

        int hours =
            wholeMinutes / 60;

        int minutes =
            wholeMinutes % 60;

        timeText.text =
            hours.ToString("00")
            + ":"
            + minutes.ToString("00");
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
        {
            return;
        }

        if (hasDayEnded)
        {
            statusText.text =
                "DAY ENDED";

            statusText.color =
                EndedStatusColor;

            return;
        }

        if (IsStoreOpen)
        {
            statusText.text =
                "OPEN";

            statusText.color =
                OpenStatusColor;

            return;
        }

        statusText.text =
            "CLOSED";

        statusText.color =
            ClosedStatusColor;
    }
}