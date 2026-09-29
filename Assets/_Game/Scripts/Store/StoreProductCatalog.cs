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

    public ProductData GetProduct(
        int index
    )
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

    public ProductData FindProductByID(
        string productID
    )
    {
        if (products == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(
                productID
            ))
        {
            return null;
        }

        for (int i = 0;
             i < products.Length;
             i++)
        {
            ProductData product =
                products[i];

            if (product == null)
            {
                continue;
            }

            if (product.ProductID
                != productID)
            {
                continue;
            }

            return product;
        }

        return null;
    }
}