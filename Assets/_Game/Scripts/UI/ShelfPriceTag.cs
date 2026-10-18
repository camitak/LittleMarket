using TMPro;
using UnityEngine;

public class ShelfPriceTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private TMP_Text productNameText;
    [SerializeField] private TMP_Text priceText;

    private void Awake()
    {
        Refresh(null);
    }

    public void Refresh(ProductData product)
    {
        bool hasProduct = product != null;

        if (visualRoot != null)
        {
            visualRoot.SetActive(hasProduct);
        }

        if (!hasProduct)
        {
            return;
        }

        if (productNameText != null)
        {
            productNameText.text = product.ProductName;
        }

        if (priceText != null)
        {
            priceText.text = $"£{product.SellPrice:0.00}";
        }
    }
}