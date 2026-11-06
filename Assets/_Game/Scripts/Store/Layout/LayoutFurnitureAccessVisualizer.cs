using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LayoutFurnitureAccessVisualizer : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private FurniturePlacementController placementController;

    [Header("Existing Furniture Access")] 
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.04f;
    [SerializeField] private Color availableColor = new Color32(240, 201, 106, 48);
    [SerializeField] private Color blockedColor = new Color32(196, 107, 107, 150);

    private sealed class AccessVisual
    {
        public FurnitureIdentity Furniture;
        public GameObject VisualObject;
        public MeshRenderer Renderer;
    }

    private readonly List<AccessVisual> accessVisuals = new();

    private GameObject visualRoot;
    private Mesh unitQuad;
    private Material availableMaterial;
    private Material blockedMaterial;

    private bool wasLayoutActive;
    private FurnitureIdentity previousSelection;

    private void Awake()
    {
        visualRoot = new GameObject("FurnitureAccessVisuals_Runtime");
        unitQuad = LayoutVisualUtility.CreateUnitQuad();

        availableMaterial = LayoutVisualUtility.CreateTransparentMaterial(
            "ExistingFurnitureAccess_Runtime", availableColor);

        blockedMaterial = LayoutVisualUtility.CreateTransparentMaterial("BlockedFurnitureAccess_Runtime", blockedColor);

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

        if (visualRoot == null)
        {
            return;
        }

        FurnitureIdentity[] furniture = FindObjectsByType<FurnitureIdentity>(FindObjectsSortMode.None);

        foreach (FurnitureIdentity candidate in furniture)
        {
            if (candidate == null ||
                candidate == selected ||
                !candidate.gameObject.activeInHierarchy ||
                candidate.FurnitureDefinition == null ||
                !candidate.FurnitureDefinition.RequiresOperationalAccess)
            {
                continue;
            }

            GameObject visual = new($"Access_{candidate.name}");
            visual.transform.SetParent(visualRoot.transform, false);

            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = unitQuad;

            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = availableMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 2;

            accessVisuals.Add(new AccessVisual { Furniture = candidate, VisualObject = visual, Renderer = renderer });
        }
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
                if (entry.VisualObject != null)
                {
                    entry.VisualObject.SetActive(false);
                }

                continue;
            }

            FurnitureDefinition definition = entry.Furniture.FurnitureDefinition;
            
            if (!definition.RequiresOperationalAccess)
            {
                if (entry.VisualObject != null)
                {
                    entry.VisualObject.SetActive(
                        false
                    );
                }

                continue;
            }
            
            Transform furniture = entry.Furniture.transform;

            Vector3 worldAccessCenter = furniture.TransformPoint(definition.AccessCenter);
            Vector3 scale = furniture.lossyScale;

            Vector2 accessHalfExtents = new(Mathf.Abs(definition.AccessHalfExtents.x * scale.x),
                Mathf.Abs(definition.AccessHalfExtents.z * scale.z));

            Vector3 surfaceCenter = furniture.TransformPoint(
                new Vector3(definition.AccessCenter.x, 0f, definition.AccessCenter.z));

            surfaceCenter.y = furniture.position.y + surfaceOffset;

            entry.VisualObject.transform.SetPositionAndRotation(surfaceCenter,
                Quaternion.Euler(0f, furniture.eulerAngles.y, 0f));

            entry.VisualObject.transform.localScale = new Vector3(
                accessHalfExtents.x * 2f, 1f, accessHalfExtents.y * 2f);

            bool blocked = hasPreview && definition.PlacementArea == previewArea &&
                           LayoutFootprintUtility.OverlapsHorizontal(previewCenter, previewRotation, previewHalfExtents,
                               worldAccessCenter, furniture.rotation, accessHalfExtents);

            Material desiredMaterial = blocked ? blockedMaterial : availableMaterial;

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
    }
}