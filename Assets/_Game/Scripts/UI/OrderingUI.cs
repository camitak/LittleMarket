using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OrderingUI : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private GameObject orderingPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform productListContent;
    [SerializeField] private OrderingProductRow productRowPrefab;

    [Header("Progression")] 
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private StoreStockOverview storeStockOverview;

    [Header("Order")] 
    [Min(1)] [SerializeField] private int quantityPerBox = 4;
    [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [SerializeField] private DeliveryZone deliveryZone;
    [SerializeField] private SupplierDeliveryQueue supplierDeliveryQueue;

    [Header("Store")] 
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private DailyStats dailyStats;

    [Header("Player")] 
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InteractionUI interactionUI;

    private List<OrderingProductRow> spawnedRows = new();

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closeButton.onClick.AddListener(
            Close
        );

        orderingPanel.SetActive(
            false
        );
    }

    public void Open()
    {
        if (isOpen) return;

        if (playerInteraction != null &&
            playerInteraction.HasAnythingHeld)
            return;

        isOpen = true;

        RebuildCatalogRows();

        statusText.text =
            supplierDeliveryQueue != null
                ? supplierDeliveryQueue.GetTerminalStatusText()
                : "";

        orderingPanel.SetActive(
            true
        );

        interactionUI.HidePrompt();

        playerController.enabled =
            false;

        playerInteraction.enabled =
            false;

        playerController.ReleaseCursor();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        orderingPanel.SetActive(
            false
        );

        playerController.enabled =
            true;

        playerInteraction.enabled =
            true;

        StartCoroutine(
            RecaptureCursorAfterClose()
        );
    }

    private IEnumerator
        RecaptureCursorAfterClose()
    {
        /*
         * Wait until the UI Button mouse-release
         * event has finished.
         *
         * Otherwise the same click that closes the
         * panel can leave the cursor released in the
         * Unity Game view.
         */
        yield return null;

        if (EventSystem.current != null)
            EventSystem.current
                .SetSelectedGameObject(
                    null
                );

        if (playerController != null && playerController.enabled)
            playerController.CaptureCursor();
    }

    private void RebuildCatalogRows()
    {
        ClearSpawnedRows();

        if (productListContent == null) return;

        if (productRowPrefab == null) return;

        if (storeProgression == null) return;

        for (var i = 0;
             i < storeProgression.CatalogProductCount;
             i++)
        {
            var product =
                storeProgression.GetCatalogProduct(
                    i
                );

            if (product == null) continue;

            var newRow =
                Instantiate(
                    productRowPrefab,
                    productListContent
                );

            spawnedRows.Add(
                newRow
            );

            var isUnlocked =
                storeProgression
                    .IsProductUnlocked(
                        product
                    );

            if (isUnlocked)
                newRow.Configure(
                    product,
                    quantityPerBox,
                    OrderProduct,
                    storeProgression,
                    storeStockOverview
                );
            else
                newRow.ConfigureLocked(
                    product,
                    storeProgression,
                    storeStockOverview
                );
        }
    }

    private void ClearSpawnedRows()
    {
        for (var i = 0;
             i < spawnedRows.Count;
             i++)
        {
            var row =
                spawnedRows[i];

            if (row == null) continue;

            row.gameObject.SetActive(
                false
            );

            Destroy(
                row.gameObject
            );
        }

        spawnedRows.Clear();
    }

    private void OrderProduct(ProductData productData)
    {
        if (productData == null)
        {
            statusText.text = "Product not configured.";
            return;
        }

        if (storeProgression == null)
        {
            statusText.text = "Store progression not configured.";
            return;
        }

        if (!storeProgression.IsProductUnlocked(productData))
        {
            statusText.text = "This product is still locked.";
            return;
        }

        if (deliveryBoxPrefab == null)
        {
            statusText.text = "Delivery box not configured.";
            return;
        }

        if (deliveryZone == null)
        {
            statusText.text = "Delivery zone not configured.";
            return;
        }

        if (supplierDeliveryQueue == null)
        {
            statusText.text = "Supplier delivery schedule not configured.";
            return;
        }

        if (storeEconomy == null)
        {
            statusText.text = "Store economy not configured.";
            return;
        }

        bool arrivesTomorrow = supplierDeliveryQueue.IsPastCutoff;

        if (!arrivesTomorrow && !deliveryZone.HasSpace)
        {
            statusText.text =
                "Delivery area full. Move a carton out of the receiving area.";

            return;
        }

        if (arrivesTomorrow &&
            !supplierDeliveryQueue.CanQueueTomorrowOrder)
        {
            statusText.text =
                "Tomorrow's delivery queue is full.";

            return;
        }

        float orderCost =
            productData.BuyPrice * quantityPerBox;

        bool purchaseSucceeded =
            storeEconomy.TrySpend(orderCost);

        if (!purchaseSucceeded)
        {
            statusText.text = "Not enough money.";
            return;
        }

        if (arrivesTomorrow)
        {
            bool queued =
                supplierDeliveryQueue.TryQueueForTomorrow(
                    productData,
                    quantityPerBox
                );

            if (!queued)
            {
                storeEconomy.AddMoney(orderCost);
                statusText.text =
                    "Could not schedule tomorrow's delivery. Order was refunded.";

                return;
            }

            if (dailyStats != null)
            {
                dailyStats.RecordStockSpending(orderCost);
            }

            statusText.text =
                $"Ordered {quantityPerBox} x {productData.ProductName} for £{orderCost:0.00}.\n" +
                $"Arrives Day {supplierDeliveryQueue.NextOrderArrivalDay} • " +
                $"Queue {supplierDeliveryQueue.PendingOrderCount} / {supplierDeliveryQueue.QueueCapacity}";

            return;
        }

        bool deliveryCreated =
            deliveryZone.TryCreateDelivery(
                deliveryBoxPrefab,
                productData,
                quantityPerBox,
                out _
            );

        if (!deliveryCreated)
        {
            storeEconomy.AddMoney(orderCost);

            statusText.text =
                "Delivery failed. Order was refunded.";

            return;
        }

        if (dailyStats != null)
        {
            dailyStats.RecordStockSpending(orderCost);
        }

        statusText.text =
            $"Ordered {quantityPerBox} x {productData.ProductName} for £{orderCost:0.00}. " +
            "Delivered now.";
    }
}