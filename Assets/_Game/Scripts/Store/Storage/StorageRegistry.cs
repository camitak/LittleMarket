using System.Collections.Generic;
using UnityEngine;

public class StorageRegistry : MonoBehaviour
{
    private readonly List<StorageBay>
        registeredSlots = new();

    public int RegisteredSlotCount =>
        registeredSlots.Count;

    public int AssignedBayCount
    {
        get
        {
            var count =
                0;

            for (var i = 0;
                 i < registeredSlots.Count;
                 i++)
            {
                var bay =
                    registeredSlots[i];

                if (bay == null ||
                    !bay.IsAssigned)
                    continue;

                count++;
            }

            return count;
        }
    }

    public int UnassignedBayCount =>
        Mathf.Max(
            0,
            RegisteredSlotCount
            - AssignedBayCount
        );

    /*
     * Compatibility with the existing
     * StorageCapacityUI terminology.
     */
    public int OccupiedSlotCount =>
        AssignedBayCount;

    public int EmptySlotCount =>
        UnassignedBayCount;

    public bool HasSpace =>
        UnassignedBayCount > 0;

    public bool IsFull =>
        RegisteredSlotCount > 0
        && UnassignedBayCount <= 0;

    public float OccupancyRatio
    {
        get
        {
            if (RegisteredSlotCount <= 0) return 0f;

            return (float)AssignedBayCount
                   / RegisteredSlotCount;
        }
    }

    public void Register(
        StorageBay slot
    )
    {
        if (slot == null) return;

        if (string.IsNullOrWhiteSpace(
                slot.SlotID
            ))
        {
            Debug.LogError(
                "StorageBay '"
                + slot.gameObject.name
                + "' has no Slot ID.",
                slot
            );

            return;
        }

        for (var i = 0;
             i < registeredSlots.Count;
             i++)
        {
            var existingSlot =
                registeredSlots[i];

            if (existingSlot == null) continue;

            if (existingSlot == slot) return;

            if (existingSlot.SlotID
                != slot.SlotID)
                continue;

            Debug.LogError(
                "Duplicate storage Slot ID '"
                + slot.SlotID
                + "'. Storage IDs must be unique.",
                slot
            );

            return;
        }

        registeredSlots.Add(
            slot
        );
    }

    public void Unregister(
        StorageBay slot
    )
    {
        if (slot == null) return;

        registeredSlots.Remove(
            slot
        );
    }

    public StorageBay GetRegisteredSlot(
        int index
    )
    {
        if (index < 0 ||
            index >= registeredSlots.Count)
            return null;

        return registeredSlots[index];
    }

    public StorageBay FindSlotByID(
        string slotID
    )
    {
        if (string.IsNullOrWhiteSpace(
                slotID
            ))
            return null;

        for (var i = 0;
             i < registeredSlots.Count;
             i++)
        {
            var bay =
                registeredSlots[i];

            if (bay == null) continue;

            if (bay.SlotID == slotID) return bay;
        }

        return null;
    }
}