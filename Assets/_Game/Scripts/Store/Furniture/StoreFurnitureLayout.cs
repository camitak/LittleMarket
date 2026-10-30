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

    [Header("Shelf Registration")]
    [SerializeField] private ShelfRegistry shelfRegistry;

    private readonly Dictionary<string, Pose> authoredPositions = new();
    private readonly HashSet<FurnitureIdentity> pendingRemoval = new();

    private bool capturedAuthoredLayout;

    private void Awake()
    {
        if (!TryFindFurniture(out Dictionary<string, FurnitureIdentity> furniture))
            return;

        foreach (KeyValuePair<string, FurnitureIdentity> entry in furniture)
        {
            Transform root = entry.Value.transform;

            authoredPositions.Add(
                entry.Key,
                new Pose(root.position, root.rotation)
            );
        }

        capturedAuthoredLayout = true;
    }

    public bool TryCreatePreviewFurniture(
        FurnitureDefinition definition,
        out FurnitureIdentity furniture)
    {
        furniture = null;

        if (!capturedAuthoredLayout ||
            navMeshSurface == null ||
            shelfRegistry == null ||
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
        while (existing.ContainsKey(id));

        furniture.AssignRuntimeID(id);
        furniture.AssignFurnitureDefinition(definition);
        furniture.gameObject.name = $"Purchased_{definition.FurnitureID}";

        return true;
    }

    public void DiscardUnconfirmedFurniture(FurnitureIdentity furniture)
    {
        if (furniture == null)
            return;

        if (authoredPositions.ContainsKey(furniture.PersistentID))
            return;

        RetireFurniture(furniture);
    }

    public bool TryCreateSaveData(
        out List<FurniturePlacementSaveData> entries)
    {
        entries = new List<FurniturePlacementSaveData>();

        if (navMeshSurface == null ||
            shelfRegistry == null ||
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
                return Fail(
                    $"Furniture '{identity.name}' has no valid FurnitureDefinition."
                );
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

        entries.Sort(
            (a, b) => string.CompareOrdinal(
                a.furnitureID,
                b.furnitureID
            )
        );

        return true;
    }

    public bool CanRestoreSaveData(
        List<FurniturePlacementSaveData> entries)
    {
        if (!capturedAuthoredLayout ||
            navMeshSurface == null ||
            shelfRegistry == null ||
            furnitureCatalog == null ||
            !TryFindFurniture(out Dictionary<string, FurnitureIdentity> current))
        {
            return false;
        }

        if (entries == null)
            return Fail("Furniture placements are missing from the save.");

        foreach (string id in authoredPositions.Keys)
        {
            if (!current.ContainsKey(id))
                return Fail($"Authored furniture '{id}' is missing from the scene.");
        }

        HashSet<string> seenIDs = new();
        int authoredCount = 0;

        foreach (FurniturePlacementSaveData entry in entries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.furnitureID) ||
                !seenIDs.Add(entry.furnitureID))
            {
                return Fail("Saved furniture identities are missing or duplicated.");
            }

            if (authoredPositions.ContainsKey(entry.furnitureID))
            {
                authoredCount++;
                continue;
            }

            if (!TryResolveDefinition(entry, out _))
            {
                return Fail(
                    $"Furniture '{entry.furnitureID}' has an invalid furniture definition."
                );
            }
        }

        if (authoredCount != authoredPositions.Count)
        {
            return Fail(
                "Saved furniture layout does not contain every authored furniture item."
            );
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
            return;

        foreach (KeyValuePair<string, FurnitureIdentity> pair in current)
        {
            if (!authoredPositions.ContainsKey(pair.Key))
                RetireFurniture(pair.Value);
        }

        foreach (FurniturePlacementSaveData entry in entries)
        {
            if (authoredPositions.ContainsKey(entry.furnitureID))
            {
                current[entry.furnitureID].transform.SetPositionAndRotation(
                    entry.position,
                    entry.rotation
                );

                continue;
            }

            if (!TryResolveDefinition(
                    entry,
                    out FurnitureDefinition definition))
            {
                Debug.LogError(
                    $"Could not resolve furniture '{entry.furnitureID}'.",
                    this
                );

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

            RegisterShelfSlots(purchased);
        }

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }

    private bool TryResolveDefinition(
        FurniturePlacementSaveData entry,
        out FurnitureDefinition definition)
    {
        definition = null;

        if (entry == null || furnitureCatalog == null)
            return false;

        if (!furnitureCatalog.TryGetByID(
                entry.furnitureDefinitionID,
                out definition))
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

    private void RegisterShelfSlots(FurnitureIdentity furniture)
    {
        if (furniture == null)
            return;

        ShelfSlot[] slots =
            furniture.GetComponentsInChildren<ShelfSlot>(true);

        foreach (ShelfSlot slot in slots)
            shelfRegistry.RegisterSlot(slot);
    }

    private void RetireFurniture(FurnitureIdentity furniture)
    {
        if (furniture == null || pendingRemoval.Contains(furniture))
            return;

        ShelfSlot[] slots =
            furniture.GetComponentsInChildren<ShelfSlot>(true);

        foreach (ShelfSlot slot in slots)
            shelfRegistry.UnregisterSlot(slot);

        pendingRemoval.Add(furniture);

        furniture.gameObject.SetActive(false);
        Destroy(furniture.gameObject);
    }

    private bool TryFindFurniture(
        out Dictionary<string, FurnitureIdentity> furniture)
    {
        furniture = new Dictionary<string, FurnitureIdentity>();

        FurnitureIdentity[] found =
            FindObjectsByType<FurnitureIdentity>(
                FindObjectsInactive.Include
            );

        foreach (FurnitureIdentity identity in found)
        {
            if (identity == null ||
                pendingRemoval.Contains(identity) ||
                identity.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            string id = identity.PersistentID;

            if (string.IsNullOrWhiteSpace(id) ||
                furniture.ContainsKey(id))
            {
                return Fail(
                    $"Duplicate or empty FurnitureIdentity on '{identity.name}'."
                );
            }

            furniture.Add(id, identity);
        }

        return furniture.Count > 0 ||
               Fail("No managed furniture was found in the scene.");
    }

    private bool Fail(string message)
    {
        Debug.LogError(message, this);
        return false;
    }
}