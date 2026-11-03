using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(NavMeshModifier))]
public class FixedNavigationBlocker : MonoBehaviour
{
    private NavMeshModifier navMeshModifier;

    private void Awake()
    {
        ConfigureModifier();
    }

    private void OnValidate()
    {
        ConfigureModifier();
    }

    private void ConfigureModifier()
    {
        if (navMeshModifier == null)
        {
            navMeshModifier = GetComponent<NavMeshModifier>();
        }

        if (navMeshModifier == null)
            return;

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");

        if (notWalkableArea < 0)
        {
            Debug.LogError("The NavMesh area 'Not Walkable' could not be found.", this);

            return;
        }

        navMeshModifier.overrideArea = true;
        navMeshModifier.area = notWalkableArea;
        navMeshModifier.ignoreFromBuild = false;
    }
}