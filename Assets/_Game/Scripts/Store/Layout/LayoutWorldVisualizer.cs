using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LayoutWorldVisualizer : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private StoreBuildArea buildArea;

    [Header("Grid")] 
    [Min(0.1f)] [SerializeField] private float visibleGridSize = 0.5f;
    [Min(0.001f)] [SerializeField] private float gridLineWidth = 0.015f;
    [Min(0f)] [SerializeField] private float gridSurfaceOffset = 0.025f;
    [SerializeField] private Color gridColor = new Color32(207, 247, 250, 38);

    [Header("Operational Clearance")] 
    [Min(0f)] [SerializeField] private float clearanceSurfaceOffset = 0.035f;
    [SerializeField] private Color clearanceColor = new Color32(151, 126, 255, 66);

    private GameObject visualRoot;

    private MeshFilter gridFilter;
    private MeshRenderer gridRenderer;

    private Mesh gridMesh;
    private Mesh unitQuad;

    private Material gridMaterial;
    private Material clearanceMaterial;

    private readonly List<GameObject> clearanceVisuals = new();

    private readonly List<Bounds> availableFloorBounds = new();

    private bool previousLayoutState;

    private void Awake()
    {
        CreateRuntimeObjects();

        SetVisualsActive(false);
    }

    private void Update()
    {
        bool layoutActive = layoutMode != null && layoutMode.IsActive;

        if (layoutActive == previousLayoutState)
        {
            return;
        }

        previousLayoutState = layoutActive;

        if (layoutActive)
        {
            RebuildVisuals();
        }

        SetVisualsActive(layoutActive);
    }
    
    public void RefreshVisuals()
    {
        if (layoutMode == null ||
            !layoutMode.IsActive)
        {
            return;
        }

        RebuildVisuals();

        SetVisualsActive(
            true
        );
    }

    private void CreateRuntimeObjects()
    {
        visualRoot = new GameObject("LayoutWorldVisuals_Runtime");

        GameObject gridObject = new("FloorGrid");

        gridObject.transform.SetParent(visualRoot.transform, false);

        gridFilter = gridObject.AddComponent<MeshFilter>();

        gridRenderer = gridObject.AddComponent<MeshRenderer>();

        gridMaterial = LayoutVisualUtility.CreateTransparentMaterial("LayoutGrid_Runtime", gridColor);

        clearanceMaterial = LayoutVisualUtility.CreateTransparentMaterial("LayoutClearance_Runtime", clearanceColor);

        unitQuad = LayoutVisualUtility.CreateUnitQuad();

        gridRenderer.sharedMaterial = gridMaterial;

        gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        gridRenderer.receiveShadows = false;

        gridRenderer.sortingOrder = -10;
    }

    private void RebuildVisuals()
    {
        RebuildGrid();
        RebuildClearanceZones();
    }

    private void RebuildGrid()
    {
        if (buildArea == null || gridFilter == null)
        {
            return;
        }

        availableFloorBounds.Clear();

        buildArea.CollectAvailableFloorBounds(StoreAreaType.SalesFloor, availableFloorBounds);

        buildArea.CollectAvailableFloorBounds(StoreAreaType.Backroom, availableFloorBounds);

        if (gridMesh != null)
        {
            Destroy(gridMesh);
        }

        gridMesh = LayoutVisualUtility.CreateGridMesh(availableFloorBounds, visibleGridSize, gridLineWidth,
            gridSurfaceOffset);

        gridFilter.sharedMesh = gridMesh;

        LayoutVisualUtility.SetMaterialColor(gridMaterial, gridColor);
    }

    private void RebuildClearanceZones()
    {
        ClearClearanceVisuals();

        LayoutClearanceZone[] zones = FindObjectsByType<LayoutClearanceZone>(FindObjectsSortMode.None);

        foreach (LayoutClearanceZone zone in zones)
        {
            if (zone == null || !zone.gameObject.activeInHierarchy)
            {
                continue;
            }

            CreateClearanceVisual(zone);
        }

        LayoutVisualUtility.SetMaterialColor(clearanceMaterial, clearanceColor);
    }

    private void CreateClearanceVisual(LayoutClearanceZone zone)
    {
        GameObject visual = new($"Clearance_{zone.name}");

        visual.transform.SetParent(visualRoot.transform, true);

        Vector3 localCenter = zone.Center;

        localCenter.y = 0f;

        Vector3 worldPosition = zone.transform.TransformPoint(localCenter);

        worldPosition.y = zone.transform.position.y + clearanceSurfaceOffset;

        visual.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, zone.transform.eulerAngles.y, 0f));

        Vector3 zoneScale = zone.transform.lossyScale;

        visual.transform.localScale = new Vector3(zone.HalfExtents.x * 2f * Mathf.Abs(zoneScale.x), 1f,
            zone.HalfExtents.z * 2f * Mathf.Abs(zoneScale.z));

        MeshFilter filter = visual.AddComponent<MeshFilter>();

        filter.sharedMesh = unitQuad;

        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = clearanceMaterial;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        renderer.receiveShadows = false;

        renderer.sortingOrder = 0;

        clearanceVisuals.Add(visual);
    }

    private void ClearClearanceVisuals()
    {
        foreach (GameObject visual in clearanceVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }

        clearanceVisuals.Clear();
    }

    private void SetVisualsActive(bool active)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        ClearClearanceVisuals();

        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }

        if (gridMesh != null)
        {
            Destroy(gridMesh);
        }

        if (unitQuad != null)
        {
            Destroy(unitQuad);
        }

        if (gridMaterial != null)
        {
            Destroy(gridMaterial);
        }

        if (clearanceMaterial != null)
        {
            Destroy(clearanceMaterial);
        }
    }
}