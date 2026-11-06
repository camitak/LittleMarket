using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StoreExpansionWorldVisualizer : MonoBehaviour
{
    private enum ExpansionVisualStyle
    {
        Neutral,
        AvailableFocus,
        UnavailableFocus,
        LevelLocked
    }

    private sealed class ExpansionVisual
    {
        public StoreExpansionZone Zone;
        public GameObject Root;
        public MeshRenderer FillRenderer;
        public LineRenderer Border;
    }

    [Header("References")]
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private StoreExpansionSystem expansionSystem;

    [Header("Position")]
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.03f;
    [Min(0f)] [SerializeField] private float borderSurfaceOffset = 0.045f;
    [Min(0.005f)] [SerializeField] private float borderWidth = 0.035f;

    [Header("Locked")]
    [SerializeField] private Color lockedFillColor = new Color32(98, 93, 112, 64);
    [SerializeField] private Color lockedBorderColor = new Color32(185, 175, 206, 153);

    [Header("Purchasable Focus")]
    [SerializeField] private Color availableFillColor = new Color32(111, 207, 151, 56);
    [SerializeField] private Color availableBorderColor = new Color32(169, 230, 213, 230);

    [Header("Unavailable Focus")]
    [SerializeField] private Color unavailableFocusFillColor = new Color32(217, 165, 122, 56);
    [SerializeField] private Color unavailableFocusBorderColor = new Color32(230, 185, 143, 230);

    [Header("Store Level Locked")]
    [SerializeField] private Color levelLockedFillColor = new Color32(107, 101, 122, 51);
    [SerializeField] private Color levelLockedBorderColor = new Color32(138, 127, 150, 166);

    private readonly List<ExpansionVisual> visuals = new();

    private GameObject visualRoot;
    private Mesh unitQuad;

    private Material lockedFillMaterial;
    private Material lockedBorderMaterial;
    private Material availableFillMaterial;
    private Material availableBorderMaterial;
    private Material unavailableFocusFillMaterial;
    private Material unavailableFocusBorderMaterial;
    private Material levelLockedFillMaterial;
    private Material levelLockedBorderMaterial;

    private StoreExpansionZone selectedZone;
    private StoreExpansionZone hoveredZone;

    private void Awake()
    {
        CreateRuntimeResources();
        RebuildVisuals();
        SetVisualsActive(false);
    }

    private void LateUpdate()
    {
        bool active = layoutMode != null && layoutMode.IsActive;

        SetVisualsActive(active);

        if (!active)
        {
            return;
        }

        UpdateVisuals();
    }

    public void SetInteractionState(StoreExpansionZone selected, StoreExpansionZone hovered)
    {
        selectedZone = selected;
        hoveredZone = hovered;

        if (layoutMode != null && layoutMode.IsActive)
        {
            UpdateVisuals();
        }
    }

    public void RefreshVisuals()
    {
        RebuildVisuals();

        if (layoutMode != null && layoutMode.IsActive)
        {
            SetVisualsActive(true);
            UpdateVisuals();
        }
    }

    public bool TryGetZoneAtRay(Ray ray, out StoreExpansionZone zone)
    {
        zone = null;

        if (expansionSystem == null)
        {
            return false;
        }

        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < expansionSystem.ExpansionCount; i++)
        {
            StoreExpansionZone candidate = expansionSystem.GetExpansion(i);

            if (candidate == null ||
                candidate.IsUnlocked ||
                !candidate.TryGetPreviewBounds(out Bounds bounds))
            {
                continue;
            }

            float planeY = bounds.max.y + surfaceOffset;
            Plane plane = new(Vector3.up, new Vector3(0f, planeY, 0f));

            if (!plane.Raycast(ray, out float distance))
            {
                continue;
            }

            if (distance < 0f || distance >= bestDistance)
            {
                continue;
            }

            Vector3 point = ray.GetPoint(distance);

            if (point.x < bounds.min.x ||
                point.x > bounds.max.x ||
                point.z < bounds.min.z ||
                point.z > bounds.max.z)
            {
                continue;
            }

            bestDistance = distance;
            zone = candidate;
        }

        return zone != null;
    }

    private void CreateRuntimeResources()
    {
        visualRoot = new GameObject("ExpansionVisuals_Runtime");
        unitQuad = LayoutVisualUtility.CreateUnitQuad();

        lockedFillMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("ExpansionLockedFill_Runtime", lockedFillColor);

        lockedBorderMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("ExpansionLockedBorder_Runtime", lockedBorderColor);

        availableFillMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("ExpansionAvailableFill_Runtime", availableFillColor);

        availableBorderMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("ExpansionAvailableBorder_Runtime", availableBorderColor);

        unavailableFocusFillMaterial =
            LayoutVisualUtility.CreateTransparentMaterial(
                "ExpansionUnavailableFocusFill_Runtime",
                unavailableFocusFillColor
            );

        unavailableFocusBorderMaterial =
            LayoutVisualUtility.CreateTransparentMaterial(
                "ExpansionUnavailableFocusBorder_Runtime",
                unavailableFocusBorderColor
            );

        levelLockedFillMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("ExpansionLevelLockedFill_Runtime", levelLockedFillColor);

        levelLockedBorderMaterial =
            LayoutVisualUtility.CreateTransparentMaterial(
                "ExpansionLevelLockedBorder_Runtime",
                levelLockedBorderColor
            );
    }

    private void RebuildVisuals()
    {
        ClearVisuals();

        if (visualRoot == null || expansionSystem == null)
        {
            return;
        }

        for (int i = 0; i < expansionSystem.ExpansionCount; i++)
        {
            StoreExpansionZone zone = expansionSystem.GetExpansion(i);

            if (zone == null || !zone.IsConfigured)
            {
                continue;
            }

            visuals.Add(CreateVisual(zone));
        }
    }

    private ExpansionVisual CreateVisual(StoreExpansionZone zone)
    {
        ExpansionVisual entry = new();
        entry.Zone = zone;

        entry.Root = new GameObject($"Expansion_{zone.name}");
        entry.Root.transform.SetParent(visualRoot.transform, true);

        GameObject fill = new("Fill");
        fill.transform.SetParent(entry.Root.transform, true);

        MeshFilter filter = fill.AddComponent<MeshFilter>();
        filter.sharedMesh = unitQuad;

        entry.FillRenderer = fill.AddComponent<MeshRenderer>();
        entry.FillRenderer.sharedMaterial = lockedFillMaterial;
        entry.FillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        entry.FillRenderer.receiveShadows = false;
        entry.FillRenderer.sortingOrder = -4;

        GameObject borderObject = new("Border");
        borderObject.transform.SetParent(entry.Root.transform, true);

        entry.Border = borderObject.AddComponent<LineRenderer>();
        entry.Border.useWorldSpace = true;
        entry.Border.positionCount = 5;
        entry.Border.loop = false;
        entry.Border.widthMultiplier = borderWidth;
        entry.Border.numCornerVertices = 2;
        entry.Border.numCapVertices = 2;
        entry.Border.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        entry.Border.receiveShadows = false;
        entry.Border.sortingOrder = 4;
        entry.Border.sharedMaterial = lockedBorderMaterial;

        return entry;
    }

    private void UpdateVisuals()
    {
        foreach (ExpansionVisual entry in visuals)
        {
            if (entry == null || entry.Root == null || entry.Zone == null)
            {
                continue;
            }

            if (entry.Zone.IsUnlocked ||
                !entry.Zone.TryGetPreviewBounds(out Bounds bounds))
            {
                entry.Root.SetActive(false);
                continue;
            }

            if (!entry.Root.activeSelf)
            {
                entry.Root.SetActive(true);
            }

            UpdateGeometry(entry, bounds);
            ApplyStyle(entry, GetStyle(entry.Zone));
        }
    }

    private void UpdateGeometry(ExpansionVisual entry, Bounds bounds)
    {
        Transform fillTransform = entry.FillRenderer.transform;

        fillTransform.SetPositionAndRotation(
            new Vector3(bounds.center.x, bounds.max.y + surfaceOffset, bounds.center.z),
            Quaternion.identity
        );

        fillTransform.localScale = new Vector3(bounds.size.x, 1f, bounds.size.z);

        float y = bounds.max.y + borderSurfaceOffset;

        entry.Border.SetPosition(0, new Vector3(bounds.min.x, y, bounds.min.z));
        entry.Border.SetPosition(1, new Vector3(bounds.max.x, y, bounds.min.z));
        entry.Border.SetPosition(2, new Vector3(bounds.max.x, y, bounds.max.z));
        entry.Border.SetPosition(3, new Vector3(bounds.min.x, y, bounds.max.z));
        entry.Border.SetPosition(4, new Vector3(bounds.min.x, y, bounds.min.z));
    }

    private ExpansionVisualStyle GetStyle(StoreExpansionZone zone)
    {
        if (expansionSystem == null || zone == null)
        {
            return ExpansionVisualStyle.Neutral;
        }

        bool focused = zone == selectedZone || zone == hoveredZone;

        if (focused)
        {
            return expansionSystem.CanPurchase(zone, out _)
                ? ExpansionVisualStyle.AvailableFocus
                : ExpansionVisualStyle.UnavailableFocus;
        }

        if (!expansionSystem.MeetsLevelRequirement(zone))
        {
            return ExpansionVisualStyle.LevelLocked;
        }

        return ExpansionVisualStyle.Neutral;
    }

    private void ApplyStyle(ExpansionVisual entry, ExpansionVisualStyle style)
    {
        switch (style)
        {
            case ExpansionVisualStyle.AvailableFocus:
                entry.FillRenderer.sharedMaterial = availableFillMaterial;
                entry.Border.sharedMaterial = availableBorderMaterial;
                break;

            case ExpansionVisualStyle.UnavailableFocus:
                entry.FillRenderer.sharedMaterial = unavailableFocusFillMaterial;
                entry.Border.sharedMaterial = unavailableFocusBorderMaterial;
                break;

            case ExpansionVisualStyle.LevelLocked:
                entry.FillRenderer.sharedMaterial = levelLockedFillMaterial;
                entry.Border.sharedMaterial = levelLockedBorderMaterial;
                break;

            default:
                entry.FillRenderer.sharedMaterial = lockedFillMaterial;
                entry.Border.sharedMaterial = lockedBorderMaterial;
                break;
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(active);
        }
    }

    private void ClearVisuals()
    {
        foreach (ExpansionVisual entry in visuals)
        {
            if (entry?.Root == null)
            {
                continue;
            }

            entry.Root.SetActive(false);
            Destroy(entry.Root);
        }

        visuals.Clear();
    }

    private void OnDestroy()
    {
        ClearVisuals();

        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }

        if (unitQuad != null)
        {
            Destroy(unitQuad);
        }

        DestroyMaterial(lockedFillMaterial);
        DestroyMaterial(lockedBorderMaterial);
        DestroyMaterial(availableFillMaterial);
        DestroyMaterial(availableBorderMaterial);
        DestroyMaterial(unavailableFocusFillMaterial);
        DestroyMaterial(unavailableFocusBorderMaterial);
        DestroyMaterial(levelLockedFillMaterial);
        DestroyMaterial(levelLockedBorderMaterial);
    }

    private static void DestroyMaterial(Material material)
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}