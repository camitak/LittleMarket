using TMPro;
using UnityEngine;

public class StorageCapacityUI : MonoBehaviour
{
    [Header("UI")] [SerializeField] private TMP_Text hudCapacityText;

    [SerializeField] private TMP_Text rackCapacityText;

    [Header("Store")] [SerializeField] private StorageRegistry storageRegistry;

    [Header("Refresh")] [Min(0.05f)] [SerializeField]
    private float refreshInterval = 0.25f;

    private float refreshTimer;

    private static readonly Color32 AvailableColor =
        new(
            111,
            207,
            151,
            255
        );

    private static readonly Color32 NearlyFullColor =
        new(
            246,
            215,
            122,
            255
        );

    private static readonly Color32 FullColor =
        new(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 NeutralColor =
        new(
            122,
            104,
            96,
            255
        );

    private void Start()
    {
        RefreshDisplay();
    }

    private void Update()
    {
        refreshTimer +=
            Time.unscaledDeltaTime;

        if (refreshTimer
            < refreshInterval)
            return;

        refreshTimer =
            0f;

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (storageRegistry == null)
        {
            ApplyText(
                hudCapacityText,
                "RESERVE -- / --",
                NeutralColor
            );

            ApplyText(
                rackCapacityText,
                "RESERVE STOCK\nNOT CONFIGURED",
                NeutralColor
            );

            return;
        }

        var capacity =
            storageRegistry
                .RegisteredSlotCount;

        var occupied =
            storageRegistry
                .OccupiedSlotCount;

        var empty =
            storageRegistry
                .EmptySlotCount;

        if (capacity <= 0)
        {
            ApplyText(
                hudCapacityText,
                "RESERVE 0 / 0",
                NeutralColor
            );

            ApplyText(
                rackCapacityText,
                "RESERVE STOCK\n0 / 0",
                NeutralColor
            );

            return;
        }

        var statusColor =
            GetCapacityColor(
                empty
            );

        string hudText;

        string rackText;

        if (storageRegistry.IsFull)
        {
            hudText =
                "RESERVE FULL  •  "
                + occupied
                + " / "
                + capacity;

            rackText =
                "RESERVE STOCK\n"
                + "FULL  •  "
                + occupied
                + " / "
                + capacity;
        }
        else
        {
            hudText =
                "RESERVE "
                + occupied
                + " / "
                + capacity
                + "  •  "
                + empty
                + " FREE";

            rackText =
                "RESERVE STOCK\n"
                + occupied
                + " / "
                + capacity
                + "  •  "
                + empty
                + " FREE";
        }

        ApplyText(
            hudCapacityText,
            hudText,
            statusColor
        );

        ApplyText(
            rackCapacityText,
            rackText,
            statusColor
        );
    }

    private Color32 GetCapacityColor(
        int emptySlots
    )
    {
        if (emptySlots <= 0) return FullColor;

        if (emptySlots <= 2) return NearlyFullColor;

        return AvailableColor;
    }

    private void ApplyText(
        TMP_Text target,
        string value,
        Color32 color
    )
    {
        if (target == null) return;

        target.text = value;

        target.color = color;
    }
}