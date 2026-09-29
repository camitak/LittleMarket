using System.Collections.Generic;
using UnityEngine;

public class ShelfRegistry : MonoBehaviour
{
    private List<ShelfSlot> registeredSlots =
        new List<ShelfSlot>();

    public int RegisteredSlotCount =>
        registeredSlots.Count;

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

        if (string.IsNullOrWhiteSpace(
                shelfSlot.SlotID
            ))
        {
            Debug.LogError(
                "ShelfSlot '"
                + shelfSlot.gameObject.name
                + "' has no Slot ID.",
                shelfSlot
            );

            return;
        }

        ShelfSlot existingSlot =
            FindSlotByID(
                shelfSlot.SlotID
            );

        if (existingSlot != null &&
            existingSlot != shelfSlot)
        {
            Debug.LogError(
                "Duplicate ShelfSlot ID '"
                + shelfSlot.SlotID
                + "'. Every shelf slot must "
                + "have a unique stable ID.",
                shelfSlot
            );

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

    public ShelfSlot GetRegisteredSlot(
        int index
    )
    {
        if (index < 0 ||
            index >= registeredSlots.Count)
        {
            return null;
        }

        return registeredSlots[index];
    }

    public ShelfSlot FindSlotByID(
        string slotID
    )
    {
        if (string.IsNullOrWhiteSpace(
                slotID
            ))
        {
            return null;
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

            if (slot.SlotID != slotID)
            {
                continue;
            }

            return slot;
        }

        return null;
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

            foundSlot =
                slot;

            return true;
        }

        return false;
    }
}