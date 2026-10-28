using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to active GameUI (NOT OrderingPanel, which is disabled on Close).
// Shares the existing OrderingUI and its input/cursor lifecycle.
[DisallowMultipleComponent]
public class StoreComputerFurnitureUI : MonoBehaviour
{
    [Header("Existing Terminal")]
    [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private GameObject productsScrollView;
    [SerializeField] private TMP_Text orderStatusText;

    [Header("New Furniture Section")]
    [SerializeField] private GameObject furniturePanel;
    [SerializeField] private Button productsTabButton;
    [SerializeField] private Button furnitureTabButton;
    [SerializeField] private Button buyShelfButton;
    [SerializeField] private TMP_Text shelfPriceText;

    [Header("Business Rules")]
    [SerializeField] private FurnitureShop furnitureShop;

    private bool lastOpen;
    private bool startingPlacement;

    private void Awake()
    {
        if (productsTabButton != null) productsTabButton.onClick.AddListener(ShowProducts);
        if (furnitureTabButton != null) furnitureTabButton.onClick.AddListener(ShowFurniture);
        if (buyShelfButton != null) buyShelfButton.onClick.AddListener(BuyShelf);
        if (furniturePanel != null) furniturePanel.SetActive(false);
    }

    private void Update()
    {
        bool open = orderingUI != null && orderingUI.IsOpen;
        if (open && !lastOpen) ShowProducts();
        lastOpen = open;
    }

    private void ShowProducts()
    {
        if (productsScrollView != null) productsScrollView.SetActive(true);
        if (furniturePanel != null) furniturePanel.SetActive(false);
    }

    private void ShowFurniture()
    {
        if (productsScrollView != null) productsScrollView.SetActive(false);
        if (furniturePanel != null) furniturePanel.SetActive(true);
        if (shelfPriceText != null && furnitureShop != null)
            shelfPriceText.text = $"Regular shelf — £{furnitureShop.ShelfPrice:0.00}";
        if (orderStatusText != null)
            orderStatusText.text = "Furniture must be placed while the store is closed.";
    }

    private void BuyShelf()
    {
        if (startingPlacement || orderingUI == null || !orderingUI.IsOpen ||
            furnitureShop == null)
            return;

        if (!furnitureShop.CanBeginShelfPurchase(out string reason))
        {
            if (orderStatusText != null) orderStatusText.text = reason;
            return;
        }

        startingPlacement = true;
        orderingUI.Close();
        StartCoroutine(BeginWhenCursorReturns());
    }

    private IEnumerator BeginWhenCursorReturns()
    {
        // OrderingUI re-locks the cursor after a frame to avoid button-release issues.
        // This UI component lives on GameUI, so the coroutine survives panel Close().
        for (int i = 0; i < 5 && Cursor.lockState != CursorLockMode.Locked; i++)
            yield return null;

        if (!furnitureShop.TryBeginShelfPurchase(out string reason))
            Debug.LogWarning("Shelf purchase not started: " + reason, this);

        startingPlacement = false;
    }
}
