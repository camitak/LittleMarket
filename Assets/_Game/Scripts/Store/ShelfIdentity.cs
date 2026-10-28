using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class ShelfIdentity : MonoBehaviour
{
    [Tooltip("Generated automatically for each scene shelf; do not change manually.")]
    [SerializeField] private string persistentID = "";

    public string PersistentID => persistentID;

    // For purchases, after Instantiate and before any ShelfSlot registers.
    // Existing authored shelf identities never need this method.
    public void AssignRuntimeID(string newID)
    {
        if (string.IsNullOrWhiteSpace(newID))
        {
            Debug.LogError("A runtime shelf needs a non-empty ID.", this);
            return;
        }

        persistentID = newID;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            persistentID = "";
            return;
        }

        if (!gameObject.scene.IsValid()) return;

        if (string.IsNullOrWhiteSpace(persistentID) || HasDuplicateID())
        {
            persistentID = Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
    }

    private bool HasDuplicateID()
    {
        if (string.IsNullOrWhiteSpace(persistentID)) return false;

        ShelfIdentity[] identities = FindObjectsByType<ShelfIdentity>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ShelfIdentity other in identities)
        {
            if (other == null || other == this || other.gameObject.scene != gameObject.scene)
                continue;
            if (other.persistentID == persistentID) return true;
        }

        return false;
    }
#endif
}