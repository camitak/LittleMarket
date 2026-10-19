using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class ShelfIdentity : MonoBehaviour
{
    [Tooltip(
        "Generated automatically for each shelf instance. " +
        "Do not edit this value manually."
    )]
    [SerializeField]
    private string persistentID = "";

    public string PersistentID => persistentID;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // The prefab asset itself must remain blank.
        // Individual scene instances own the persistent ID.
        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            persistentID = "";
            return;
        }

        if (!gameObject.scene.IsValid()) return;

        if (string.IsNullOrWhiteSpace(persistentID) ||
            HasDuplicateID())
            GenerateNewID();
    }

    private bool HasDuplicateID()
    {
        if (string.IsNullOrWhiteSpace(persistentID)) return false;

        var identities =
            FindObjectsByType<ShelfIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (var i = 0; i < identities.Length; i++)
        {
            var other = identities[i];

            if (other == null ||
                other == this ||
                other.gameObject.scene != gameObject.scene)
                continue;

            if (other.persistentID == persistentID) return true;
        }

        return false;
    }

    private void GenerateNewID()
    {
        persistentID = Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(this);
    }
#endif
}