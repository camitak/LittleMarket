using System.Collections.Generic;
using UnityEngine;

public class StorageRegistry : MonoBehaviour
{
    [SerializeField]
    private List<ShelfSlot> registeredSlots = new List<ShelfSlot>();

    public int RegisteredSlotCount => registeredSlots.Count;

    public void Register(
        ShelfSlot slot
    )
    {
        if (slot == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                slot.SlotID
            ))
        {
            Debug.LogError(
                "Storage ShelfSlot '"
                + slot.gameObject.name
                + "' has no Slot ID.",
                slot
            );

            return;
        }

        for (int i = 0;
             i < registeredSlots.Count;
             i++)
        {
            ShelfSlot existingSlot =
                registeredSlots[i];

            if (existingSlot == null)
            {
                continue;
            }

            if (existingSlot == slot)
            {
                return;
            }

            if (existingSlot.SlotID
                != slot.SlotID)
            {
                continue;
            }

            Debug.LogError(
                "Duplicate storage Slot ID '"
                + slot.SlotID
                + "'. Storage Slot IDs must be unique.",
                slot
            );

            return;
        }

        registeredSlots.Add(
            slot
        );
    }

    public void Unregister(
        ShelfSlot slot
    )
    {
        if (slot == null)
        {
            return;
        }

        registeredSlots.Remove(
            slot
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

            if (slot.SlotID == slotID)
            {
                return slot;
            }
        }

        return null;
    }
    
    public int OccupiedSlotCount
    {
        get
        {
            int count = 0;

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

                if (slot.GetStoredProductData()
                    == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }

    public int EmptySlotCount =>
        Mathf.Max(
            0,
            RegisteredSlotCount
            - OccupiedSlotCount
        );
    
    public bool HasSpace =>
        EmptySlotCount > 0;

    public bool IsFull =>
        RegisteredSlotCount > 0
        && EmptySlotCount <= 0;

    public float OccupancyRatio
    {
        get
        {
            if (RegisteredSlotCount <= 0)
            {
                return 0f;
            }

            return (float)OccupiedSlotCount
                   / RegisteredSlotCount;
        }
    }
}