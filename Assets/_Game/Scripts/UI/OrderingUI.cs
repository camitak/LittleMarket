using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject orderingPanel;

    [SerializeField] private TMP_Text productNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button orderButton;
    [SerializeField] private Button closeButton;

    [Header("Order")]
    [SerializeField] private ProductData productData;

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
        orderButton.onClick.AddListener(OrderProduct);
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

        UpdateProductDisplay();

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

    private void OrderProduct()
    {
        if (productData == null)
        {
            statusText.text = "Product not configured.";
            return;
        }

        if (deliveryBoxPrefab == null)
        {
            statusText.text = "Delivery box not configured.";
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

        DeliveryBox newDelivery = Instantiate(
            deliveryBoxPrefab,
            deliverySpawnPoint.position,
            deliverySpawnPoint.rotation
        );

        newDelivery.Configure(
            productData,
            quantityPerBox
        );

        statusText.text =
            "Order placed! Delivery has arrived.";
    }

    private void UpdateProductDisplay()
    {
        if (productData == null)
        {
            productNameText.text = "No product";
            priceText.text = "";
            return;
        }

        float orderCost =
            productData.BuyPrice * quantityPerBox;

        productNameText.text =
            productData.ProductName;

        priceText.text =
            "Box of "
            + quantityPerBox
            + " - £"
            + orderCost.ToString("0.00");
    }
}