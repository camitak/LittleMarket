using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// Scene-authored shelf IDs stay stable. Purchased shelves receive runtime IDs.
// SaveManager serializes the data; ShelfSlot remains the only stock authority.
[DisallowMultipleComponent]
public class StoreFurnitureLayout : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Purchasable Shelf")]
    [SerializeField] private ShelfIdentity shelfPrefab;
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

    public bool TryCreatePreviewShelf(out ShelfIdentity shelf)
    {
        shelf = null;
        if (!capturedAuthoredLayout || shelfPrefab == null ||
            shelfRegistry == null || navMeshSurface == null ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> existing))
            return false;

        // New instances are created below the map and immediately passed to
        // FurniturePlacementController, which disables colliders during preview.
        shelf = Instantiate(shelfPrefab, new Vector3(0f, -100f, 0f),
            Quaternion.identity);
        string id = Guid.NewGuid().ToString("N");
        while (existing.ContainsKey(id)) id = Guid.NewGuid().ToString("N");
        shelf.AssignRuntimeID(id);
        shelf.gameObject.name = "Purchased_Shelf";
        return true;
    }

    public void DiscardUnconfirmedShelf(ShelfIdentity shelf)
    {
        if (shelf == null || authoredPositions.ContainsKey(shelf.PersistentID))
            return;
        RetireShelf(shelf);
    }

    public bool TryCreateSaveData(out List<ShelfPlacementSaveData> entries)
    {
        entries = new List<ShelfPlacementSaveData>();
        if (navMeshSurface == null || shelfRegistry == null ||
            !capturedAuthoredLayout ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
            return false;

        foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
        {
            Transform root = entry.Value.transform;
            entries.Add(new ShelfPlacementSaveData
            {
                shelfID = entry.Key,
                position = root.position,
                rotation = root.rotation,
                purchasedByPlayer = !authoredPositions.ContainsKey(entry.Key)
            });
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.shelfID, b.shelfID));
        return true;
    }

    // Preflight before SaveManager modifies money or inventory.
    public bool CanRestoreSaveData(List<ShelfPlacementSaveData> entries, int saveVersion)
    {
        if (!capturedAuthoredLayout || navMeshSurface == null || shelfRegistry == null ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> existing))
            return false;

        foreach (string id in authoredPositions.Keys)
            if (!existing.ContainsKey(id)) return Fail("An authored shelf is missing from the scene.");

        if (saveVersion < 18) return true;
        if (entries == null) return Fail("Shelf placements are missing in save version 18+. ");

        HashSet<string> seen = new();
        int authoredCount = 0;
        foreach (ShelfPlacementSaveData entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.shelfID) ||
                !seen.Add(entry.shelfID))
                return Fail("Saved shelf identities are missing or duplicated.");

            bool authored = authoredPositions.ContainsKey(entry.shelfID);
            if (authored)
            {
                if (saveVersion >= 19 && entry.purchasedByPlayer)
                    return Fail("An authored shelf is incorrectly marked as purchased.");
                authoredCount++;
            }
            else if (saveVersion < 19 || !entry.purchasedByPlayer)
            {
                return Fail("An unknown shelf is not a valid purchased furniture entry.");
            }
        }

        if (authoredCount != authoredPositions.Count)
            return Fail("Saved layout does not contain every original shelf.");
        if (saveVersion < 19 && entries.Count != authoredPositions.Count)
            return Fail("Legacy furniture save count does not match the authored layout.");
        if (saveVersion >= 19 && entries.Count > authoredCount && shelfPrefab == null)
            return Fail("Purchased shelf prefab is missing; cannot restore purchases.");
        return true;
    }

    // Called before shelf stock restore. New purchased shelf slots are registered
    // synchronously so their stable slot IDs can be found immediately.
    public void RestoreLayout(List<ShelfPlacementSaveData> entries, int saveVersion)
    {
        if (!CanRestoreSaveData(entries, saveVersion))
        {
            Debug.LogError("Shelf layout restore aborted.", this);
            return;
        }

        if (!TryFindShelves(out Dictionary<string, ShelfIdentity> current)) return;
        foreach (KeyValuePair<string, ShelfIdentity> pair in current)
        {
            if (!authoredPositions.ContainsKey(pair.Key)) RetireShelf(pair.Value);
        }

        if (saveVersion < 18)
        {
            foreach (KeyValuePair<string, Pose> pair in authoredPositions)
            {
                current[pair.Key].transform.SetPositionAndRotation(
                    pair.Value.position, pair.Value.rotation);
            }
        }
        else
        {
            foreach (ShelfPlacementSaveData entry in entries)
            {
                if (authoredPositions.ContainsKey(entry.shelfID))
                {
                    current[entry.shelfID].transform.SetPositionAndRotation(
                        entry.position, entry.rotation);
                    continue;
                }

                ShelfIdentity purchased = Instantiate(shelfPrefab,
                    entry.position, entry.rotation);
                purchased.AssignRuntimeID(entry.shelfID);
                purchased.gameObject.name = "Purchased_Shelf";
                foreach (ShelfSlot slot in purchased.GetComponentsInChildren<ShelfSlot>(true))
                    shelfRegistry.RegisterSlot(slot);
            }
        }

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }

    private void RetireShelf(ShelfIdentity shelf)
    {
        if (shelf == null || pendingRemoval.Contains(shelf)) return;
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
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ShelfIdentity shelf in found)
        {
            if (shelf == null || pendingRemoval.Contains(shelf) ||
                shelf.gameObject.scene != gameObject.scene)
                continue;

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
