using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject orderingPanel;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Transform productListContent;

    [SerializeField]
    private OrderingProductRow productRowPrefab;

    [Header("Catalog")]
    [SerializeField]
    private StoreProductCatalog productCatalog;

    [Header("Order")]
    [Min(1)]
    [SerializeField]
    private int quantityPerBox = 4;

    [SerializeField]
    private DeliveryBox deliveryBoxPrefab;

    [SerializeField]
    private DeliveryZone deliveryZone;

    [Header("Store")]
    [SerializeField]
    private StoreEconomy storeEconomy;

    [SerializeField]
    private DailyStats dailyStats;

    [Header("Player")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    [SerializeField]
    private InteractionUI interactionUI;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closeButton.onClick.AddListener(
            Close
        );

        BuildCatalogRows();

        orderingPanel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        statusText.text = "";

        orderingPanel.SetActive(true);

        interactionUI.HidePrompt();

        playerController.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        orderingPanel.SetActive(false);

        playerController.enabled = true;
        playerInteraction.enabled = true;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    private void BuildCatalogRows()
    {
        if (productListContent == null)
        {
            return;
        }

        if (productRowPrefab == null)
        {
            return;
        }

        if (productCatalog == null)
        {
            return;
        }

        for (int i = 0;
             i < productCatalog.ProductCount;
             i++)
        {
            ProductData productData =
                productCatalog.GetProduct(i);

            if (productData == null)
            {
                continue;
            }

            OrderingProductRow newRow =
                Instantiate(
                    productRowPrefab,
                    productListContent
                );

            newRow.Configure(
                productData,
                quantityPerBox,
                OrderProduct
            );
        }
    }

    private void OrderProduct(
        ProductData productData
    )
    {
        if (productData == null)
        {
            statusText.text =
                "Product not configured.";

            return;
        }

        if (deliveryBoxPrefab == null)
        {
            statusText.text =
                "Delivery box not configured.";

            return;
        }

        if (deliveryZone == null)
        {
            statusText.text =
                "Delivery zone not configured.";

            return;
        }

        if (storeEconomy == null)
        {
            statusText.text =
                "Store economy not configured.";

            return;
        }

        if (!deliveryZone.HasSpace)
        {
            statusText.text =
                "Delivery area full. "
                + "Remove an empty box.";

            return;
        }

        float orderCost =
            productData.BuyPrice
            * quantityPerBox;

        bool purchaseSucceeded =
            storeEconomy.TrySpend(
                orderCost
            );

        if (!purchaseSucceeded)
        {
            statusText.text =
                "Not enough money.";

            return;
        }

        bool deliveryCreated =
            deliveryZone.TryCreateDelivery(
                deliveryBoxPrefab,
                productData,
                quantityPerBox,
                out DeliveryBox newDelivery
            );

        if (!deliveryCreated)
        {
            storeEconomy.AddMoney(
                orderCost
            );

            statusText.text =
                "Delivery failed. "
                + "Order was refunded.";

            return;
        }

        if (dailyStats != null)
        {
            dailyStats.RecordStockSpending(
                orderCost
            );
        }

        statusText.text =
            "Ordered "
            + quantityPerBox
            + " x "
            + productData.ProductName
            + " for £"
            + orderCost.ToString("0.00")
            + ".";
    }
}