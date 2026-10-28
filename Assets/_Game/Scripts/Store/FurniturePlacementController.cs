using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

// Lesson 100: two-way rotation and required clear access in front of each shelf.
// Persistence is intentionally postponed until these rules are reliable.
[DisallowMultipleComponent]
public class FurniturePlacementController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InteractionUI interactionUI;

    [Header("Store")]
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private Collider storeFloor;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Placement")]
    [Min(1f)] [SerializeField] private float grabDistance = 4f;
    [Min(1f)] [SerializeField] private float previewRayDistance = 18f;
    [Min(0.1f)] [SerializeField] private float gridSize = 0.5f;
    [SerializeField] private Vector3 footprintHalfExtents = new(1.30f, 1.05f, 0.52f);
    [SerializeField] private Vector3 footprintCenter = new(0f, 1.10f, 0f);

    [Header("Front Access Footprint - Shelf Front Is Local -Z")]
    [SerializeField] private Vector3 accessHalfExtents = new(1.25f, 0.75f, 0.50f);
    [SerializeField] private Vector3 accessCenter = new(0f, 0.80f, -1.02f);
    [Min(0.01f)] [SerializeField] private float outlineWidth = 0.05f;

    [Header("Preview Colours")]
    [SerializeField] private Color validColor = new Color32(111, 207, 151, 255);
    [SerializeField] private Color invalidColor = new Color32(196, 107, 107, 255);

    private ShelfIdentity selectedShelf;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float previewYaw;
    private bool canPlace;
    private bool hasUnpersistedFurnitureChanges;

    private Collider[] selectedColliders;
    private bool[] originallyEnabled;
    private MeshRenderer[] selectedRenderers;
    private MaterialPropertyBlock[] originalProperties;
    private LineRenderer accessOutline;
    private Material outlineMaterial;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // SaveManager reads this until furniture positions have a save format.
    public bool HasUnpersistedFurnitureChanges => hasUnpersistedFurnitureChanges;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (selectedShelf == null)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame && CanStartPlacement())
                TryStartPlacement();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked ||
            Time.timeScale <= 0f || storeClock == null ||
            storeClock.IsStoreOpen ||
            customerFlow == null || customerFlow.ActiveCustomerCount > 0)
        {
            CancelPlacement();
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            CancelPlacement();
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
            previewYaw = Mathf.Repeat(previewYaw + 90f, 360f);

        if (Keyboard.current.tKey.wasPressedThisFrame)
            previewYaw = Mathf.Repeat(previewYaw - 90f, 360f);

        UpdatePreview();

        if (Keyboard.current.eKey.wasPressedThisFrame && canPlace)
            ConfirmPlacement();
    }

    private void LateUpdate()
    {
        if (interactionUI == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (selectedShelf != null)
        {
            interactionUI.ShowPrompt(canPlace
                ? "[E] Place  |  [R] Right  [T] Left  |  [Q] Cancel"
                : "Keep the front clear  |  [R] Right  [T] Left  |  [Q] Cancel");
            return;
        }

        if (CanStartPlacement() && TryFindLookedAtShelf(out ShelfIdentity shelf)
                                && IsEmpty(shelf))
            interactionUI.ShowPrompt("[G] Move empty shelf (test layout)");
    }

    private bool CanStartPlacement()
    {
        return playerCamera != null && playerInteraction != null &&
               interactionUI != null && storeFloor != null &&
               navMeshSurface != null && storeClock != null &&
               customerFlow != null &&
               !storeClock.IsStoreOpen && !storeClock.HasDayEnded &&
               customerFlow.ActiveCustomerCount == 0 &&
               !playerInteraction.HasAnythingHeld &&
               Time.timeScale > 0f &&
               Cursor.lockState == CursorLockMode.Locked;
    }

    private bool TryFindLookedAtShelf(out ShelfIdentity shelf)
    {
        shelf = null;
        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(
            ray, grabDistance, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Only select if the closest solid object belongs to a shelf.
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                continue;
            shelf = hit.collider.GetComponentInParent<ShelfIdentity>();
            return shelf != null;
        }
        return false;
    }

    private static bool IsEmpty(ShelfIdentity shelf)
    {
        if (shelf == null)
            return false;
        ShelfSlot[] slots = shelf.GetComponentsInChildren<ShelfSlot>();
        if (slots.Length == 0)
            return false;
        foreach (ShelfSlot slot in slots)
            if (slot.StoredItemCount > 0)
                return false;
        return true;
    }

    private void TryStartPlacement()
    {
        if (!TryFindLookedAtShelf(out ShelfIdentity shelf) || !IsEmpty(shelf))
            return;

        selectedShelf = shelf;
        originalPosition = shelf.transform.position;
        originalRotation = shelf.transform.rotation;
        previewYaw = shelf.transform.eulerAngles.y;
        canPlace = false;

        selectedColliders = shelf.GetComponentsInChildren<Collider>(true);
        originallyEnabled = new bool[selectedColliders.Length];
        for (int i = 0; i < selectedColliders.Length; i++)
        {
            originallyEnabled[i] = selectedColliders[i].enabled;
            selectedColliders[i].enabled = false;
        }

        selectedRenderers = shelf.GetComponentsInChildren<MeshRenderer>(true);
        originalProperties = new MaterialPropertyBlock[selectedRenderers.Length];
        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            originalProperties[i] = new MaterialPropertyBlock();
            selectedRenderers[i].GetPropertyBlock(originalProperties[i]);
        }

        playerInteraction.enabled = false;
        CreateAccessOutline();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (selectedShelf == null)
            return;

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(
            ray, previewRayDistance, ~0, QueryTriggerInteraction.Ignore);
        bool foundFloor = false;
        Vector3 target = originalPosition;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != storeFloor)
                continue;

            float step = Mathf.Max(0.1f, gridSize);
            target = new Vector3(
                Mathf.Round(hit.point.x / step) * step,
                storeFloor.bounds.max.y,
                Mathf.Round(hit.point.z / step) * step);
            foundFloor = true;
            break;
        }

        Quaternion rotation = Quaternion.Euler(0f, previewYaw, 0f);
        if (foundFloor)
            selectedShelf.transform.SetPositionAndRotation(target, rotation);

        canPlace = foundFloor && ValidatePlacement(target, rotation);
        Color previewColor = canPlace ? validColor : invalidColor;
        TintPreview(previewColor);
        UpdateAccessOutline(previewColor);
    }

    private bool ValidatePlacement(Vector3 position, Quaternion rotation)
    {
        // Both the furniture itself AND the customer/player access strip must
        // be within the permitted shop floor. This space is never a collider.
        if (!IsFootprintOnFloor(position, rotation, footprintCenter, footprintHalfExtents))
            return false;
        if (!IsFootprintOnFloor(position, rotation, accessCenter, accessHalfExtents))
            return false;

        if (HasBlockingCollider(position, rotation, footprintCenter, footprintHalfExtents, false))
            return false;

        // The player is allowed to stand in the access area while arranging it.
        // Other solid furniture/walls still make the arrangement invalid.
        if (HasBlockingCollider(position, rotation, accessCenter, accessHalfExtents, true))
            return false;

        return true;
    }

    private bool IsFootprintOnFloor(
        Vector3 position, Quaternion rotation, Vector3 offset, Vector3 halfExtents)
    {
        Bounds floor = storeFloor.bounds;
        float halfX = Mathf.Max(0.01f, halfExtents.x);
        float halfZ = Mathf.Max(0.01f, halfExtents.z);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = position + rotation *
                (offset + new Vector3(x * halfX, 0f, z * halfZ));
            if (corner.x < floor.min.x || corner.x > floor.max.x ||
                corner.z < floor.min.z || corner.z > floor.max.z)
                return false;
        }
        return true;
    }

    private bool HasBlockingCollider(
        Vector3 position, Quaternion rotation, Vector3 offset,
        Vector3 halfExtents, bool ignorePlayer)
    {
        Vector3 center = position + rotation * offset;
        Collider[] hits = Physics.OverlapBox(center, halfExtents,
            rotation, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null || hit == storeFloor)
                continue;
            if (selectedShelf != null &&
                hit.transform.IsChildOf(selectedShelf.transform))
                continue;
            if (ignorePlayer && hit.transform.IsChildOf(transform))
                continue;
            return true;
        }
        return false;
    }

    private void CreateAccessOutline()
    {
        if (accessOutline != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("No compatible outline shader found.", this);
            return;
        }

        GameObject outlineObject = new("ShelfFrontAccessPreview");
        accessOutline = outlineObject.AddComponent<LineRenderer>();
        accessOutline.useWorldSpace = true;
        accessOutline.positionCount = 5;
        accessOutline.widthMultiplier = outlineWidth;
        accessOutline.numCornerVertices = 2;
        accessOutline.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        accessOutline.receiveShadows = false;

        outlineMaterial = new Material(shader);
        accessOutline.sharedMaterial = outlineMaterial;
    }

    private void UpdateAccessOutline(Color color)
    {
        if (accessOutline == null || selectedShelf == null)
            return;

        // Display a low, flat rectangle in GAME view. Never add a collider:
        // this represents required clear walking/interaction space.
        Vector3 half = accessHalfExtents;
        Vector3 center = accessCenter;
        float y = 0.075f;
        Vector3[] localCorners =
        {
            new(center.x - half.x, y, center.z - half.z),
            new(center.x + half.x, y, center.z - half.z),
            new(center.x + half.x, y, center.z + half.z),
            new(center.x - half.x, y, center.z + half.z)
        };

        for (int i = 0; i < 4; i++)
            accessOutline.SetPosition(i,
                selectedShelf.transform.TransformPoint(localCorners[i]));
        accessOutline.SetPosition(4,
            selectedShelf.transform.TransformPoint(localCorners[0]));

        accessOutline.startColor = color;
        accessOutline.endColor = color;
        if (outlineMaterial != null)
        {
            if (outlineMaterial.HasProperty(BaseColorId))
                outlineMaterial.SetColor(BaseColorId, color);
            else
                outlineMaterial.color = color;
        }
    }

    private void DestroyAccessOutline()
    {
        if (accessOutline != null)
            Destroy(accessOutline.gameObject);
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
        accessOutline = null;
        outlineMaterial = null;
    }

    private void TintPreview(Color color)
    {
        if (selectedRenderers == null)
            return;
        foreach (MeshRenderer renderer in selectedRenderers)
        {
            if (renderer == null)
                continue;
            MaterialPropertyBlock propertyBlock = new();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ConfirmPlacement()
    {
        FinishPreview(false);
        hasUnpersistedFurnitureChanges = true;
        // Current scene already has Navigation/NavMeshSurface.
        // Rebuild after the moved shelf's colliders are enabled again.
        navMeshSurface.BuildNavMesh();
        Debug.Log("Shelf moved. Layout persistence will be added in Lesson 101.", this);
    }

    private void CancelPlacement()
    {
        FinishPreview(true);
    }

    private void FinishPreview(bool restorePosition)
    {
        if (selectedShelf == null)
            return;

        if (restorePosition)
            selectedShelf.transform.SetPositionAndRotation(originalPosition, originalRotation);

        if (selectedColliders != null)
            for (int i = 0; i < selectedColliders.Length; i++)
                if (selectedColliders[i] != null)
                    selectedColliders[i].enabled = originallyEnabled[i];

        if (selectedRenderers != null)
            for (int i = 0; i < selectedRenderers.Length; i++)
                if (selectedRenderers[i] != null)
                    selectedRenderers[i].SetPropertyBlock(originalProperties[i]);

        DestroyAccessOutline();
        selectedShelf = null;
        selectedColliders = null;
        selectedRenderers = null;
        originalProperties = null;
        originallyEnabled = null;
        canPlace = false;
        if (Cursor.lockState == CursorLockMode.Locked && playerInteraction != null)
            playerInteraction.enabled = true;
        if (interactionUI != null)
            interactionUI.HidePrompt();
    }

    private void OnDisable()
    {
        CancelPlacement();
    }
}
