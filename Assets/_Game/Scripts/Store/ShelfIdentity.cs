using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class ShelfIdentity : MonoBehaviour
{
    [Header("Furniture")]
    [SerializeField] private FurnitureDefinition furnitureDefinition;

    [Header("Persistent Identity")]
    [Tooltip("Generated automatically for each scene shelf; do not change manually.")]
    [SerializeField] private string persistentID = "";

    public string PersistentID => persistentID;
    public FurnitureDefinition FurnitureDefinition => furnitureDefinition;

    public void AssignRuntimeID(string newID)
    {
        if (string.IsNullOrWhiteSpace(newID))
        {
            Debug.LogError("A runtime shelf needs a non-empty ID.", this);
            return;
        }

        persistentID = newID;
    }

    public void AssignFurnitureDefinition(FurnitureDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError("A furniture instance needs a valid FurnitureDefinition.", this);
            return;
        }

        furnitureDefinition = definition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            persistentID = "";
            return;
        }

        if (!gameObject.scene.IsValid())
            return;

        if (string.IsNullOrWhiteSpace(persistentID) || HasDuplicateID())
        {
            persistentID = Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
    }

    private bool HasDuplicateID()
    {
        if (string.IsNullOrWhiteSpace(persistentID))
            return false;

        ShelfIdentity[] identities = FindObjectsByType<ShelfIdentity>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (ShelfIdentity other in identities)
        {
            if (other == null || other == this)
                continue;

            if (other.gameObject.scene != gameObject.scene)
                continue;

            if (other.persistentID == persistentID)
                return true;
        }

        return false;
    }
#endif
}