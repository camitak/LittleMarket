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
    private bool wantsBasket;
    private bool hasBasket;

    public bool NeedsBasket => wantsBasket;
    public bool UsesBasket => hasBasket;

    public void ConfigureForShoppingList(int itemCount)
    {
        wantsBasket = itemCount >= minimumItemsForBasket;
        hasBasket = false;
        itemSlots.Clear();

        if (basketRoot == null || itemSlotsRoot == null)
        {
            Debug.LogError(
                "CustomerShoppingBasket needs Basket Root and Item Slots Root assigned.",
                this);
            wantsBasket = false;
            return;
        }

        // The customer must first visit the entrance basket rack.
        basketRoot.SetActive(false);
    }

    public void TakeBasket()
    {
        if (!wantsBasket || hasBasket || basketRoot == null)
            return;

        hasBasket = true;
        basketRoot.SetActive(true);
    }

    public Transform GetCarryPoint(int itemIndex)
    {
        if (!hasBasket || itemSlotsRoot == null || itemIndex < 0)
            return null;

        while (itemSlots.Count <= itemIndex)
        {
            int index = itemSlots.Count;
            GameObject slotObject = new($"BasketItemPoint_{index + 1:00}");
            Transform slot = slotObject.transform;
            slot.SetParent(itemSlotsRoot, false);

            int column = index % 2;
            int row = (index / 2) % 3;
            int layer = index / 6;

            slot.localPosition = new Vector3(
                (column - 0.5f) * columnSpacing,
                layer * layerSpacing,
                (row - 1f) * rowSpacing);
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;
            itemSlots.Add(slot);
        }

        return itemSlots[itemIndex];
    }

    public void ReturnAfterCheckout(CustomerBasketStation station)
    {
        if (!hasBasket || basketRoot == null)
            return;

        hasBasket = false;
        GameObject returnedBasket = basketRoot;

        // Ownership transfers away from the customer: the basket can remain
        // briefly at checkout after the customer starts walking to the exit.
        basketRoot = null;
        itemSlotsRoot = null;
        itemSlots.Clear();

        if (station != null)
            station.LeaveBasketAtCheckout(returnedBasket);
        else
            Destroy(returnedBasket);
    }

    public void HideWithoutCheckout()
    {
        if (!hasBasket)
            return;

        hasBasket = false;

        // An empty-handed customer can leave without a checkout transaction.
        // In that exceptional case, the basket goes away with the customer.
        if (basketRoot != null)
            basketRoot.SetActive(false);
    }
}
