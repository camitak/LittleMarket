using System.Collections.Generic;
using UnityEngine;

public class WorldItemRegistry : MonoBehaviour
{
    private List<PickupItem> registeredItems =
        new List<PickupItem>();

    public int RegisteredItemCount =>
        registeredItems.Count;

    public void RegisterItem(
        PickupItem item
    )
    {
        if (item == null)
        {
            return;
        }

        if (registeredItems.Contains(
                item
            ))
        {
            return;
        }

        registeredItems.Add(
            item
        );
    }

    public void UnregisterItem(
        PickupItem item
    )
    {
        if (item == null)
        {
            return;
        }

        registeredItems.Remove(
            item
        );
    }

    public PickupItem GetRegisteredItem(
        int index
    )
    {
        if (index < 0 ||
            index >= registeredItems.Count)
        {
            return null;
        }

        return registeredItems[index];
    }

    public void ClearLooseWorldItemsForLoad()
    {
        for (int i =
                 registeredItems.Count - 1;
             i >= 0;
             i--)
        {
            PickupItem item =
                registeredItems[i];

            if (item == null)
            {
                registeredItems.RemoveAt(
                    i
                );

                continue;
            }

            if (!item.IsLooseWorldItem)
            {
                continue;
            }

            registeredItems.RemoveAt(
                i
            );

            item.gameObject.SetActive(
                false
            );

            Destroy(
                item.gameObject
            );
        }
    }

    public bool RestoreLooseProduct(
        ProductData productData,
        Vector3 position,
        Quaternion rotation
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (productData.WorldPrefab == null)
        {
            return false;
        }

        PickupItem restoredItem =
            Instantiate(
                productData.WorldPrefab,
                position,
                rotation
            );

        restoredItem.RestoreAsLooseWorldItem(
            position,
            rotation
        );

        return true;
    }
}