using UnityEngine;

[CreateAssetMenu(
    fileName = "NewStoreProductCatalog",
    menuName = "Little Market/Store Product Catalog"
)]
public class StoreProductCatalog : ScriptableObject
{
    [SerializeField]
    private ProductData[] products;

    public int ProductCount
    {
        get
        {
            if (products == null)
            {
                return 0;
            }

            return products.Length;
        }
    }

    public ProductData GetProduct(int index)
    {
        if (products == null)
        {
            return null;
        }

        if (index < 0 ||
            index >= products.Length)
        {
            return null;
        }

        return products[index];
    }
}