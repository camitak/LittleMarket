using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class FurnitureIdentity : MonoBehaviour
{
    [Header("Furniture")]
    [SerializeField] private FurnitureDefinition furnitureDefinition;

    [Header("Persistent Identity")]
    [Tooltip("Generated automatically for authored scene furniture; do not change manually.")]
    [SerializeField] private string persistentID = "";

    private bool runtimeActive = true;

    public string PersistentID => persistentID;
    public FurnitureDefinition FurnitureDefinition => furnitureDefinition;

    private void Start()
    {
        if (runtimeActive)
            RegisterRuntimeParticipants();
    }

    private void OnDestroy()
    {
        UnregisterRuntimeParticipants();
    }

    public void AssignRuntimeID(string newID)
    {
        if (string.IsNullOrWhiteSpace(newID))
        {
            Debug.LogError(
                "A runtime furniture instance needs a non-empty ID.",
                this
            );

            return;
        }

        persistentID = newID;
    }

    public void AssignFurnitureDefinition(FurnitureDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError(
                "A furniture instance needs a valid FurnitureDefinition.",
                this
            );

            return;
        }

        furnitureDefinition = definition;
    }

    public void SetRuntimeActive(bool active)
    {
        runtimeActive = active;

        if (active)
            RegisterRuntimeParticipants();
        else
            UnregisterRuntimeParticipants();
    }

    public bool CanMoveFurniture(out string reason)
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IFurnitureMoveConstraint constraint)
                continue;

            if (!constraint.CanMoveFurniture(out reason))
                return false;
        }

        reason = "";
        return true;
    }

    private void RegisterRuntimeParticipants()
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IFurnitureRuntimeParticipant participant)
                participant.RegisterFurnitureRuntime();
        }
    }

    private void UnregisterRuntimeParticipants()
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IFurnitureRuntimeParticipant participant)
                participant.UnregisterFurnitureRuntime();
        }
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

        FurnitureIdentity[] identities =
            FindObjectsByType<FurnitureIdentity>(
                FindObjectsInactive.Include
            );

        foreach (FurnitureIdentity other in identities)
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