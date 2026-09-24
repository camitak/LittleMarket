using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject orderingPanel;

    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button closeButton;

    [SerializeField]
    private OrderCatalogEntry[] catalogEntries;

    [Header("Order")]
    [Min(1)]
    [SerializeField] private int quantityPerBox = 4;

    [SerializeField] private DeliveryBox deliveryBoxPrefab;

    [SerializeField] private Transform deliverySpawnPoint;

    [Header("Store")]
    [SerializeField] private StoreEconomy storeEconomy;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [SerializeField] private PlayerInteraction playerInteraction;

    [SerializeField] private InteractionUI interactionUI;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        SetupCatalogButtons();

        closeButton.onClick.AddListener(Close);

        orderingPanel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        RefreshCatalogDisplay();

        statusText.text = "";

        orderingPanel.SetActive(true);

        interactionUI.HidePrompt();

        playerController.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupCatalogButtons()
    {
        if (catalogEntries == null)
        {
            return;
        }

        for (int i = 0;
             i < catalogEntries.Length;
             i++)
        {
            OrderCatalogEntry entry =
                catalogEntries[i];

            if (entry == null)
            {
                continue;
            }

            ProductData product =
                entry.ProductData;

            Button button =
                entry.OrderButton;

            if (product == null ||
                button == null)
            {
                continue;
            }

            button.onClick.AddListener(
                () => OrderProduct(product)
            );
        }
    }

    private void RefreshCatalogDisplay()
    {
        if (catalogEntries == null)
        {
            return;
        }

        for (int i = 0;
             i < catalogEntries.Length;
             i++)
        {
            OrderCatalogEntry entry =
                catalogEntries[i];

            if (entry == null)
            {
                continue;
            }

            entry.RefreshDisplay(
                quantityPerBox
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

        if (deliverySpawnPoint == null)
        {
            statusText.text =
                "Delivery point not configured.";

            return;
        }

        if (storeEconomy == null)
        {
            statusText.text =
                "Store economy not configured.";

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

        DeliveryBox newDelivery =
            Instantiate(
                deliveryBoxPrefab,
                deliverySpawnPoint.position,
                deliverySpawnPoint.rotation
            );

        newDelivery.Configure(
            productData,
            quantityPerBox
        );

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