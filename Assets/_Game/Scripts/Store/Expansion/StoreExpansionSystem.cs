using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreExpansionSystem : MonoBehaviour
{
    [Header("Store")]
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private StoreLevelProgression storeLevelProgression;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Expansions")]
    [SerializeField] private StoreExpansionZone[] expansionZones;

    public int ExpansionCount =>
        expansionZones != null ? expansionZones.Length : 0;

    public StoreExpansionZone GetExpansion(int index)
    {
        if (expansionZones == null ||
            index < 0 ||
            index >= expansionZones.Length)
        {
            return null;
        }

        return expansionZones[index];
    }

    public bool CanPurchase(
        StoreExpansionZone zone,
        out string reason)
    {
        if (storeEconomy == null ||
            storeClock == null ||
            customerFlow == null ||
            storeLevelProgression == null ||
            navMeshSurface == null)
        {
            reason = "Store expansion is not configured.";
            return false;
        }

        if (zone == null ||
            !ContainsZone(zone) ||
            !zone.IsConfigured)
        {
            reason = "This expansion is not configured.";
            return false;
        }

        if (zone.IsUnlocked)
        {
            reason = "This expansion is already owned.";
            return false;
        }

        if (storeClock.IsStoreOpen ||
            storeClock.HasDayEnded ||
            customerFlow.ActiveCustomerCount > 0)
        {
            reason = "Expand the store while it is closed and empty.";
            return false;
        }

        StoreExpansionDefinition definition = zone.Definition;

        if (storeLevelProgression.CurrentLevel <
            definition.RequiredStoreLevel)
        {
            reason =
                $"Requires Store Level {definition.RequiredStoreLevel}.";
            return false;
        }

        if (storeEconomy.CurrentMoney <
            definition.PurchasePrice)
        {
            reason =
                $"Not enough money. Need £{definition.PurchasePrice:0.00}.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryPurchase(
        StoreExpansionZone zone,
        out string reason)
    {
        if (!CanPurchase(zone, out reason))
            return false;

        StoreExpansionDefinition definition = zone.Definition;

        if (definition.PurchasePrice > 0f &&
            !storeEconomy.TrySpend(definition.PurchasePrice))
        {
            reason = "Not enough money.";
            return false;
        }

        zone.SetUnlocked(true);

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();

        reason =
            $"{definition.DisplayName} purchased. New floor space is now available.";

        return true;
    }

    public List<string> GetUnlockedExpansionIDs()
    {
        List<string> unlockedIDs = new();

        if (expansionZones == null)
            return unlockedIDs;

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone == null ||
                !zone.IsUnlocked ||
                zone.Definition == null ||
                string.IsNullOrWhiteSpace(zone.Definition.ExpansionID))
            {
                continue;
            }

            unlockedIDs.Add(zone.Definition.ExpansionID);
        }

        unlockedIDs.Sort();
        return unlockedIDs;
    }

    public void RestoreUnlockedExpansionIDs(
        List<string> unlockedIDs,
        int saveVersion)
    {
        if (expansionZones == null)
            return;

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone != null)
                zone.SetUnlocked(false);
        }

        // Version 20 and older had no store expansion state.
        if (saveVersion < 21 || unlockedIDs == null)
        {
            Physics.SyncTransforms();
            return;
        }

        HashSet<string> restoredIDs = new(unlockedIDs);

        foreach (string expansionID in restoredIDs)
        {
            StoreExpansionZone zone =
                FindZoneByID(expansionID);

            if (zone == null)
            {
                Debug.LogWarning(
                    $"Saved expansion '{expansionID}' does not exist in the current scene.",
                    this
                );

                continue;
            }

            zone.SetUnlocked(true);
        }

        Physics.SyncTransforms();

        // StoreFurnitureLayout restores immediately after this during load
        // and rebuilds NavMesh once all furniture is back in place.
    }

    private StoreExpansionZone FindZoneByID(string expansionID)
    {
        if (string.IsNullOrWhiteSpace(expansionID) ||
            expansionZones == null)
        {
            return null;
        }

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone == null || zone.Definition == null)
                continue;

            if (zone.Definition.ExpansionID == expansionID)
                return zone;
        }

        return null;
    }

    private bool ContainsZone(StoreExpansionZone candidate)
    {
        if (candidate == null || expansionZones == null)
            return false;

        foreach (StoreExpansionZone zone in expansionZones)
        {
            if (zone == candidate)
                return true;
        }

        return false;
    }
}