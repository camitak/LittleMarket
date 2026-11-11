using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent] public sealed class LayoutFurnitureAccessVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StoreLayoutModeController layoutMode;

    [SerializeField] private FurniturePlacementController placementController;

    [Header("Existing Furniture Access")]
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.04f;

    [SerializeField] private Color availableColor = new Color32(240, 201, 106, 48);
    [SerializeField] private Color blockedColor = new Color32(196, 107, 107, 150);

    [Header("Staff Access")]
    [SerializeField] private Color staffAvailableColor = new Color32(122, 186, 225, 52);

    private sealed class AccessVisual
    {
        public FurnitureIdentity Furniture;
        public GameObject VisualObject;
        public MeshRenderer Renderer;
        public bool IsStaff;
    }

    private readonly List<AccessVisual> accessVisuals = new();

    private GameObject visualRoot;
    private Mesh unitQuad;
    private Material availableMaterial;
    private Material blockedMaterial;
    private Material staffAvailableMaterial;

    private bool wasLayoutActive;
    private FurnitureIdentity previousSelection;
    
    private void Awake()
    {
        visualRoot = new GameObject("FurnitureAccessVisuals_Runtime");
        unitQuad = LayoutVisualUtility.CreateUnitQuad();

        availableMaterial = LayoutVisualUtility.CreateTransparentMaterial(
            "ExistingFurnitureAccess_Runtime", availableColor);

        blockedMaterial = LayoutVisualUtility.CreateTransparentMaterial("BlockedFurnitureAccess_Runtime", blockedColor);

        staffAvailableMaterial = LayoutVisualUtility.CreateTransparentMaterial(
            "ExistingFurnitureStaffAccess_Runtime", staffAvailableColor);

        visualRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        bool layoutActive = layoutMode != null && layoutMode.IsActive;

        if (!layoutActive)
        {
            if (wasLayoutActive && visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            wasLayoutActive = false;
            previousSelection = null;
            return;
        }

        FurnitureIdentity selection = placementController != null ? placementController.SelectedFurniture : null;

        // Rebuild on layout entry or selection changes.
        // This also handles newly confirmed furniture purchases.
        if (!wasLayoutActive || selection != previousSelection)
        {
            RebuildAccessVisuals(selection);
            previousSelection = selection;
        }

        wasLayoutActive = true;

        if (visualRoot == null)
        {
            return;
        }

        if (!visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        UpdateAccessVisuals();
    }


    private void RebuildAccessVisuals(FurnitureIdentity selected)
    {
        ClearAccessVisuals();

        if (visualRoot == null) return;

        FurnitureIdentity[] furniture = FindObjectsByType<FurnitureIdentity>(FindObjectsSortMode.None);

        foreach (FurnitureIdentity candidate in furniture)
        {
            if (candidate == null || candidate == selected || !candidate.gameObject.activeInHierarchy ||
                candidate.FurnitureDefinition == null)
            {
                continue;
            }

            FurnitureDefinition definition = candidate.FurnitureDefinition;

            if (definition.RequiresOperationalAccess)
            {
                AddAccessVisual(candidate, false);
            }

            if (definition.RequiresStaffAccess)
            {
                AddAccessVisual(candidate, true);
            }
        }
    }

    private void AddAccessVisual(FurnitureIdentity furniture, bool isStaff)
    {
        GameObject visual = new(isStaff ? $"StaffAccess_{furniture.name}" : $"Access_{furniture.name}");

        visual.transform.SetParent(visualRoot.transform, false);

        MeshFilter filter = visual.AddComponent<MeshFilter>();
        filter.sharedMesh = unitQuad;

        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = isStaff ? staffAvailableMaterial : availableMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = isStaff ? 3 : 2;

        accessVisuals.Add(new AccessVisual
        {
            Furniture = furniture, VisualObject = visual, Renderer = renderer, IsStaff = isStaff
        });
    }


    private void UpdateAccessVisuals()
    {
        Vector3 previewCenter = Vector3.zero;
        Quaternion previewRotation = Quaternion.identity;
        Vector2 previewHalfExtents = Vector2.zero;
        StoreAreaType previewArea = default;

        bool hasPreview = placementController != null &&
                          placementController.TryGetSelectedBodyFootprint(out previewCenter, out previewRotation,
                              out previewHalfExtents, out previewArea);

        foreach (AccessVisual entry in accessVisuals)
        {
            if (entry.Furniture == null || !entry.Furniture.gameObject.activeInHierarchy ||
                entry.Furniture.FurnitureDefinition == null)
            {
                if (entry.VisualObject != null) entry.VisualObject.SetActive(false);
                continue;
            }

            FurnitureDefinition definition = entry.Furniture.FurnitureDefinition;

            bool showAccess = entry.IsStaff ? definition.RequiresStaffAccess : definition.RequiresOperationalAccess;

            if (!showAccess)
            {
                if (entry.VisualObject != null) entry.VisualObject.SetActive(false);
                continue;
            }

            if (!entry.VisualObject.activeSelf)
            {
                entry.VisualObject.SetActive(true);
            }

            Vector3 localCenter = entry.IsStaff ? definition.StaffAccessCenter : definition.AccessCenter;

            Vector3 localHalfExtents = entry.IsStaff ? definition.StaffAccessHalfExtents : definition.AccessHalfExtents;

            Transform furniture = entry.Furniture.transform;
            Vector3 scale = furniture.lossyScale;

            Vector3 worldAccessCenter = furniture.TransformPoint(localCenter);

            Vector2 accessHalfExtents = new(Mathf.Abs(localHalfExtents.x * scale.x),
                Mathf.Abs(localHalfExtents.z * scale.z));

            Vector3 surfaceCenter = furniture.TransformPoint(new Vector3(localCenter.x, 0f, localCenter.z));

            surfaceCenter.y = furniture.position.y + surfaceOffset;

            entry.VisualObject.transform.SetPositionAndRotation(surfaceCenter,
                Quaternion.Euler(0f, furniture.eulerAngles.y, 0f));

            entry.VisualObject.transform.localScale = new Vector3(
                accessHalfExtents.x * 2f, 1f, accessHalfExtents.y * 2f);

            bool blocked = hasPreview && definition.PlacementArea == previewArea &&
                           LayoutFootprintUtility.OverlapsHorizontal(previewCenter, previewRotation, previewHalfExtents,
                               worldAccessCenter, furniture.rotation, accessHalfExtents);

            Material desiredMaterial =
                blocked ? blockedMaterial : entry.IsStaff ? staffAvailableMaterial : availableMaterial;

            if (entry.Renderer.sharedMaterial != desiredMaterial)
            {
                entry.Renderer.sharedMaterial = desiredMaterial;
            }
        }
    }


    private void ClearAccessVisuals()
    {
        foreach (AccessVisual entry in accessVisuals)
        {
            if (entry.VisualObject != null)
            {
                entry.VisualObject.SetActive(false);
                Destroy(entry.VisualObject);
            }
        }

        accessVisuals.Clear();
    }

    private void OnDestroy()
    {
        ClearAccessVisuals();

        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }

        if (unitQuad != null)
        {
            Destroy(unitQuad);
        }

        if (availableMaterial != null)
        {
            Destroy(availableMaterial);
        }

        if (blockedMaterial != null)
        {
            Destroy(blockedMaterial);
        }
        
        if (staffAvailableMaterial != null)
        {
            Destroy(staffAvailableMaterial);
        }
    }
}
