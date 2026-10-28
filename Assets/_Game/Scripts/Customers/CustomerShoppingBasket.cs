using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomerShoppingBasket : MonoBehaviour
{
    [Header("Customer Attachment")]
    [SerializeField] private Transform basketCarryPoint;

    [Header("When To Use A Basket")]
    [Min(1)] [SerializeField] private int minimumItemsForBasket = 3;

    [Header("Basket Item Positions (Local Units)")]
    [Min(0.01f)] [SerializeField] private float columnSpacing = 0.30f;
    [Min(0.01f)] [SerializeField] private float rowSpacing = 0.21f;
    [Min(0.01f)] [SerializeField] private float layerSpacing = 0.16f;

    [Header("Optional Product Shrinking")]
    [Tooltip("Off for Little Market: prefer readable original-size groceries. Enable only when an art pass needs compact items.")]
    [SerializeField] private bool shrinkOversizedItems = false;
    [Min(0.01f)] [SerializeField] private float maximumItemWidth = 0.29f;
    [Min(0.01f)] [SerializeField] private float maximumItemHeight = 0.32f;
    [Min(0.01f)] [SerializeField] private float maximumItemDepth = 0.20f;

    private readonly List<Transform> itemSlots = new();
    private readonly Dictionary<PickupItem, Vector3> originalItemScales = new();
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
            Debug.LogWarning("Customer cannot collect a basket: station or BasketCarryPoint is missing.", this);
            return false;
        }

        GameObject newBasket = station.DispenseBasket(basketCarryPoint);
        if (newBasket == null)
            return false;

        Transform newSlotsRoot = newBasket.transform.Find("ItemSlotsRoot");
        if (newSlotsRoot == null)
        {
            Debug.LogError("PF_CustomerBasket must contain a direct child named ItemSlotsRoot.", newBasket);
            Destroy(newBasket);
            return false;
        }

        activeBasket = newBasket;
        itemSlotsRoot = newSlotsRoot;
        itemSlots.Clear();
        originalItemScales.Clear();
        return true;
    }

    public Transform GetCarryPoint(int itemIndex)
    {
        if (!UsesBasket || itemSlotsRoot == null || itemIndex < 0)
            return null;
        while (itemSlots.Count <= itemIndex)
        {
            int index = itemSlots.Count;
            GameObject slotObject = new($"BasketItemPoint_{index + 1:00}");
            Transform slot = slotObject.transform;
            slot.SetParent(itemSlotsRoot, false);
            int column = index % 2;
            int row = (index / 2) % 2;
            int layer = index / 4;
            slot.localPosition = new Vector3(
                (column - 0.5f) * columnSpacing,
                layer * layerSpacing,
                (row - 0.5f) * rowSpacing);
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;
            itemSlots.Add(slot);
        }
        return itemSlots[itemIndex];
    }

    // Called only after ShelfSlot successfully hands this real PickupItem to a customer.
    public void FitCollectedItem(PickupItem item)
    {
        if (!UsesBasket || item == null || itemSlotsRoot == null)
            return;
        Transform slot = item.transform.parent;
        if (slot == null || slot.parent != itemSlotsRoot)
            return;
        if (originalItemScales.ContainsKey(item))
            return;
        originalItemScales.Add(item, item.transform.localScale);
        if (!TryGetVisibleBounds(item, out Bounds bounds))
            return;

        // Default OFF: the user confirmed that removing this limit restores
        // readable groceries. Keep the scale option for later art tuning.
        if (shrinkOversizedItems)
        {
            float fit = Mathf.Min(
                1f,
                maximumItemWidth / Mathf.Max(0.001f, bounds.size.x),
                maximumItemHeight / Mathf.Max(0.001f, bounds.size.y),
                maximumItemDepth / Mathf.Max(0.001f, bounds.size.z));
            item.transform.localScale *= fit;
        }

        // Preserve the original pivot-correction improvement from Lesson 97.
        if (TryGetVisibleBounds(item, out Bounds fittedBounds))
            item.transform.position += slot.position - fittedBounds.center;
    }

    public void RestoreItemForCheckout(PickupItem item)
    {
        if (item == null)
            return;
        if (!originalItemScales.TryGetValue(item, out Vector3 originalScale))
            return;
        item.transform.localScale = originalScale;
        originalItemScales.Remove(item);
    }

    public void ClearAfterCheckout() => DiscardBasket();

    public void DiscardBasket()
    {
        if (activeBasket != null)
            Destroy(activeBasket);
        activeBasket = null;
        itemSlotsRoot = null;
        itemSlots.Clear();
        originalItemScales.Clear();
    }

    private static bool TryGetVisibleBounds(PickupItem item, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }
}
