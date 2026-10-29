using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_FurnitureCatalog",
    menuName = "Little Market/Furniture/Furniture Catalog"
)]
public class FurnitureCatalog : ScriptableObject
{
    [SerializeField] private List<FurnitureDefinition> entries = new();

    public int Count => entries.Count;

    public FurnitureDefinition GetAt(int index)
    {
        if (index < 0 || index >= entries.Count)
            return null;

        return entries[index];
    }

    public bool Contains(FurnitureDefinition definition)
    {
        return definition != null && entries.Contains(definition);
    }

    public bool TryGetByID(string furnitureID, out FurnitureDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(furnitureID))
            return false;

        foreach (FurnitureDefinition entry in entries)
        {
            if (entry == null)
                continue;

            if (entry.FurnitureID != furnitureID)
                continue;

            definition = entry;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<string> seenIDs = new();

        foreach (FurnitureDefinition entry in entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.FurnitureID))
            {
                Debug.LogWarning(
                    $"Furniture definition '{entry.name}' has no Furniture ID.",
                    this
                );

                continue;
            }

            if (!seenIDs.Add(entry.FurnitureID))
            {
                Debug.LogWarning(
                    $"Furniture ID '{entry.FurnitureID}' appears more than once in '{name}'.",
                    this
                );
            }
        }
    }
#endif
}