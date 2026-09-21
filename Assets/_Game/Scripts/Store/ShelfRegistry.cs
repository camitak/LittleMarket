using UnityEngine;

public class ShelfRegistry : MonoBehaviour
{
    [SerializeField] private ShelfSlot[] shelfSlots;

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

        for (int i = 0; i < shelfSlots.Length; i++)
        {
            ShelfSlot slot = shelfSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (slot.CustomerStandPoint == null)
            {
                continue;
            }

            if (!slot.ContainsProduct(productData))
            {
                continue;
            }

            foundSlot = slot;

            return true;
        }

        return false;
    }
}