using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreFurnitureLayout : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Furniture Catalog")]
    [SerializeField] private FurnitureCatalog furnitureCatalog;

    private readonly Dictionary<string, Pose> authoredPositions = new();
    private readonly Dictionary<string, FurnitureIdentity> authoredFurniture = new();
    private readonly HashSet<FurnitureIdentity> retiredFurniture = new();

    private bool capturedAuthoredLayout;

    private void Awake()
    {
        if (!TryFindFurniture(out Dictionary<string, FurnitureIdentity> furniture) || furniture.Count == 0)
        {
            Fail("No managed furniture was found in the scene.");
            return;
        }

        foreach (KeyValuePair<string, FurnitureIdentity> entry in furniture)
        {
            Transform root = entry.Value.transform;

            authoredPositions.Add(entry.Key, new Pose(root.position, root.rotation));
            authoredFurniture.Add(entry.Key, entry.Value);
        }

        capturedAuthoredLayout = true;
    }

    public int CountFurnitureByDefinition(FurnitureDefinition definition)
    {
        if (definition == null || !TryFindFurniture(out Dictionary<string, FurnitureIdentity> furniture))
        {
            return 0;
        }

        int count = 0;

        foreach (FurnitureIdentity identity in furniture.Values)
        {
            if (identity != null && identity.FurnitureDefinition == definition)
            {
                count++;
            }
        }

        return count;
    }

    public bool CanSellFurniture(FurnitureIdentity furniture, out string reason)
    {
        if (!capturedAuthoredLayout || navMeshSurface == null || furnitureCatalog == null)
        {
            reason = "Furniture selling is not configured.";
            return false;
        }

        if (furniture == null ||
            retiredFurniture.Contains(furniture) ||
            furniture.gameObject.scene != gameObject.scene)
        {
            reason = "This furniture is not available to sell.";
            return false;
        }

        FurnitureDefinition definition = furniture.FurnitureDefinition;

        if (!IsCatalogDefinitionUsable(definition))
        {
            reason = "This furniture is not part of the current furniture catalog.";
            return false;
        }

        if (!definition.Sellable)
        {
            reason = $"{definition.DisplayName} cannot be sold.";
            return false;
        }

        if (!furniture.CanSellFurniture(out reason))
        {
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryRemoveFurniture(FurnitureIdentity furniture, out string reason)
    {
        if (!CanSellFurniture(furniture, out reason))
        {
            return false;
        }

        RetireFurniture(furniture);

        reason = "";
        return true;
    }

    public bool TryCreatePreviewFurniture(FurnitureDefinition definition, out FurnitureIdentity furniture)
    {
        furniture = null;

        if (!capturedAuthoredLayout ||
            navMeshSurface == null ||
            !IsCatalogDefinitionUsable(definition) ||
            !TryFindFurniture(out Dictionary<string, FurnitureIdentity> existing))
        {
            return false;
        }

        furniture = Instantiate(
            definition.Prefab,
            new Vector3(0f, -100f, 0f),
            Quaternion.identity
        );

        string id;

        do
        {
            id = Guid.NewGuid().ToString("N");
        }
        while (existing.ContainsKey(id) || authoredPositions.ContainsKey(id));

        furniture.AssignRuntimeID(id);
        furniture.AssignFurnitureDefinition(definition);
        furniture.SetRuntimeActive(false);

        furniture.gameObject.name = $"Purchased_{definition.FurnitureID}";

        return true;
    }

    public void DiscardUnconfirmedFurniture(FurnitureIdentity furniture)
    {
        if (furniture == null)
        {
            return;
        }

        if (authoredPositions.ContainsKey(furniture.PersistentID))
        {
            return;
        }

        RetireFurniture(furniture);
    }

    public bool TryCreateSaveData(out List<FurniturePlacementSaveData> entries)
    {
        entries = new List<FurniturePlacementSaveData>();

        if (navMeshSurface == null ||
            furnitureCatalog == null ||
            !capturedAuthoredLayout ||
            !TryFindFurniture(out Dictionary<string, FurnitureIdentity> furniture))
        {
            return false;
        }

        foreach (KeyValuePair<string, FurnitureIdentity> entry in furniture)
        {
            FurnitureIdentity identity = entry.Value;
            FurnitureDefinition definition = identity.FurnitureDefinition;

            if (!IsCatalogDefinitionUsable(definition))
            {
                return Fail($"Furniture '{identity.name}' has no valid FurnitureDefinition.");
            }

            Transform root = identity.transform;

            entries.Add(new FurniturePlacementSaveData
            {
                furnitureID = entry.Key,
                furnitureDefinitionID = definition.FurnitureID,
                position = root.position,
                rotation = root.rotation
            });
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.furnitureID, b.furnitureID));

        return true;
    }

    public bool CanRestoreSaveData(List<FurniturePlacementSaveData> entries)
    {
        if (!capturedAuthoredLayout || navMeshSurface == null || furnitureCatalog == null)
        {
            return false;
        }

        if (entries == null)
        {
            return Fail("Furniture placements are missing from the save.");
        }

        HashSet<string> seenIDs = new();

        foreach (FurniturePlacementSaveData entry in entries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.furnitureID) ||
                string.IsNullOrWhiteSpace(entry.furnitureDefinitionID) ||
                !seenIDs.Add(entry.furnitureID))
            {
                return Fail("Saved furniture identities are missing or duplicated.");
            }

            if (authoredFurniture.TryGetValue(entry.furnitureID, out FurnitureIdentity authored))
            {
                FurnitureDefinition authoredDefinition = authored != null ? authored.FurnitureDefinition : null;

                if (authoredDefinition == null ||
                    authoredDefinition.FurnitureID != entry.furnitureDefinitionID)
                {
                    return Fail($"Authored furniture '{entry.furnitureID}' has an invalid saved definition.");
                }

                continue;
            }

            if (!TryResolveDefinition(entry, out _))
            {
                return Fail($"Furniture '{entry.furnitureID}' has an invalid furniture definition.");
            }
        }

        return true;
    }

    public void RestoreLayout(List<FurniturePlacementSaveData> entries)
    {
        if (!CanRestoreSaveData(entries))
        {
            Debug.LogError("Furniture layout restore aborted.", this);
            return;
        }

        if (!TryFindFurniture(out Dictionary<string, FurnitureIdentity> current))
        {
            return;
        }

        foreach (FurnitureIdentity identity in current.Values)
        {
            RetireFurniture(identity);
        }

        foreach (FurniturePlacementSaveData entry in entries)
        {
            if (authoredFurniture.TryGetValue(entry.furnitureID, out FurnitureIdentity authored))
            {
                RestoreAuthoredFurniture(authored, entry.position, entry.rotation);
                continue;
            }

            if (!TryResolveDefinition(entry, out FurnitureDefinition definition))
            {
                Debug.LogError($"Could not resolve furniture '{entry.furnitureID}'.", this);
                continue;
            }

            FurnitureIdentity purchased = Instantiate(
                definition.Prefab,
                entry.position,
                entry.rotation
            );

            purchased.AssignRuntimeID(entry.furnitureID);
            purchased.AssignFurnitureDefinition(definition);
            purchased.gameObject.name = $"Purchased_{definition.FurnitureID}";
            purchased.SetRuntimeActive(true);
        }

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }

    private void RestoreAuthoredFurniture(FurnitureIdentity furniture, Vector3 position, Quaternion rotation)
    {
        if (furniture == null)
        {
            return;
        }

        retiredFurniture.Remove(furniture);

        furniture.transform.SetPositionAndRotation(position, rotation);
        furniture.gameObject.SetActive(true);
        furniture.SetRuntimeActive(true);
    }

    private bool TryResolveDefinition(FurniturePlacementSaveData entry, out FurnitureDefinition definition)
    {
        definition = null;

        if (entry == null || furnitureCatalog == null)
        {
            return false;
        }

        if (!furnitureCatalog.TryGetByID(entry.furnitureDefinitionID, out definition))
        {
            return false;
        }

        return IsCatalogDefinitionUsable(definition);
    }

    private bool IsCatalogDefinitionUsable(FurnitureDefinition definition)
    {
        return furnitureCatalog != null &&
               definition != null &&
               furnitureCatalog.Contains(definition) &&
               definition.IsConfigured;
    }

    private void RetireFurniture(FurnitureIdentity furniture)
    {
        if (furniture == null || retiredFurniture.Contains(furniture))
        {
            return;
        }

        bool authored = authoredFurniture.ContainsKey(furniture.PersistentID);

        furniture.SetRuntimeActive(false);

        retiredFurniture.Add(furniture);

        furniture.gameObject.SetActive(false);

        if (!authored)
        {
            Destroy(furniture.gameObject);
        }
    }

    private bool TryFindFurniture(out Dictionary<string, FurnitureIdentity> furniture)
    {
        furniture = new Dictionary<string, FurnitureIdentity>();

        FurnitureIdentity[] found =
            FindObjectsByType<FurnitureIdentity>(FindObjectsInactive.Include);

        foreach (FurnitureIdentity identity in found)
        {
            if (identity == null ||
                retiredFurniture.Contains(identity) ||
                identity.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            string id = identity.PersistentID;

            if (string.IsNullOrWhiteSpace(id) || furniture.ContainsKey(id))
            {
                return Fail($"Duplicate or empty FurnitureIdentity on '{identity.name}'.");
            }

            furniture.Add(id, identity);
        }

        return true;
    }

    private bool Fail(string message)
    {
        Debug.LogError(message, this);
        return false;
    }
}