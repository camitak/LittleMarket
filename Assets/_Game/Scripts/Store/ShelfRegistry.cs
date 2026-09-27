using System.Collections.Generic;
using UnityEngine;

public class ShelfRegistry : MonoBehaviour
{
    private List<ShelfSlot> registeredSlots =
        new List<ShelfSlot>();

    public void RegisterSlot(
        ShelfSlot shelfSlot
    )
    {
        if (shelfSlot == null)
        {
            return;
        }

        if (registeredSlots.Contains(
                shelfSlot
            ))
        {
            return;
        }

        registeredSlots.Add(
            shelfSlot
        );
    }

    public void UnregisterSlot(
        ShelfSlot shelfSlot
    )
    {
        if (shelfSlot == null)
        {
            return;
        }

        registeredSlots.Remove(
            shelfSlot
        );
    }

    public bool TryFindStockedSlot(
        ProductData productData,
        out ShelfSlot foundSlot
    )
    {
        foundSlot = null;

        if (productData == null)
        {
            return false;
        }

        for (int i = 0;
             i < registeredSlots.Count;
             i++)
        {
            ShelfSlot slot =
                registeredSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (!slot.isActiveAndEnabled)
            {
                continue;
            }

            if (slot.CustomerStandPoint == null)
            {
                continue;
            }

            if (!slot.ContainsProduct(
                    productData
                ))
            {
                continue;
            }

            foundSlot = slot;

            return true;
        }

        return false;
    }
}