
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasketContentsHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject basketPanel;

    [SerializeField]
    private RectTransform basketPanelRect;

    [SerializeField]
    private TMP_Text capacityText;

    [SerializeField]
    private TMP_Text contentsText;

    [Header("References")]
    [SerializeField]
    private PlayerInteraction playerInteraction;

    [SerializeField]
    private StoreProgression storeProgression;

    [Header("Layout")]
    [SerializeField]
    private float collapsedHeight = 42f;

    [SerializeField]
    private float expandedHeight = 156f;

    [Header("Refresh")]
    [Min(0.05f)]
    [SerializeField]
    private float refreshInterval = 0.15f;

    private static readonly Color32 NormalColor =
        new Color32(89, 70, 64, 255);

    private static readonly Color32 FullColor =
        new Color32(176, 133, 41, 255);

    private readonly StringBuilder textBuilder =
        new StringBuilder();

    private RestockBasket previousBasket;
    private bool previousExpanded;
    private float refreshTimer;

    private void Awake()
    {
        if (basketPanel != null)
        {
            basketPanel.SetActive(false);
        }
    }

    private void Update()
    {
        RestockBasket basket = null;

        if (playerInteraction != null &&
            playerInteraction.isActiveAndEnabled &&
            Time.timeScale > 0f)
        {
            basket =
                playerInteraction.GetHeldRestockBasket();
        }

        if (basket == null)
        {
            HidePanel();
            return;
        }

        if (basketPanel == null ||
            basketPanelRect == null ||
            capacityText == null ||
            contentsText == null ||
            storeProgression == null)
        {
            return;
        }

        bool wasVisible = basketPanel.activeSelf;

        if (!wasVisible)
        {
            basketPanel.SetActive(true);
        }

        bool expanded =
            Keyboard.current != null &&
            Keyboard.current.tabKey.isPressed;

        if (expanded != previousExpanded ||
            !wasVisible)
        {
            UpdatePanelLayout(expanded);
        }

        refreshTimer += Time.unscaledDeltaTime;

        bool needsRefresh =
            !wasVisible ||
            basket != previousBasket ||
            expanded != previousExpanded ||
            refreshTimer >= refreshInterval;

        if (needsRefresh)
        {
            RefreshText(basket, expanded);
            refreshTimer = 0f;
        }

        previousBasket = basket;
        previousExpanded = expanded;
    }

    private void UpdatePanelLayout(bool expanded)
    {
        basketPanelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            expanded ? expandedHeight : collapsedHeight
        );

        contentsText.gameObject.SetActive(expanded);
    }

    private void RefreshText(
        RestockBasket basket,
        bool expanded)
    {
        capacityText.text =
            "BASKET  " +
            basket.ItemCount +
            " / " +
            basket.Capacity +
            (expanded ? "" : "   [TAB]");

        capacityText.color =
            basket.IsFull ? FullColor : NormalColor;

        if (!expanded)
        {
            return;
        }

        textBuilder.Clear();

        if (basket.IsEmpty)
        {
            textBuilder.Append("Basket is empty.");
        }
        else
        {
            for (int i = 0;
                 i < storeProgression.CatalogProductCount;
                 i++)
            {
                ProductData product =
                    storeProgression.GetCatalogProduct(i);

                if (product == null)
                {
                    continue;
                }

                int count =
                    basket.GetProductCount(product);

                if (count <= 0)
                {
                    continue;
                }

                if (textBuilder.Length > 0)
                {
                    textBuilder.AppendLine();
                }

                textBuilder.Append(product.ProductName);
                textBuilder.Append("  x");
                textBuilder.Append(count);
            }

            if (textBuilder.Length == 0)
            {
                textBuilder.Append(
                    "Contents not found in catalogue."
                );
            }
        }

        contentsText.text = textBuilder.ToString();
    }

    private void HidePanel()
    {
        if (basketPanel != null &&
            basketPanel.activeSelf)
        {
            basketPanel.SetActive(false);
        }

        previousBasket = null;
        previousExpanded = false;
        refreshTimer = 0f;
    }
}
