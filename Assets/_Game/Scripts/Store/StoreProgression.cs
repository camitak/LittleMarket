using System.Collections.Generic;
using UnityEngine;

public class StoreProgression : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField]
    private StoreProductCatalog productCatalog;

    [Header("Progression")]
    [SerializeField]
    private StoreReputation storeReputation;

    [SerializeField]
    private StoreLevelProgression
        storeLevelProgression;

    private List<ProductData> unlockedProducts =
        new List<ProductData>();

    private List<ProductData> lastNewUnlocks =
        new List<ProductData>();

    private bool initialized;

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

    public float CurrentReputation
    {
        get
        {
            if (storeReputation == null)
            {
                return 0f;
            }

            return storeReputation
                .CurrentReputation;
        }
    }

    public int CurrentStoreLevel
    {
        get
        {
            if (storeLevelProgression == null)
            {
                return 1;
            }

            return storeLevelProgression
                .CurrentLevel;
        }
    }

    private void Start()
    {
        EnsureInitialized();
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

    public ProductData GetUnlockedProduct(
        int index
    )
    {
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

    public bool IsProductUnlocked(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        return unlockedProducts.Contains(
            productData
        );
    }

    public bool WasUnlockedInLastRefresh(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        return lastNewUnlocks.Contains(
            productData
        );
    }

    public bool MeetsReputationRequirement(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (storeReputation == null)
        {
            return false;
        }

        return storeReputation.CurrentReputation
               >= productData.RequiredReputation;
    }

    public bool MeetsStoreLevelRequirement(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (storeLevelProgression == null)
        {
            return false;
        }

        return storeLevelProgression.CurrentLevel
               >= productData.RequiredStoreLevel;
    }

    public bool MeetsUnlockRequirements(
        ProductData productData
    )
    {
        if (!MeetsReputationRequirement(
                productData
            ))
        {
            return false;
        }

        if (!MeetsStoreLevelRequirement(
                productData
            ))
        {
            return false;
        }

        return true;
    }

    public void RefreshUnlocks()
    {
        EnsureInitializedWithoutRefresh();

        lastNewUnlocks.Clear();

        if (productCatalog == null)
        {
            return;
        }

        for (int i = 0;
             i < productCatalog.ProductCount;
             i++)
        {
            ProductData product =
                productCatalog.GetProduct(
                    i
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

            if (!MeetsUnlockRequirements(
                    product
                ))
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

    public List<string> GetUnlockedProductIDs()
    {
        List<string> productIDs =
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

            if (string.IsNullOrWhiteSpace(
                    product.ProductID
                ))
            {
                continue;
            }

            productIDs.Add(
                product.ProductID
            );
        }

        return productIDs;
    }

    public void RestoreUnlockedProducts(
        List<string> productIDs
    )
    {
        EnsureInitializedWithoutRefresh();

        unlockedProducts.Clear();

        lastNewUnlocks.Clear();

        if (productIDs != null)
        {
            for (int i = 0;
                 i < productIDs.Count;
                 i++)
            {
                ProductData product =
                    FindCatalogProductByID(
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

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        EnsureInitializedWithoutRefresh();

        /*
         * Starter products exist from the beginning.
         *
         * We add them BEFORE RefreshUnlocks so they
         * are not incorrectly reported as "NEW"
         * products on a brand-new game.
         */
        EnsureStarterProductsUnlocked();

        RefreshUnlocks();
    }

    private void EnsureInitializedWithoutRefresh()
    {
        if (initialized)
        {
            return;
        }

        initialized =
            true;

        unlockedProducts.Clear();

        lastNewUnlocks.Clear();
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
                productCatalog.GetProduct(
                    i
                );

            if (product == null)
            {
                continue;
            }

            bool isStarterProduct =
                product.RequiredReputation <= 0f
                &&
                product.RequiredStoreLevel <= 1;

            if (!isStarterProduct)
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
}