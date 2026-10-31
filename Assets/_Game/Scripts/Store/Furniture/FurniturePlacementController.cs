using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private StoreBuildArea buildArea;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Placement")]
    [Min(1f)] [SerializeField] private float grabDistance = 4f;
    [Min(1f)] [SerializeField] private float previewRayDistance = 18f;
    [Min(0.1f)] [SerializeField] private float gridSize = 0.5f;

    [Header("Fallback Body Footprint")]
    [SerializeField] private Vector3 footprintHalfExtents = new(1.30f, 1.05f, 0.52f);
    [SerializeField] private Vector3 footprintCenter = new(0f, 1.10f, 0f);

    [Header("Fallback Front Access Footprint")]
    [SerializeField] private Vector3 accessHalfExtents = new(1.25f, 0.75f, 0.50f);
    [SerializeField] private Vector3 accessCenter = new(0f, 0.80f, -1.02f);

    [Min(0.01f)] [SerializeField] private float outlineWidth = 0.05f;

    [Header("Preview Colours")]
    [SerializeField] private Color validColor = new Color32(111, 207, 151, 255);
    [SerializeField] private Color invalidColor = new Color32(196, 107, 107, 255);

    private FurnitureIdentity selectedFurniture;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float previewYaw;
    private bool canPlace;
    private bool isNewFurniturePurchase;
    private FurnitureShop pendingPurchase;

    private Collider[] selectedColliders;
    private bool[] originallyEnabled;
    private MeshRenderer[] selectedRenderers;
    private MaterialPropertyBlock[] originalProperties;
    private LineRenderer accessOutline;
    private Material outlineMaterial;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    public bool IsPlacementActive => selectedFurniture != null;

    private FurnitureDefinition CurrentDefinition =>
        selectedFurniture != null
            ? selectedFurniture.FurnitureDefinition
            : null;

    private StoreAreaType CurrentPlacementArea =>
        CurrentDefinition != null
            ? CurrentDefinition.PlacementArea
            : StoreAreaType.SalesFloor;

    private Vector3 CurrentBodyHalfExtents =>
        CurrentDefinition != null
            ? CurrentDefinition.BodyHalfExtents
            : footprintHalfExtents;

    private Vector3 CurrentBodyCenter =>
        CurrentDefinition != null
            ? CurrentDefinition.BodyCenter
            : footprintCenter;

    private Vector3 CurrentAccessHalfExtents =>
        CurrentDefinition != null
            ? CurrentDefinition.AccessHalfExtents
            : accessHalfExtents;

    private Vector3 CurrentAccessCenter =>
        CurrentDefinition != null
            ? CurrentDefinition.AccessCenter
            : accessCenter;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (selectedFurniture == null)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame &&
                CanStartPlacement())
            {
                TryStartPlacement();
            }

            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked ||
            Time.timeScale <= 0f ||
            storeClock == null ||
            storeClock.IsStoreOpen ||
            customerFlow == null ||
            customerFlow.ActiveCustomerCount > 0)
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
        if (interactionUI == null ||
            Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        if (selectedFurniture != null)
        {
            if (!canPlace)
            {
                interactionUI.ShowPrompt(
                    "Keep furniture and front access clear  |  [R] Right  [T] Left  |  [Q] Cancel"
                );

                return;
            }

            if (isNewFurniturePurchase)
            {
                FurnitureDefinition definition = CurrentDefinition;

                string priceText = definition != null
                    ? $"£{definition.PurchasePrice:0.00}"
                    : "purchase";

                interactionUI.ShowPrompt(
                    $"[E] Buy & place {priceText}  |  [R] Right  [T] Left  |  [Q] Cancel (free)"
                );
            }
            else
            {
                interactionUI.ShowPrompt(
                    "[E] Place  |  [R] Right  [T] Left  |  [Q] Cancel"
                );
            }

            return;
        }

        if (CanStartPlacement() &&
            TryFindLookedAtFurniture(out FurnitureIdentity furniture) &&
            furniture.CanMoveFurniture(out _))
        {
            interactionUI.ShowPrompt("[G] Move empty furniture");
        }
    }

    private bool CanStartPlacement()
    {
        return playerCamera != null &&
               playerInteraction != null &&
               interactionUI != null &&
               buildArea != null &&
               navMeshSurface != null &&
               storeClock != null &&
               customerFlow != null &&
               !storeClock.IsStoreOpen &&
               !storeClock.HasDayEnded &&
               customerFlow.ActiveCustomerCount == 0 &&
               !playerInteraction.HasAnythingHeld &&
               Time.timeScale > 0f &&
               Cursor.lockState == CursorLockMode.Locked;
    }

    private bool TryFindLookedAtFurniture(
        out FurnitureIdentity furniture)
    {
        furniture = null;

        Ray ray = new(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            grabDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            FurnitureIdentity candidate =
                hit.collider.GetComponentInParent<FurnitureIdentity>();

            if (candidate == null)
                continue;

            furniture = candidate;
            return true;
        }

        return false;
    }

    public bool BeginPurchasedFurniturePreview(
        FurnitureIdentity furniture,
        FurnitureShop shop)
    {
        if (furniture == null ||
            furniture.FurnitureDefinition == null ||
            shop == null ||
            selectedFurniture != null ||
            !CanStartPlacement())
        {
            return false;
        }

        StartPreview(furniture, true, shop);

        return true;
    }

    private void TryStartPlacement()
    {
        if (!TryFindLookedAtFurniture(
                out FurnitureIdentity furniture))
        {
            return;
        }

        if (!furniture.CanMoveFurniture(out _))
            return;

        StartPreview(furniture, false, null);
    }

    private void StartPreview(
        FurnitureIdentity furniture,
        bool purchased,
        FurnitureShop shop)
    {
        selectedFurniture = furniture;
        isNewFurniturePurchase = purchased;
        pendingPurchase = shop;

        originalPosition = furniture.transform.position;
        originalRotation = furniture.transform.rotation;
        previewYaw = furniture.transform.eulerAngles.y;
        canPlace = false;

        selectedColliders =
            furniture.GetComponentsInChildren<Collider>(true);

        originallyEnabled = new bool[selectedColliders.Length];

        for (int i = 0; i < selectedColliders.Length; i++)
        {
            originallyEnabled[i] = selectedColliders[i].enabled;
            selectedColliders[i].enabled = false;
        }

        selectedRenderers =
            furniture.GetComponentsInChildren<MeshRenderer>(true);

        originalProperties =
            new MaterialPropertyBlock[selectedRenderers.Length];

        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            originalProperties[i] = new MaterialPropertyBlock();

            selectedRenderers[i]
                .GetPropertyBlock(originalProperties[i]);
        }

        playerInteraction.enabled = false;

        CreateAccessOutline();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (selectedFurniture == null)
            return;

        Ray ray = new(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        bool foundFloor = buildArea.TryGetPlacementSurface(
            ray,
            previewRayDistance,
            CurrentPlacementArea,
            out Vector3 surfacePoint
        );

        Vector3 target = originalPosition;

        if (foundFloor)
        {
            float step = Mathf.Max(0.1f, gridSize);

            target = new Vector3(
                Mathf.Round(surfacePoint.x / step) * step,
                surfacePoint.y,
                Mathf.Round(surfacePoint.z / step) * step
            );
        }

        Quaternion rotation =
            Quaternion.Euler(0f, previewYaw, 0f);

        if (foundFloor)
        {
            selectedFurniture.transform.SetPositionAndRotation(
                target,
                rotation
            );
        }

        canPlace =
            foundFloor &&
            ValidatePlacement(target, rotation);

        Color previewColor =
            canPlace ? validColor : invalidColor;

        TintPreview(previewColor);
        UpdateAccessOutline(previewColor);
    }

    private bool ValidatePlacement(
        Vector3 position,
        Quaternion rotation)
    {
        if (!buildArea.IsFootprintInside(
                position,
                rotation,
                CurrentBodyCenter,
                CurrentBodyHalfExtents,
                CurrentPlacementArea))
        {
            return false;
        }

        if (!buildArea.IsFootprintInside(
                position,
                rotation,
                CurrentAccessCenter,
                CurrentAccessHalfExtents,
                CurrentPlacementArea))
        {
            return false;
        }

        if (HasBlockingCollider(
                position,
                rotation,
                CurrentBodyCenter,
                CurrentBodyHalfExtents,
                false))
        {
            return false;
        }

        if (HasBlockingCollider(
                position,
                rotation,
                CurrentAccessCenter,
                CurrentAccessHalfExtents,
                true))
        {
            return false;
        }

        return true;
    }

    private bool HasBlockingCollider(
        Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents,
        bool ignorePlayer)
    {
        Vector3 center =
            position + rotation * offset;

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == null ||
                buildArea.IsBuildFloorCollider(hit))
            {
                continue;
            }

            if (selectedFurniture != null &&
                hit.transform.IsChildOf(selectedFurniture.transform))
            {
                continue;
            }

            if (ignorePlayer &&
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void CreateAccessOutline()
    {
        if (accessOutline != null)
            return;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning(
                "No compatible outline shader found.",
                this
            );

            return;
        }

        GameObject outlineObject =
            new("FurnitureFrontAccessPreview");

        accessOutline =
            outlineObject.AddComponent<LineRenderer>();

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
        if (accessOutline == null ||
            selectedFurniture == null)
        {
            return;
        }

        Vector3 half = CurrentAccessHalfExtents;
        Vector3 center = CurrentAccessCenter;

        const float y = 0.075f;

        Vector3[] localCorners =
        {
            new(center.x - half.x, y, center.z - half.z),
            new(center.x + half.x, y, center.z - half.z),
            new(center.x + half.x, y, center.z + half.z),
            new(center.x - half.x, y, center.z + half.z)
        };

        for (int i = 0; i < 4; i++)
        {
            accessOutline.SetPosition(
                i,
                selectedFurniture.transform
                    .TransformPoint(localCorners[i])
            );
        }

        accessOutline.SetPosition(
            4,
            selectedFurniture.transform
                .TransformPoint(localCorners[0])
        );

        accessOutline.startColor = color;
        accessOutline.endColor = color;

        if (outlineMaterial == null)
            return;

        if (outlineMaterial.HasProperty(BaseColorId))
            outlineMaterial.SetColor(BaseColorId, color);
        else
            outlineMaterial.color = color;
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
        string furnitureName =
            CurrentDefinition != null
                ? CurrentDefinition.DisplayName
                : "Furniture";

        if (isNewFurniturePurchase)
        {
            FurnitureDefinition definition = CurrentDefinition;

            if (definition == null ||
                pendingPurchase == null ||
                !pendingPurchase.TryCompletePurchase(definition))
            {
                Debug.LogWarning(
                    "Not enough money or invalid furniture purchase configuration.",
                    this
                );

                return;
            }

            selectedFurniture.SetRuntimeActive(true);
        }

        FinishPreview(false);

        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();

        Debug.Log(
            $"{furnitureName} placed. Save your game to keep the new layout.",
            this
        );
    }

    private void CancelPlacement()
    {
        if (selectedFurniture == null)
            return;

        if (isNewFurniturePurchase)
        {
            FurnitureIdentity preview = selectedFurniture;
            FurnitureShop shop = pendingPurchase;

            FinishPreview(false);

            if (shop != null)
                shop.CancelPurchase(preview);
        }
        else
        {
            FinishPreview(true);
        }
    }

    private void FinishPreview(bool restorePosition)
    {
        if (selectedFurniture == null)
            return;

        if (restorePosition)
        {
            selectedFurniture.transform.SetPositionAndRotation(
                originalPosition,
                originalRotation
            );
        }

        if (selectedColliders != null)
        {
            for (int i = 0; i < selectedColliders.Length; i++)
            {
                if (selectedColliders[i] != null)
                {
                    selectedColliders[i].enabled =
                        originallyEnabled[i];
                }
            }
        }

        if (selectedRenderers != null)
        {
            for (int i = 0; i < selectedRenderers.Length; i++)
            {
                if (selectedRenderers[i] != null)
                {
                    selectedRenderers[i]
                        .SetPropertyBlock(originalProperties[i]);
                }
            }
        }

        DestroyAccessOutline();

        selectedFurniture = null;
        isNewFurniturePurchase = false;
        pendingPurchase = null;
        selectedColliders = null;
        selectedRenderers = null;
        originalProperties = null;
        originallyEnabled = null;
        canPlace = false;

        if (Cursor.lockState == CursorLockMode.Locked &&
            playerInteraction != null)
        {
            playerInteraction.enabled = true;
        }

        if (interactionUI != null)
            interactionUI.HidePrompt();
    }

    private void OnDisable()
    {
        CancelPlacement();
    }
}
