
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomerShoppingBasket : MonoBehaviour
{
    [Header("Customer Attachment")]
    [SerializeField] private Transform basketCarryPoint;

    [Header("When To Use A Basket")]
    [Min(1)]
    [SerializeField] private int minimumItemsForBasket = 3;

    [Header("Product Layout (Local Units)")]
    [Min(0.01f)]
    [SerializeField] private float columnSpacing = 0.26f;

    [Min(0.01f)]
    [SerializeField] private float rowSpacing = 0.17f;

    [Min(0.01f)]
    [SerializeField] private float layerSpacing = 0.15f;

    private readonly List<Transform> itemSlots = new();

    private bool wantsBasket;
    private GameObject activeBasket;
    private Transform itemSlotsRoot;

    public bool NeedsBasket => wantsBasket;
    public bool UsesBasket => activeBasket != null;

    public void ConfigureForShoppingList(int itemCount)
    {
        DiscardBasket();
        wantsBasket = itemCount >= minimumItemsForBasket;
    }

    public bool TakeBasket(CustomerBasketStation station)
    {
        if (!wantsBasket)
            return false;

        if (activeBasket != null)
            return true;

        if (station == null || basketCarryPoint == null)
        {
            Debug.LogWarning(
                "Customer cannot collect a basket: station or BasketCarryPoint is missing.",
                this
            );
            return false;
        }

        GameObject newBasket =
            station.DispenseBasket(basketCarryPoint);

        if (newBasket == null)
            return false;

        Transform newSlotsRoot =
            newBasket.transform.Find("ItemSlotsRoot");

        if (newSlotsRoot == null)
        {
            Debug.LogError(
                "PF_CustomerBasket must contain a direct child named ItemSlotsRoot.",
                newBasket
            );

            Destroy(newBasket);
            return false;
        }

        activeBasket = newBasket;
        itemSlotsRoot = newSlotsRoot;
        itemSlots.Clear();

        return true;
    }

    public Transform GetCarryPoint(int itemIndex)
    {
        if (!UsesBasket || itemSlotsRoot == null || itemIndex < 0)
            return null;

        while (itemSlots.Count <= itemIndex)
        {
            int index = itemSlots.Count;

            GameObject slotObject =
                new($"BasketItemPoint_{index + 1:00}");

            Transform slot = slotObject.transform;
            slot.SetParent(itemSlotsRoot, false);

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

    public void ClearAfterCheckout()
    {
        DiscardBasket();
    }

    public void DiscardBasket()
    {
        if (activeBasket != null)
            Destroy(activeBasket);

        activeBasket = null;
        itemSlotsRoot = null;
        itemSlots.Clear();
    }
}
