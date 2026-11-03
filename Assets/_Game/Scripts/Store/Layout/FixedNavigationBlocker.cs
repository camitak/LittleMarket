using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(NavMeshModifierVolume))]
public class FixedNavigationBlocker : MonoBehaviour
{
    [Header("Navigation Footprint")] 
    [Min(0f)] [SerializeField] private float horizontalPadding = 0.05f;
    [Min(0f)] [SerializeField] private float floorOverlap = 0.20f;

    private BoxCollider sourceCollider;
    private NavMeshModifierVolume modifierVolume;

    private void Awake()
    {
        CacheReferences();
        RefreshFromCollider();
    }

    private void OnValidate()
    {
        CacheReferences();
        RefreshFromCollider();
    }

    private void RefreshFromCollider()
    {
        if (sourceCollider == null || modifierVolume == null)
        {
            return;
        }

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");

        if (notWalkableArea < 0)
        {
            Debug.LogError("The NavMesh area 'Not Walkable' could not be found.", this);

            return;
        }

        Vector3 center = sourceCollider.center;

        Vector3 size = sourceCollider.size;

        center.y -= floorOverlap * 0.5f;

        size.x += horizontalPadding * 2f;

        size.y += floorOverlap;

        size.z += horizontalPadding * 2f;

        modifierVolume.center = center;

        modifierVolume.size = size;

        modifierVolume.area = notWalkableArea;
    }

    private void CacheReferences()
    {
        if (sourceCollider == null)
        {
            sourceCollider = GetComponent<BoxCollider>();
        }

        if (modifierVolume == null)
        {
            modifierVolume = GetComponent<NavMeshModifierVolume>();
        }
    }
}