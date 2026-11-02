using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(FurnitureIdentity))]
[RequireComponent(typeof(NavMeshModifierVolume))]
public class FurnitureNavigationBlocker : MonoBehaviour
{
    [Header("Navigation Footprint")] 
    [Min(0f)] [SerializeField] private float horizontalPadding = 0.05f;
    [Min(0f)] [SerializeField] private float floorOverlap = 0.20f;

    private FurnitureIdentity furnitureIdentity;
    private NavMeshModifierVolume modifierVolume;

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        RefreshFromDefinition();
    }

    private void OnValidate()
    {
        CacheReferences();
        RefreshFromDefinition();
    }

    public void RefreshFromDefinition()
    {
        CacheReferences();

        if (furnitureIdentity == null || modifierVolume == null)
        {
            return;
        }

        FurnitureDefinition definition = furnitureIdentity.FurnitureDefinition;

        if (definition == null)
            return;

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");

        if (notWalkableArea < 0)
        {
            Debug.LogError("The NavMesh area 'Not Walkable' could not be found.", this);

            return;
        }

        Vector3 halfExtents = definition.BodyHalfExtents;

        Vector3 center = definition.BodyCenter;

        center.y -= floorOverlap * 0.5f;

        modifierVolume.center = center;

        modifierVolume.size = new Vector3(
            halfExtents.x * 2f + horizontalPadding * 2f,
            halfExtents.y * 2f + floorOverlap, 
            halfExtents.z * 2f + horizontalPadding * 2f);

        modifierVolume.area = notWalkableArea;
    }

    private void CacheReferences()
    {
        if (furnitureIdentity == null)
        {
            furnitureIdentity = GetComponent<FurnitureIdentity>();
        }

        if (modifierVolume == null)
        {
            modifierVolume = GetComponent<NavMeshModifierVolume>();
        }
    }
}