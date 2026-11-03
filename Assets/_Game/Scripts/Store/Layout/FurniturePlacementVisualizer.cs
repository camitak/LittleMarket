using UnityEngine;

[DisallowMultipleComponent]
public class FurniturePlacementVisualizer : MonoBehaviour
{
    [Header("Position")] 
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.045f;

    [Header("Body")] 
    [SerializeField] private Color validBodyColor = new Color32(111, 207, 151, 102);
    [SerializeField] private Color invalidBodyColor = new Color32(196, 107, 107, 128);

    [Header("Front Access")] 
    [SerializeField] private Color validAccessColor = new Color32(240, 201, 106, 102);
    [SerializeField] private Color invalidAccessColor = new Color32(196, 107, 107, 112);

    private GameObject visualRoot;
    private GameObject bodyVisual;
    private GameObject accessVisual;

    private Mesh unitQuad;

    private Material bodyMaterial;
    private Material accessMaterial;

    private void Awake()
    {
        CreateRuntimeObjects();

        Hide();
    }

    public void UpdateFootprints(Transform furniture,
        Vector3 bodyCenter,
        Vector3 bodyHalfExtents,
        Vector3 accessCenter,
        Vector3 accessHalfExtents,
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

        ApplyFootprint(accessVisual, furniture, accessCenter, accessHalfExtents, surfaceOffset + 0.005f);

        LayoutVisualUtility.SetMaterialColor(bodyMaterial, canPlace ? validBodyColor : invalidBodyColor);

        LayoutVisualUtility.SetMaterialColor(accessMaterial, canPlace ? validAccessColor : invalidAccessColor);
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

        bodyVisual = CreateFootprintObject("BodyFootprint", bodyMaterial, 10);

        accessVisual = CreateFootprintObject("AccessFootprint", accessMaterial, 11);
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

    private static void ApplyFootprint(GameObject visual,
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
    }
}