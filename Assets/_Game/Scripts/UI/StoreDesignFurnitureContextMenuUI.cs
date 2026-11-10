
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreDesignFurnitureContextMenuUI : MonoBehaviour
{
    [Header("Store Design")]
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private FurniturePlacementController placementController;
    [SerializeField] private FurnitureShop furnitureShop;
    [SerializeField] private BuildCatalogUI buildCatalogUI;
    [SerializeField] private StoreExpansionLayoutController expansionLayoutController;

    [Header("Context Menu")]
    [SerializeField] private RectTransform layoutFrame;
    [SerializeField] private GameObject dismissLayer;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private TMP_Text furnitureNameText;
    [SerializeField] private TMP_Text ownershipText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button copyButton;
    [SerializeField] private TMP_Text copyButtonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [SerializeField] private Button closeButton;

    [Header("Sale Confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmSaleButton;
    [SerializeField] private Button cancelSaleButton;

    private FurnitureIdentity chosenFurniture;
    private bool confirmingSale;
    private float nextRefreshTime;

    public bool IsOpen => chosenFurniture != null && dismissLayer != null && dismissLayer.activeSelf;

    private void Awake()
    {
        if (copyButton != null) copyButton.onClick.AddListener(CopyFurniture);
        if (sellButton != null) sellButton.onClick.AddListener(RequestSale);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);
        if (confirmSaleButton != null) confirmSaleButton.onClick.AddListener(ConfirmSale);
        if (cancelSaleButton != null) cancelSaleButton.onClick.AddListener(CancelSale);

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (dismissLayer != null) dismissLayer.SetActive(false);
    }

    private void Update()
    {
        if (!CanUseContextMenu())
        {
            CloseMenu();
            return;
        }

        if (dismissLayer != null && dismissLayer.activeSelf && chosenFurniture == null)
        {
            CloseMenu();
            return;
        }

        if (IsOpen)
        {
            if (!chosenFurniture.gameObject.activeInHierarchy || chosenFurniture.FurnitureDefinition == null)
            {
                CloseMenu();
                return;
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleBack();
                return;
            }

            // The invisible backdrop consumes mouse-down. Dismiss on
            // release so the same click cannot move furniture beneath it.
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame &&
                !RectTransformUtility.RectangleContainsScreenPoint(
                    menuPanel, Mouse.current.position.ReadValue(), null))
            {
                HandleBack();
                return;
            }

            if (!confirmingSale && Time.unscaledTime >= nextRefreshTime)
            {
                RefreshMenu();
                nextRefreshTime = Time.unscaledTime + 0.25f;
            }

            return;
        }

        if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame ||
            layoutMode.IsCameraManipulating || IsPointerOverUI())
        {
            return;
        }

        if (placementController.TryFindPointedAtFurniture(out FurnitureIdentity furniture))
        {
            OpenFor(furniture);
        }
    }

    private bool CanUseContextMenu()
    {
        return layoutMode != null && layoutMode.IsActive && Time.timeScale > 0f &&
               Cursor.lockState == CursorLockMode.None && placementController != null &&
               !placementController.IsPlacementActive && !placementController.CancelledPlacementThisFrame &&
               furnitureShop != null && (buildCatalogUI == null || !buildCatalogUI.IsOpen) &&
               (expansionLayoutController == null || !expansionLayoutController.HasSelection);
    }

    private void OpenFor(FurnitureIdentity furniture)
    {
        if (furniture == null || furniture.FurnitureDefinition == null ||
            dismissLayer == null || layoutFrame == null || menuPanel == null)
        {
            return;
        }

        chosenFurniture = furniture;
        confirmingSale = false;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        dismissLayer.SetActive(true);
        dismissLayer.transform.SetAsLastSibling();

        PositionMenuNearCursor();
        RefreshMenu();

        nextRefreshTime = Time.unscaledTime + 0.25f;
    }

    private void PositionMenuNearCursor()
    {
        Canvas.ForceUpdateCanvases();

        if (Mouse.current == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layoutFrame, Mouse.current.position.ReadValue(), null, out Vector2 point))
        {
            return;
        }

        Rect parent = layoutFrame.rect;
        float width = menuPanel.rect.width;
        float height = menuPanel.rect.height;
        const float margin = 12f;

        float x = Mathf.Clamp(point.x + 16f, parent.xMin + margin, parent.xMax - width - margin);
        float y = Mathf.Clamp(point.y - 12f, parent.yMin + height + margin, parent.yMax - margin);

        menuPanel.anchoredPosition = new Vector2(x, y);
    }

    private void RefreshMenu()
    {
        if (chosenFurniture == null || chosenFurniture.FurnitureDefinition == null)
        {
            return;
        }

        FurnitureDefinition definition = chosenFurniture.FurnitureDefinition;

        bool canCopy = furnitureShop.CanBeginPurchase(definition, out string copyReason);
        bool canSell = furnitureShop.CanSellFurniture(chosenFurniture, out float refund, out string sellReason);

        if (furnitureNameText != null)
        {
            furnitureNameText.text = definition.DisplayName;
        }

        if (ownershipText != null)
        {
            ownershipText.text = definition.MaxOwned > 0
                ? $"Owned {furnitureShop.GetOwnedCount(definition)} / {definition.MaxOwned}"
                : "Choose an action";
        }

        if (copyButton != null) copyButton.interactable = canCopy;
        if (sellButton != null) sellButton.interactable = canSell;

        if (copyButtonText != null)
        {
            copyButtonText.text = $"COPY  ·  £{definition.PurchasePrice:0.00}";
        }

        if (sellButtonText != null)
        {
            sellButtonText.text = $"SELL  ·  +£{furnitureShop.GetSellRefund(definition):0.00}";
        }

        if (statusText != null)
        {
            string explanation = "";

            if (!canCopy)
            {
                explanation = $"Copy: {copyReason}";
            }

            if (!canSell)
            {
                if (explanation.Length > 0) explanation += "\n";
                explanation += $"Sell: {sellReason}";
            }

            statusText.text = explanation.Length > 0
                ? explanation
                : "Click outside or press Esc to close.";
        }
    }

    private void CopyFurniture()
    {
        if (chosenFurniture == null) return;

        FurnitureDefinition definition = chosenFurniture.FurnitureDefinition;

        if (!furnitureShop.CanBeginPurchase(definition, out string reason))
        {
            RefreshMenu();
            placementController.ShowTransientStatus(reason);
            return;
        }

        float yaw = chosenFurniture.transform.eulerAngles.y;

        CloseMenu();
        StartCoroutine(BeginCopyAfterUICloses(definition, yaw));
    }

    private IEnumerator BeginCopyAfterUICloses(FurnitureDefinition definition, float yaw)
    {
        // Allow EventSystem to refresh its pointer raycast after hiding the panel.
        yield return null;

        if (layoutMode == null || !layoutMode.IsActive || furnitureShop == null)
        {
            yield break;
        }

        if (!furnitureShop.TryBeginPurchase(definition, out string reason, yaw, false))
        {
            placementController.ShowTransientStatus(reason);
        }
    }

    private void RequestSale()
    {
        if (chosenFurniture == null || confirmingSale) return;

        if (!furnitureShop.CanSellFurniture(chosenFurniture, out float refund, out string reason))
        {
            RefreshMenu();
            placementController.ShowTransientStatus(reason);
            return;
        }

        confirmingSale = true;

        if (confirmationText != null)
        {
            confirmationText.text =
                $"Sell {chosenFurniture.FurnitureDefinition.DisplayName} for £{refund:0.00}?\n\n" +
                "This removes it from your store. There is no Undo yet.";
        }

        if (confirmationPanel != null) confirmationPanel.SetActive(true);
    }

    private void ConfirmSale()
    {
        if (!confirmingSale || chosenFurniture == null) return;

        string itemName = chosenFurniture.FurnitureDefinition.DisplayName;

        if (!furnitureShop.TrySellFurniture(chosenFurniture, out float refund, out string reason))
        {
            CancelSale();
            RefreshMenu();
            placementController.ShowTransientStatus(reason);
            return;
        }

        CloseMenu();
        layoutMode.MarkNavigationDirty();
        placementController.ShowTransientStatus($"{itemName} sold for £{refund:0.00}.");
    }

    private void CancelSale()
    {
        confirmingSale = false;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (IsOpen) RefreshMenu();
    }

    public bool HandleBack()
    {
        if (!IsOpen) return false;

        if (confirmingSale) CancelSale();
        else CloseMenu();

        return true;
    }

    public void CloseMenu()
    {
        chosenFurniture = null;
        confirmingSale = false;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (dismissLayer != null) dismissLayer.SetActive(false);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && Mouse.current != null &&
               EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
    }

    private void OnDestroy()
    {
        if (copyButton != null) copyButton.onClick.RemoveListener(CopyFurniture);
        if (sellButton != null) sellButton.onClick.RemoveListener(RequestSale);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMenu);
        if (confirmSaleButton != null) confirmSaleButton.onClick.RemoveListener(ConfirmSale);
        if (cancelSaleButton != null) cancelSaleButton.onClick.RemoveListener(CancelSale);
    }
}
