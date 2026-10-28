using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// Runtime world layout for existing scene shelves.
// SaveManager coordinates persistence; ShelfIdentity owns each stable ID.
[DisallowMultipleComponent]
public class StoreFurnitureLayout : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    // Scene-authored positions are the safe fallback for version 17 and older saves.
    private readonly Dictionary<string, Pose> authoredPositions = new();
    private bool capturedAuthoredLayout;

    private void Awake()
    {
        if (!TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
            return;

        foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
        {
            Transform root = entry.Value.transform;
            authoredPositions.Add(
                entry.Key,
                new Pose(root.position, root.rotation));
        }

        capturedAuthoredLayout = true;
    }

    public bool TryCreateSaveData(out List<ShelfPlacementSaveData> entries)
    {
        entries = new List<ShelfPlacementSaveData>();

        if (navMeshSurface == null || !capturedAuthoredLayout ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
            return false;

        foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
        {
            Transform root = entry.Value.transform;
            entries.Add(new ShelfPlacementSaveData
            {
                shelfID = entry.Key,
                position = root.position,
                rotation = root.rotation
            });
        }

        // Stable order gives readable, consistent JSON for debugging.
        entries.Sort((a, b) =>
            string.CompareOrdinal(a.shelfID, b.shelfID));

        return true;
    }

    // Validate BEFORE SaveManager changes money, products, or other loaded state.
    public bool CanRestoreSaveData(
        List<ShelfPlacementSaveData> entries,
        int saveVersion)
    {
        if (navMeshSurface == null || !capturedAuthoredLayout ||
            !TryFindShelves(out Dictionary<string, ShelfIdentity> shelves))
        {
            Debug.LogError("StoreFurnitureLayout references or shelf IDs are invalid.", this);
            return false;
        }

        if (saveVersion < 18)
            return true;

        if (entries == null)
        {
            Debug.LogError("Version 18 save has no shelfPlacements collection.", this);
            return false;
        }

        HashSet<string> seen = new();
        foreach (ShelfPlacementSaveData entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.shelfID) ||
                !seen.Add(entry.shelfID) || !shelves.ContainsKey(entry.shelfID))
            {
                Debug.LogError(
                    "Saved furniture contains a missing, duplicated, or unknown shelf ID.",
                    this);
                return false;
            }
        }

        // Version 18 stores every currently authored shelf; a missing one means
        // loading would silently leave the shop with an inconsistent layout.
        if (seen.Count != shelves.Count)
        {
            Debug.LogError(
                "Saved furniture count does not match scene shelves. " +
                "Do not discard shelf inventory silently.", this);
            return false;
        }

        return true;
    }

    // Call before RestoreShelfInventory so the physical stock returns to its
    // existing shelf after that shelf is moved into the saved world position.
    public void RestoreLayout(
        List<ShelfPlacementSaveData> entries,
        int saveVersion)
    {
        if (!CanRestoreSaveData(entries, saveVersion))
        {
            Debug.LogError("Furniture layout restore was skipped.", this);
            return;
        }

        TryFindShelves(out Dictionary<string, ShelfIdentity> shelves);

        if (saveVersion < 18)
        {
            foreach (KeyValuePair<string, ShelfIdentity> entry in shelves)
            {
                if (!authoredPositions.TryGetValue(entry.Key, out Pose pose))
                    continue;

                entry.Value.transform.SetPositionAndRotation(
                    pose.position, pose.rotation);
            }
        }
        else
        {
            foreach (ShelfPlacementSaveData entry in entries)
            {
                shelves[entry.shelfID].transform.SetPositionAndRotation(
                    entry.position, entry.rotation);
            }
        }

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }

    private bool TryFindShelves(out Dictionary<string, ShelfIdentity> shelves)
    {
        shelves = new Dictionary<string, ShelfIdentity>();

        ShelfIdentity[] found = FindObjectsByType<ShelfIdentity>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ShelfIdentity shelf in found)
        {
            if (shelf == null || shelf.gameObject.scene != gameObject.scene)
                continue;

            string id = shelf.PersistentID;
            if (string.IsNullOrWhiteSpace(id) || shelves.ContainsKey(id))
            {
                Debug.LogError(
                    "Every scene shelf needs a unique ShelfIdentity.PersistentID: " +
                    shelf.name, shelf);
                return false;
            }

            shelves.Add(id, shelf);
        }

        if (shelves.Count == 0)
        {
            Debug.LogError("StoreFurnitureLayout did not find any scene shelves.", this);
            return false;
        }

        return true;
    }
}
