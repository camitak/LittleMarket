using System.Collections.Generic;
using UnityEngine;

public class StoreProgression : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private StoreProductCatalog productCatalog;

    [SerializeField]
    private StoreReputation storeReputation;

    private List<ProductData> unlockedProducts =
        new List<ProductData>();

    private List<ProductData> lastNewUnlocks =
        new List<ProductData>();

    private bool isInitialized;

    public int CatalogProductCount
    {
        get
        {
            if (productCatalog == null)
            {
                return 0;
            }

            return productCatalog.ProductCount;
        }
    }

    public int UnlockedProductCount =>
        unlockedProducts.Count;

    public int LastNewUnlockCount =>
        lastNewUnlocks.Count;

    private void Start()
    {
        InitializeProgression();
    }

    public ProductData GetCatalogProduct(
        int index
    )
    {
        if (productCatalog == null)
        {
            return null;
        }

        return productCatalog.GetProduct(
            index
        );
    }
    
    public ProductData FindCatalogProductByID(
        string productID
    )
    {
        if (productCatalog == null)
        {
            return null;
        }

        return productCatalog.FindProductByID(
            productID
        );
    }

    public ProductData GetUnlockedProduct(
        int index
    )
    {
        EnsureInitialized();

        if (index < 0 ||
            index >= unlockedProducts.Count)
        {
            return null;
        }

        return unlockedProducts[index];
    }

    public ProductData GetLastNewUnlock(
        int index
    )
    {
        if (index < 0 ||
            index >= lastNewUnlocks.Count)
        {
            return null;
        }

        return lastNewUnlocks[index];
    }

    public bool IsProductUnlocked(
        ProductData productData
    )
    {
        EnsureInitialized();

        if (productData == null)
        {
            return false;
        }

        return unlockedProducts.Contains(
            productData
        );
    }

    public List<string> GetUnlockedProductIDs()
    {
        EnsureInitialized();

        List<string> ids =
            new List<string>();

        for (int i = 0;
             i < unlockedProducts.Count;
             i++)
        {
            ProductData product =
                unlockedProducts[i];

            if (product == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(
                    product.ProductID
                ))
            {
                continue;
            }

            ids.Add(
                product.ProductID
            );
        }

        return ids;
    }

    public void RestoreUnlockedProducts(
        List<string> productIDs
    )
    {
        EnsureInitialized();

        unlockedProducts.Clear();

        lastNewUnlocks.Clear();

        if (productCatalog == null)
        {
            return;
        }

        if (productIDs != null)
        {
            for (int i = 0;
                 i < productIDs.Count;
                 i++)
            {
                ProductData product =
                    productCatalog
                        .FindProductByID(
                            productIDs[i]
                        );

                if (product == null)
                {
                    continue;
                }

                if (unlockedProducts.Contains(
                        product
                    ))
                {
                    continue;
                }

                unlockedProducts.Add(
                    product
                );
            }
        }

        EnsureStarterProductsUnlocked();
    }

    public void RefreshUnlocks()
    {
        EnsureInitialized();

        lastNewUnlocks.Clear();

        if (productCatalog == null)
        {
            return;
        }

        if (storeReputation == null)
        {
            return;
        }

        for (int i = 0;
             i < productCatalog.ProductCount;
             i++)
        {
            ProductData product =
                productCatalog.GetProduct(i);

            if (product == null)
            {
                continue;
            }

            if (unlockedProducts.Contains(
                    product
                ))
            {
                continue;
            }

            if (storeReputation.CurrentReputation
                < product.RequiredReputation)
            {
                continue;
            }

            unlockedProducts.Add(
                product
            );

            lastNewUnlocks.Add(
                product
            );
        }
    }

    private void InitializeProgression()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        unlockedProducts.Clear();

        lastNewUnlocks.Clear();

        if (productCatalog == null)
        {
            return;
        }

        if (storeReputation == null)
        {
            return;
        }

        for (int i = 0;
             i < productCatalog.ProductCount;
             i++)
        {
            ProductData product =
                productCatalog.GetProduct(i);

            if (product == null)
            {
                continue;
            }

            if (storeReputation.CurrentReputation
                < product.RequiredReputation)
            {
                continue;
            }

            unlockedProducts.Add(
                product
            );
        }
    }

    private void EnsureStarterProductsUnlocked()
    {
        if (productCatalog == null)
        {
            return;
        }

        for (int i = 0;
             i < productCatalog.ProductCount;
             i++)
        {
            ProductData product =
                productCatalog.GetProduct(i);

            if (product == null)
            {
                continue;
            }

            if (product.RequiredReputation > 0f)
            {
                continue;
            }

            if (unlockedProducts.Contains(
                    product
                ))
            {
                continue;
            }

            unlockedProducts.Add(
                product
            );
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        InitializeProgression();
    }
}