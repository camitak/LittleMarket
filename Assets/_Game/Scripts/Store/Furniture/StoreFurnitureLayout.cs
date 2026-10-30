using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// Owns persistent layout and reconstruction for the current shelf-display
// furniture family. SaveManager still owns file serialization.
[DisallowMultipleComponent]
public class StoreFurnitureLayout : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Furniture Catalog")]
    [SerializeField] private FurnitureCatalog furnitureCatalog;

    [Tooltip("Used when loading version-19 saves, where every purchased item was the old regular shelf.")]
    [SerializeField] private FurnitureDefinition legacyVersion19ShelfDefinition;

    [Header("Shelf Registration")]
    [SerializeField] private ShelfRegistry shelfRegistry;

    private readonly Dictionary<string, Pose> authoredPositions = new();
    private readonly HashSet<ShelfIdentity> pendingRemoval = new();

    private bool capturedAuthoredLayout;

    private void Awake()
    {
        if (!TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
            return;

        foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
        {
            Transform root = entry.Value.transform;
            authoredPositions.Add(entry.Key, new Pose(root.position, root.rotation));
        }

        capturedAuthoredLayout = true;
    }

    public bool TryCreatePreviewFurniture(
        FurnitureDefinition definition,
        out ShelfIdentity furniture)
    {
        furniture = null;

        if (!capturedAuthoredLayout ||
            navMeshSurface == null ||
            shelfRegistry == null ||
            !IsCatalogDefinitionUsable(definition) ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> existing))
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

    public void DiscardUnconfirmedFurniture(ShelfIdentity furniture)
    {
        if (furniture == null)
            return;

        if (authoredPositions.ContainsKey(furniture.PersistentID))
            return;

        RetireShelf(furniture);
    }

    public bool TryCreateSaveData(out List<ShelfPlacementSaveData> entries)
    {
        entries = new List<ShelfPlacementSaveData>();

        if (navMeshSurface == null ||
            shelfRegistry == null ||
            furnitureCatalog == null ||
            !capturedAuthoredLayout ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
        {
            return false;
        }

        foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
        {
            ShelfIdentity shelf = entry.Value;
            bool purchasedByPlayer = !authoredPositions.ContainsKey(entry.Key);

            if (purchasedByPlayer && !IsCatalogDefinitionUsable(shelf.FurnitureDefinition))
            {
                return Fail(
                    $"Purchased furniture '{shelf.name}' has no valid FurnitureDefinition."
                );
            }

            Transform root = shelf.transform;

            entries.Add(new ShelfPlacementSaveData
            {
                shelfID = entry.Key,
                position = root.position,
                rotation = root.rotation,
                purchasedByPlayer = purchasedByPlayer,
                furnitureDefinitionID = purchasedByPlayer
                    ? shelf.FurnitureDefinition.FurnitureID
                    : ""
            });
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.shelfID, b.shelfID));
        return true;
    }

    public bool CanRestoreSaveData(
        List<ShelfPlacementSaveData> entries,
        int saveVersion)
    {
        if (!capturedAuthoredLayout ||
            navMeshSurface == null ||
            shelfRegistry == null ||
            furnitureCatalog == null ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> existing))
        {
            return false;
        }

        foreach (string id in authoredPositions.Keys)
        {
            if (!existing.ContainsKey(id))
                return Fail("An authored shelf is missing from the scene.");
        }

        if (saveVersion < 18)
            return true;

        if (entries == null)
            return Fail("Shelf placements are missing in save version 18+.");

        HashSet<string> seen = new();
        int authoredCount = 0;

        foreach (ShelfPlacementSaveData entry in entries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.shelfID) ||
                !seen.Add(entry.shelfID))
            {
                return Fail("Saved shelf identities are missing or duplicated.");
            }

            bool authored = authoredPositions.ContainsKey(entry.shelfID);

            if (authored)
            {
                if (saveVersion >= 19 && entry.purchasedByPlayer)
                {
                    return Fail(
                        "An authored shelf is incorrectly marked as purchased."
                    );
                }

                authoredCount++;
                continue;
            }

            if (saveVersion < 19 || !entry.purchasedByPlayer)
            {
                return Fail(
                    "An unknown shelf is not a valid purchased furniture entry."
                );
            }

            if (!TryResolvePurchasedDefinition(entry, saveVersion, out _))
            {
                return Fail(
                    $"Purchased furniture '{entry.shelfID}' has no valid furniture definition."
                );
            }
        }

        if (authoredCount != authoredPositions.Count)
            return Fail("Saved layout does not contain every original shelf.");

        if (saveVersion < 19 && entries.Count != authoredPositions.Count)
            return Fail("Legacy furniture save count does not match the authored layout.");

        return true;
    }

    // Called before shelf inventory restore so purchased ShelfSlots exist
    // and are registered before their product data is reconstructed.
    public void RestoreLayout(
        List<ShelfPlacementSaveData> entries,
        int saveVersion)
    {
        if (!CanRestoreSaveData(entries, saveVersion))
        {
            Debug.LogError("Shelf layout restore aborted.", this);
            return;
        }

        if (!TryFindShelves(out Dictionary<string, ShelfIdentity> current))
            return;

        foreach (KeyValuePair<string, ShelfIdentity> pair in current)
        {
            if (!authoredPositions.ContainsKey(pair.Key))
                RetireShelf(pair.Value);
        }

        if (saveVersion < 18)
        {
            foreach (KeyValuePair<string, Pose> pair in authoredPositions)
            {
                current[pair.Key].transform.SetPositionAndRotation(
                    pair.Value.position,
                    pair.Value.rotation
                );
            }
        }
        else
        {
            foreach (ShelfPlacementSaveData entry in entries)
            {
                if (authoredPositions.ContainsKey(entry.shelfID))
                {
                    current[entry.shelfID].transform.SetPositionAndRotation(
                        entry.position,
                        entry.rotation
                    );

                    continue;
                }

                if (!TryResolvePurchasedDefinition(
                        entry,
                        saveVersion,
                        out FurnitureDefinition definition))
                {
                    Debug.LogError(
                        $"Could not resolve purchased furniture '{entry.shelfID}'.",
                        this
                    );

                    continue;
                }

                ShelfIdentity purchased = Instantiate(
                    definition.Prefab,
                    entry.position,
                    entry.rotation
                );

                purchased.AssignRuntimeID(entry.shelfID);
                purchased.AssignFurnitureDefinition(definition);
                purchased.gameObject.name = $"Purchased_{definition.FurnitureID}";

                foreach (ShelfSlot slot in purchased.GetComponentsInChildren<ShelfSlot>(true))
                    shelfRegistry.RegisterSlot(slot);
            }
        }

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }

    private bool TryResolvePurchasedDefinition(
        ShelfPlacementSaveData entry,
        int saveVersion,
        out FurnitureDefinition definition)
    {
        definition = null;

        if (saveVersion >= 20)
        {
            if (furnitureCatalog == null)
                return false;

            if (!furnitureCatalog.TryGetByID(entry.furnitureDefinitionID, out definition))
                return false;

            return IsCatalogDefinitionUsable(definition);
        }

        definition = legacyVersion19ShelfDefinition;
        return IsCatalogDefinitionUsable(definition);
    }

    private bool IsCatalogDefinitionUsable(FurnitureDefinition definition)
    {
        return furnitureCatalog != null &&
               definition != null &&
               furnitureCatalog.Contains(definition) &&
               definition.IsConfigured;
    }

    private void RetireShelf(ShelfIdentity shelf)
    {
        if (shelf == null || pendingRemoval.Contains(shelf))
            return;

        foreach (ShelfSlot slot in shelf.GetComponentsInChildren<ShelfSlot>(true))
            shelfRegistry.UnregisterSlot(slot);

        pendingRemoval.Add(shelf);
        shelf.gameObject.SetActive(false);
        Destroy(shelf.gameObject);
    }

    private bool TryFindShelves(out Dictionary<string, ShelfIdentity> shelves)
    {
        shelves = new Dictionary<string, ShelfIdentity>();

        ShelfIdentity[] found = FindObjectsByType<ShelfIdentity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (ShelfIdentity shelf in found)
        {
            if (shelf == null ||
                pendingRemoval.Contains(shelf) ||
                shelf.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            string id = shelf.PersistentID;

            if (string.IsNullOrWhiteSpace(id) || shelves.ContainsKey(id))
                return Fail("Duplicate or empty ShelfIdentity on " + shelf.name);

            shelves.Add(id, shelf);
        }

        return shelves.Count > 0 || Fail("No shelves were found in the scene.");
    }

    private bool Fail(string message)
    {
        Debug.LogError(message, this);
        return false;
    }
}