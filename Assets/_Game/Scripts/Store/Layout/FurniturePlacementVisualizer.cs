using UnityEngine;

[DisallowMultipleComponent] public class FurniturePlacementVisualizer : MonoBehaviour
{
    [Header("Position")]
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.045f;

    [Header("Body")]
    [SerializeField] private Color validBodyColor = new Color32(111, 207, 151, 102);
    [SerializeField] private Color invalidBodyColor = new Color32(196, 107, 107, 128);

    [Header("Front Access")]
    [SerializeField] private Color validAccessColor = new Color32(240, 201, 106, 102);
    [SerializeField] private Color invalidAccessColor = new Color32(196, 107, 107, 112);

    [Header("Staff Access")]
    [SerializeField] private Color validStaffAccessColor = new Color32(122, 186, 225, 112);
    [SerializeField] private Color invalidStaffAccessColor = new Color32(196, 107, 107, 112);

    [Header("Magnetic Snap Guide")]
    [Min(0.05f)] [SerializeField] private float snapGuideSize = 0.18f;
    [Min(0.005f)] [SerializeField] private float snapGuideWidth = 0.035f;
    [Min(0f)] [SerializeField] private float snapGuideSurfaceOffset = 0.085f;
    [SerializeField] private Color snapGuideColor = new Color32(95, 227, 240, 230);
    [SerializeField] private Color invalidSnapGuideColor = new Color32(196, 107, 107, 230);

    private GameObject visualRoot;
    private GameObject bodyVisual;
    private GameObject accessVisual;
    private GameObject staffAccessVisual;
    private GameObject snapGuideObject;

    private Mesh unitQuad;

    private Material bodyMaterial;
    private Material accessMaterial;
    private Material staffAccessMaterial;
    private Material snapGuideMaterial;

    private LineRenderer snapGuide;

    private void Awake()
    {
        CreateRuntimeObjects();

        Hide();
    }

    public void UpdateFootprints(
        Transform furniture,
        Vector3 bodyCenter,
        Vector3 bodyHalfExtents,
        Vector3 accessCenter,
        Vector3 accessHalfExtents,
        bool showAccess,
        bool canPlace)
    {
        if (furniture == null)
        {
            Hide();
            return;
        }

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        ApplyFootprint(bodyVisual, furniture, bodyCenter, bodyHalfExtents, surfaceOffset);

        LayoutVisualUtility.SetMaterialColor(bodyMaterial, canPlace ? validBodyColor : invalidBodyColor);

        if (accessVisual == null)
        {
            return;
        }

        accessVisual.SetActive(showAccess);

        if (!showAccess)
        {
            return;
        }

        ApplyFootprint(accessVisual, furniture, accessCenter, accessHalfExtents, surfaceOffset + 0.005f);

        LayoutVisualUtility.SetMaterialColor(accessMaterial, canPlace ? validAccessColor : invalidAccessColor);
    }

    public void UpdateStaffFootprint(
        Transform furniture,
        Vector3 center,
        Vector3 halfExtents,
        bool showAccess,
        bool canPlace)
    {
        if (staffAccessVisual == null) return;

        staffAccessVisual.SetActive(showAccess);

        if (!showAccess || furniture == null) return;

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        ApplyFootprint(staffAccessVisual, furniture, center, halfExtents, surfaceOffset + 0.01f);

        LayoutVisualUtility.SetMaterialColor(staffAccessMaterial,
            canPlace ? validStaffAccessColor : invalidStaffAccessColor);
    }

    public void UpdateSnapGuide(Vector3 worldPosition, bool active, bool canPlace)
    {
        if (snapGuideObject == null || snapGuide == null)
        {
            return;
        }

        if (!active)
        {
            snapGuideObject.SetActive(false);

            return;
        }

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        snapGuideObject.SetActive(true);

        Vector3 center = worldPosition;

        center.y += snapGuideSurfaceOffset;

        float size = Mathf.Max(0.05f, snapGuideSize);

        snapGuide.SetPosition(0, center + new Vector3(0f, 0f, -size));

        snapGuide.SetPosition(1, center + new Vector3(size, 0f, 0f));

        snapGuide.SetPosition(2, center + new Vector3(0f, 0f, size));

        snapGuide.SetPosition(3, center + new Vector3(-size, 0f, 0f));

        snapGuide.SetPosition(4, center + new Vector3(0f, 0f, -size));

        Color color = canPlace ? snapGuideColor : invalidSnapGuideColor;

        snapGuide.startColor = color;

        snapGuide.endColor = color;

        LayoutVisualUtility.SetMaterialColor(snapGuideMaterial, color);
    }

    public void Hide()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void CreateRuntimeObjects()
    {
        visualRoot = new GameObject("FurniturePlacementVisuals_Runtime");

        unitQuad = LayoutVisualUtility.CreateUnitQuad();

        bodyMaterial = LayoutVisualUtility.CreateTransparentMaterial("FurnitureBodyFootprint_Runtime", validBodyColor);

        accessMaterial =
            LayoutVisualUtility.CreateTransparentMaterial("FurnitureAccessFootprint_Runtime", validAccessColor);

        staffAccessMaterial = LayoutVisualUtility.CreateTransparentMaterial(
            "FurnitureStaffAccessFootprint_Runtime", validStaffAccessColor);

        snapGuideMaterial = LayoutVisualUtility.CreateTransparentMaterial("FurnitureSnapGuide_Runtime", snapGuideColor);

        bodyVisual = CreateFootprintObject("BodyFootprint", bodyMaterial, 10);

        accessVisual = CreateFootprintObject("AccessFootprint", accessMaterial, 11);

        staffAccessVisual = CreateFootprintObject("StaffAccessFootprint", staffAccessMaterial, 12);
        staffAccessVisual.SetActive(false);

        CreateSnapGuide();
    }

    private GameObject CreateFootprintObject(string objectName, Material material, int sortingOrder)
    {
        GameObject visual = new(objectName);

        visual.transform.SetParent(visualRoot.transform, true);

        MeshFilter filter = visual.AddComponent<MeshFilter>();

        filter.sharedMesh = unitQuad;

        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = material;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        renderer.receiveShadows = false;

        renderer.sortingOrder = sortingOrder;

        return visual;
    }

    private void CreateSnapGuide()
    {
        snapGuideObject = new GameObject("MagneticSnapGuide");

        snapGuideObject.transform.SetParent(visualRoot.transform, true);

        snapGuide = snapGuideObject.AddComponent<LineRenderer>();

        snapGuide.useWorldSpace = true;

        snapGuide.positionCount = 5;

        snapGuide.loop = false;

        snapGuide.widthMultiplier = snapGuideWidth;

        snapGuide.numCornerVertices = 2;

        snapGuide.numCapVertices = 2;

        snapGuide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        snapGuide.receiveShadows = false;

        snapGuide.sortingOrder = 20;

        snapGuide.sharedMaterial = snapGuideMaterial;

        snapGuideObject.SetActive(false);
    }

    private static void ApplyFootprint(
        GameObject visual,
        Transform furniture,
        Vector3 center,
        Vector3 halfExtents,
        float yOffset)
    {
        if (visual == null || furniture == null)
        {
            return;
        }

        Vector3 horizontalCenter = new(center.x, 0f, center.z);

        Vector3 worldPosition = furniture.position + furniture.rotation * horizontalCenter;

        worldPosition.y = furniture.position.y + yOffset;

        visual.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, furniture.eulerAngles.y, 0f));

        visual.transform.localScale = new Vector3(Mathf.Abs(halfExtents.x) * 2f, 1f, Mathf.Abs(halfExtents.z) * 2f);
    }

    private void OnDestroy()
    {
        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }

        if (unitQuad != null)
        {
            Destroy(unitQuad);
        }

        if (bodyMaterial != null)
        {
            Destroy(bodyMaterial);
        }

        if (accessMaterial != null)
        {
            Destroy(accessMaterial);
        }

        if (staffAccessMaterial != null)
        {
            Destroy(staffAccessMaterial);
        }

        if (snapGuideMaterial != null)
        {
            Destroy(snapGuideMaterial);
        }
    }
}
