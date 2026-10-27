using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomerShoppingBasket : MonoBehaviour
{
    [Header("Basket Hierarchy")]
    [SerializeField] private GameObject basketRoot;
    [SerializeField] private Transform itemSlotsRoot;

    [Header("When To Use A Basket")]
    [Min(1)] [SerializeField] private int minimumItemsForBasket = 3;

    [Header("Product Layout (Local Units)")]
    [Min(0.01f)] [SerializeField] private float columnSpacing = 0.26f;
    [Min(0.01f)] [SerializeField] private float rowSpacing = 0.17f;
    [Min(0.01f)] [SerializeField] private float layerSpacing = 0.15f;

    private readonly List<Transform> itemSlots = new();
    private bool usesBasket;

    public bool UsesBasket => usesBasket;

    public void ConfigureForShoppingList(int itemCount)
    {
        usesBasket = itemCount >= minimumItemsForBasket;

        if (basketRoot == null || itemSlotsRoot == null)
        {
            Debug.LogError(
                "CustomerShoppingBasket needs Basket Root and Item Slots Root assigned.",
                this
            );
            usesBasket = false;
            return;
        }

        basketRoot.SetActive(usesBasket);
    }

    public Transform GetCarryPoint(int itemIndex)
    {
        if (!usesBasket || itemSlotsRoot == null || itemIndex < 0)
            return null;

        // Create a position only when a customer actually takes an item.
        // We don't need a fixed array of three carry slots anymore.
        while (itemSlots.Count <= itemIndex)
        {
            int index = itemSlots.Count;
            GameObject slotObject = new($"BasketItemPoint_{index + 1:00}");
            Transform slot = slotObject.transform;
            slot.SetParent(itemSlotsRoot, false);

            // Two columns, three rows; additional items form new layers.
            int column = index % 2;
            int row = (index / 2) % 3;
            int layer = index / 6;

            slot.localPosition = new Vector3(
                (column - 0.5f) * columnSpacing,
                layer * layerSpacing,
                (row - 1f) * rowSpacing
            );
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;

            itemSlots.Add(slot);
        }

        return itemSlots[itemIndex];
    }

    public void HideAfterCheckout()
    {
        if (basketRoot != null)
            basketRoot.SetActive(false);
    }
}
