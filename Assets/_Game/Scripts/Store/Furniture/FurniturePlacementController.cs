using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FurniturePlacementController : MonoBehaviour
{
    [Header("Player")] 
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InteractionUI interactionUI;

    [Header("Store")] 
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private StoreBuildArea buildArea;
    
    [Header("Visuals")]
    [SerializeField] private FurniturePlacementVisualizer placementVisualizer;

    [Header("Placement")] 
    [Min(1f)] [SerializeField] private float selectionRayDistance = 35f;
    [Min(1f)] [SerializeField] private float previewRayDistance = 30f;
    [Min(0.1f)] [SerializeField] private float gridSize = 0.5f;

    [Header("Furniture Snapping")] 
    [Min(0f)] [SerializeField] private float furnitureSnapDistance = 0.35f;
    [Min(0f)] [SerializeField] private float furnitureSnapGap = 0.05f;
    [Min(0f)] [SerializeField] private float alignmentSnapDistance = 0.30f;

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
    private FurnitureIdentity[] snapCandidates;
    private LayoutClearanceZone[] clearanceZones;

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

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public bool IsPlacementActive => selectedFurniture != null;

    private Camera PlacementCamera => layoutMode != null ? layoutMode.PlacementCamera : null;

    private FurnitureDefinition CurrentDefinition =>
        selectedFurniture != null ? selectedFurniture.FurnitureDefinition : null;

    private StoreAreaType CurrentPlacementArea =>
        CurrentDefinition != null ? CurrentDefinition.PlacementArea : StoreAreaType.SalesFloor;

    private Vector3 CurrentBodyHalfExtents =>
        CurrentDefinition != null ? CurrentDefinition.BodyHalfExtents : footprintHalfExtents;

    private Vector3 CurrentBodyCenter => CurrentDefinition != null ? CurrentDefinition.BodyCenter : footprintCenter;

    private Vector3 CurrentAccessHalfExtents =>
        CurrentDefinition != null ? CurrentDefinition.AccessHalfExtents : accessHalfExtents;

    private Vector3 CurrentAccessCenter => CurrentDefinition != null ? CurrentDefinition.AccessCenter : accessCenter;

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
        {
            return;
        }

        if (selectedFurniture == null)
        {
            if (!CanStartPlacement() || layoutMode.IsCameraManipulating || IsPointerOverUI())
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryStartPlacement();
            }

            return;
        }

        if (Time.timeScale <= 0f || layoutMode == null || !layoutMode.IsActive ||
            Cursor.lockState != CursorLockMode.None)
        {
            CancelPlacement();
            return;
        }

        bool cancelPressed = Keyboard.current.escapeKey.wasPressedThisFrame ||
                             Keyboard.current.qKey.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame;

        if (cancelPressed)
        {
            CancelPlacement();
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            previewYaw = Mathf.Repeat(previewYaw + 90f, 360f);
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            previewYaw = Mathf.Repeat(previewYaw - 90f, 360f);
        }

        bool pointerAvailable = !layoutMode.IsCameraManipulating && !IsPointerOverUI();

        if (pointerAvailable)
        {
            UpdatePreview();
        }

        bool confirmPressed = Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;

        if (pointerAvailable && confirmPressed && canPlace)
        {
            ConfirmPlacement();
        }
    }

    private void LateUpdate()
    {
        if (interactionUI == null || layoutMode == null || !layoutMode.IsActive ||
            Cursor.lockState != CursorLockMode.None)
        {
            return;
        }

        if (selectedFurniture != null)
        {
            if (!canPlace)
            {
                interactionUI.ShowPrompt(
                    "Keep furniture and required access clear  •  [R/T] Rotate  •  RMB/Esc Cancel  •  MMB Orbit");

                return;
            }

            if (isNewFurniturePurchase)
            {
                FurnitureDefinition definition = CurrentDefinition;

                string priceText = definition != null ? $"£{definition.PurchasePrice:0.00}" : "purchase";

                interactionUI.ShowPrompt(
                    $"LMB Buy & place {priceText}  •  [R/T] Rotate  •  RMB/Esc Cancel  •  MMB Orbit");

                return;
            }

            interactionUI.ShowPrompt("LMB Place  •  [R/T] Rotate  •  RMB/Esc Cancel  •  MMB Orbit");

            return;
        }

        if (!layoutMode.IsCameraManipulating && CanStartPlacement() && !IsPointerOverUI() &&
            TryFindPointedAtFurniture(out FurnitureIdentity furniture) && furniture.CanMoveFurniture(out _))
        {
            interactionUI.ShowPrompt("LMB Move furniture  •  MMB Orbit  •  Wheel Zoom  •  WASD Pan  •  [L] Exit");

            return;
        }

        interactionUI.ShowPrompt("LAYOUT MODE  •  LMB Select  •  MMB Orbit  •  Wheel Zoom  •  WASD Pan  •  [L] Exit");
    }

    private bool CanStartPlacement()
    {
        return PlacementCamera != null && Mouse.current != null && playerInteraction != null && interactionUI != null &&
               layoutMode != null && layoutMode.IsActive && buildArea != null && !playerInteraction.HasAnythingHeld &&
               Time.timeScale > 0f && Cursor.lockState == CursorLockMode.None;
    }

    private bool TryFindPointedAtFurniture(out FurnitureIdentity furniture)
    {
        furniture = null;

        Camera placementCamera = PlacementCamera;

        if (placementCamera == null || Mouse.current == null)
        {
            return false;
        }

        Ray ray = CreatePointerRay(placementCamera);

        RaycastHit[] hits = Physics.RaycastAll(ray, selectionRayDistance, ~0, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            FurnitureIdentity candidate = hit.collider.GetComponentInParent<FurnitureIdentity>();

            if (candidate == null)
                continue;

            furniture = candidate;
            return true;
        }

        return false;
    }

    public bool BeginPurchasedFurniturePreview(FurnitureIdentity furniture, FurnitureShop shop)
    {
        if (furniture == null || furniture.FurnitureDefinition == null || shop == null || selectedFurniture != null ||
            !CanStartPlacement())
        {
            return false;
        }

        StartPreview(furniture, true, shop);

        return true;
    }

    private void TryStartPlacement()
    {
        if (!TryFindPointedAtFurniture(out FurnitureIdentity furniture))
        {
            return;
        }

        if (!furniture.CanMoveFurniture(out _))
            return;

        StartPreview(furniture, false, null);
    }

    private void StartPreview(FurnitureIdentity furniture, bool purchased, FurnitureShop shop)
    {
        selectedFurniture = furniture;
        isNewFurniturePurchase = purchased;
        pendingPurchase = shop;

        originalPosition = furniture.transform.position;

        originalRotation = furniture.transform.rotation;

        previewYaw = furniture.transform.eulerAngles.y;

        canPlace = false;

        snapCandidates = FindObjectsByType<FurnitureIdentity>(FindObjectsSortMode.None);

        clearanceZones = FindObjectsByType<LayoutClearanceZone>(FindObjectsSortMode.None);

        selectedColliders = furniture.GetComponentsInChildren<Collider>(true);

        originallyEnabled = new bool[selectedColliders.Length];

        for (int i = 0; i < selectedColliders.Length; i++)
        {
            originallyEnabled[i] = selectedColliders[i].enabled;

            selectedColliders[i].enabled = false;
        }

        selectedRenderers = furniture.GetComponentsInChildren<MeshRenderer>(true);

        originalProperties = new MaterialPropertyBlock[selectedRenderers.Length];

        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            originalProperties[i] = new MaterialPropertyBlock();

            selectedRenderers[i].GetPropertyBlock(originalProperties[i]);
        }

        CreateAccessOutline();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (selectedFurniture == null || Mouse.current == null)
        {
            return;
        }

        Camera placementCamera = PlacementCamera;

        if (placementCamera == null)
            return;

        Ray ray = CreatePointerRay(placementCamera);

        bool foundFloor =
            buildArea.TryGetPlacementSurface(ray, previewRayDistance, CurrentPlacementArea, out Vector3 surfacePoint);

        Quaternion rotation = Quaternion.Euler(0f, previewYaw, 0f);

        Vector3 target = originalPosition;

        if (foundFloor)
        {
            float step = Mathf.Max(0.1f, gridSize);

            target = new Vector3(Mathf.Round(surfacePoint.x / step) * step, surfacePoint.y,
                Mathf.Round(surfacePoint.z / step) * step);

            target = ApplyFurnitureSnap(target, rotation);

            selectedFurniture.transform.SetPositionAndRotation(target, rotation);
        }

        canPlace = foundFloor && ValidatePlacement(target, rotation);

        if (placementVisualizer != null)
        {
            placementVisualizer.UpdateFootprints(
                selectedFurniture.transform,
                CurrentBodyCenter,
                CurrentBodyHalfExtents,
                CurrentAccessCenter,
                CurrentAccessHalfExtents,
                canPlace
            );
        }
        
        Color previewColor = canPlace ? validColor : invalidColor;

        TintPreview(previewColor);

        UpdateAccessOutline(previewColor);
    }

    private Ray CreatePointerRay(Camera placementCamera)
    {
        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        return placementCamera.ScreenPointToRay(pointerPosition);
    }

    private Vector3 ApplyFurnitureSnap(Vector3 gridTarget, Quaternion selectedRotation)
    {
        if (selectedFurniture == null || snapCandidates == null || furnitureSnapDistance <= 0f)
        {
            return gridTarget;
        }

        Vector3 selectedBodyCenter = gridTarget + selectedRotation * CurrentBodyCenter;

        Vector2 selectedHalf = GetHorizontalHalfExtents(selectedRotation, CurrentBodyHalfExtents);

        Vector3 bestTarget = gridTarget;

        float bestMovement = float.MaxValue;

        foreach (FurnitureIdentity candidate in snapCandidates)
        {
            if (candidate == null || candidate == selectedFurniture || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            FurnitureDefinition definition = candidate.FurnitureDefinition;

            if (definition == null || definition.PlacementArea != CurrentPlacementArea)
            {
                continue;
            }

            Quaternion otherRotation = candidate.transform.rotation;

            Vector3 otherBodyCenter = candidate.transform.position + otherRotation * definition.BodyCenter;

            Vector2 otherHalf = GetHorizontalHalfExtents(otherRotation, definition.BodyHalfExtents);

            float maximumZSeparation = selectedHalf.y + otherHalf.y + furnitureSnapDistance;

            if (Mathf.Abs(selectedBodyCenter.z - otherBodyCenter.z) <= maximumZSeparation)
            {
                float horizontalGap = selectedHalf.x + otherHalf.x + furnitureSnapGap;

                EvaluateSnapCandidate(
                    new Vector3(otherBodyCenter.x - horizontalGap, selectedBodyCenter.y, selectedBodyCenter.z),
                    otherBodyCenter, true, gridTarget, selectedRotation, ref bestTarget, ref bestMovement);

                EvaluateSnapCandidate(
                    new Vector3(otherBodyCenter.x + horizontalGap, selectedBodyCenter.y, selectedBodyCenter.z),
                    otherBodyCenter, true, gridTarget, selectedRotation, ref bestTarget, ref bestMovement);
            }

            float maximumXSeparation = selectedHalf.x + otherHalf.x + furnitureSnapDistance;

            if (Mathf.Abs(selectedBodyCenter.x - otherBodyCenter.x) <= maximumXSeparation)
            {
                float verticalGap = selectedHalf.y + otherHalf.y + furnitureSnapGap;

                EvaluateSnapCandidate(
                    new Vector3(selectedBodyCenter.x, selectedBodyCenter.y, otherBodyCenter.z - verticalGap),
                    otherBodyCenter, false, gridTarget, selectedRotation, ref bestTarget, ref bestMovement);

                EvaluateSnapCandidate(
                    new Vector3(selectedBodyCenter.x, selectedBodyCenter.y, otherBodyCenter.z + verticalGap),
                    otherBodyCenter, false, gridTarget, selectedRotation, ref bestTarget, ref bestMovement);
            }
        }

        return bestTarget;
    }

    private void EvaluateSnapCandidate(Vector3 candidateBodyCenter,
        Vector3 otherBodyCenter,
        bool snappingX,
        Vector3 gridTarget,
        Quaternion selectedRotation,
        ref Vector3 bestTarget,
        ref float bestMovement)
    {
        Vector3 currentBodyCenter = gridTarget + selectedRotation * CurrentBodyCenter;

        float edgeDistance = snappingX
            ? Mathf.Abs(currentBodyCenter.x - candidateBodyCenter.x)
            : Mathf.Abs(currentBodyCenter.z - candidateBodyCenter.z);

        if (edgeDistance > furnitureSnapDistance)
        {
            return;
        }

        if (snappingX)
        {
            if (Mathf.Abs(candidateBodyCenter.z - otherBodyCenter.z) <= alignmentSnapDistance)
            {
                candidateBodyCenter.z = otherBodyCenter.z;
            }
        }
        else
        {
            if (Mathf.Abs(candidateBodyCenter.x - otherBodyCenter.x) <= alignmentSnapDistance)
            {
                candidateBodyCenter.x = otherBodyCenter.x;
            }
        }

        Vector3 candidateRoot = candidateBodyCenter - selectedRotation * CurrentBodyCenter;

        candidateRoot.y = gridTarget.y;

        float movement = Vector2.Distance(new Vector2(gridTarget.x, gridTarget.z),
            new Vector2(candidateRoot.x, candidateRoot.z));

        if (movement >= bestMovement)
            return;

        bestMovement = movement;
        bestTarget = candidateRoot;
    }

    private static Vector2 GetHorizontalHalfExtents(Quaternion rotation, Vector3 halfExtents)
    {
        Vector3 right = rotation * Vector3.right;

        Vector3 forward = rotation * Vector3.forward;

        float worldHalfX = Mathf.Abs(right.x) * halfExtents.x + Mathf.Abs(forward.x) * halfExtents.z;

        float worldHalfZ = Mathf.Abs(right.z) * halfExtents.x + Mathf.Abs(forward.z) * halfExtents.z;

        return new Vector2(worldHalfX, worldHalfZ);
    }

    private bool ValidatePlacement(Vector3 position, Quaternion rotation)
    {
        if (!buildArea.IsFootprintInside(position, rotation, CurrentBodyCenter, CurrentBodyHalfExtents,
                CurrentPlacementArea))
        {
            return false;
        }

        if (!buildArea.IsFootprintInside(position, rotation, CurrentAccessCenter, CurrentAccessHalfExtents,
                CurrentPlacementArea))
        {
            return false;
        }

        if (OverlapsProtectedClearance(position, rotation, CurrentBodyCenter, CurrentBodyHalfExtents, false))
        {
            return false;
        }

        if (OverlapsProtectedClearance(position, rotation, CurrentAccessCenter, CurrentAccessHalfExtents, true))
        {
            return false;
        }

        if (HasBlockingCollider(position, rotation, CurrentBodyCenter, CurrentBodyHalfExtents, false))
        {
            return false;
        }

        if (HasBlockingCollider(position, rotation, CurrentAccessCenter, CurrentAccessHalfExtents, true))
        {
            return false;
        }

        return true;
    }

    private bool OverlapsProtectedClearance(Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents,
        bool isAccessFootprint)
    {
        if (clearanceZones == null)
            return false;

        foreach (LayoutClearanceZone zone in clearanceZones)
        {
            if (zone == null || !zone.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (zone.BlocksFootprint(CurrentPlacementArea, position, rotation, offset, halfExtents, isAccessFootprint))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasBlockingCollider(Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents,
        bool ignorePlayer)
    {
        Vector3 center = position + rotation * offset;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null || buildArea.IsBuildFloorCollider(hit))
            {
                continue;
            }

            if (selectedFurniture != null && hit.transform.IsChildOf(selectedFurniture.transform))
            {
                continue;
            }

            if (ignorePlayer && hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
    }

    private void CreateAccessOutline()
    {
        if (accessOutline != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("No compatible outline shader found.", this);

            return;
        }

        GameObject outlineObject = new("FurnitureFrontAccessPreview");

        accessOutline = outlineObject.AddComponent<LineRenderer>();

        accessOutline.useWorldSpace = true;

        accessOutline.positionCount = 5;

        accessOutline.widthMultiplier = outlineWidth;

        accessOutline.numCornerVertices = 2;

        accessOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        accessOutline.receiveShadows = false;

        outlineMaterial = new Material(shader);

        accessOutline.sharedMaterial = outlineMaterial;
    }

    private void UpdateAccessOutline(Color color)
    {
        if (accessOutline == null || selectedFurniture == null)
        {
            return;
        }

        Vector3 half = CurrentAccessHalfExtents;

        Vector3 center = CurrentAccessCenter;

        const float y = 0.075f;

        Vector3[] localCorners =
        {
            new(center.x - half.x, y, center.z - half.z), new(center.x + half.x, y, center.z - half.z),
            new(center.x + half.x, y, center.z + half.z), new(center.x - half.x, y, center.z + half.z)
        };

        for (int i = 0; i < 4; i++)
        {
            accessOutline.SetPosition(i, selectedFurniture.transform.TransformPoint(localCorners[i]));
        }

        accessOutline.SetPosition(4, selectedFurniture.transform.TransformPoint(localCorners[0]));

        accessOutline.startColor = color;

        accessOutline.endColor = color;

        if (outlineMaterial == null)
            return;

        if (outlineMaterial.HasProperty(BaseColorId))
        {
            outlineMaterial.SetColor(BaseColorId, color);
        }
        else
        {
            outlineMaterial.color = color;
        }
    }

    private void DestroyAccessOutline()
    {
        if (accessOutline != null)
        {
            Destroy(accessOutline.gameObject);
        }

        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }

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
        string furnitureName = CurrentDefinition != null ? CurrentDefinition.DisplayName : "Furniture";

        if (isNewFurniturePurchase)
        {
            FurnitureDefinition definition = CurrentDefinition;

            if (definition == null || pendingPurchase == null || !pendingPurchase.TryCompletePurchase(definition))
            {
                Debug.LogWarning("Not enough money or invalid furniture purchase configuration.", this);

                return;
            }

            selectedFurniture.SetRuntimeActive(true);
        }

        FinishPreview(false);

        if (layoutMode != null)
        {
            layoutMode.MarkNavigationDirty();
        }

        Debug.Log($"{furnitureName} placed. Save your game to keep the new layout.", this);
    }

    public void CancelActivePlacement()
    {
        CancelPlacement();
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
            {
                shop.CancelPurchase(preview);
            }
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
            selectedFurniture.transform.SetPositionAndRotation(originalPosition, originalRotation);
        }

        if (selectedColliders != null)
        {
            for (int i = 0; i < selectedColliders.Length; i++)
            {
                if (selectedColliders[i] == null)
                    continue;

                selectedColliders[i].enabled = originallyEnabled[i];
            }
        }

        if (selectedRenderers != null)
        {
            for (int i = 0; i < selectedRenderers.Length; i++)
            {
                if (selectedRenderers[i] == null)
                    continue;

                selectedRenderers[i].SetPropertyBlock(originalProperties[i]);
            }
        }

        if (placementVisualizer != null)
        {
            placementVisualizer.Hide();
        }

        DestroyAccessOutline();

        selectedFurniture = null;
        snapCandidates = null;
        clearanceZones = null;

        isNewFurniturePurchase = false;
        pendingPurchase = null;

        selectedColliders = null;
        selectedRenderers = null;
        originalProperties = null;
        originallyEnabled = null;

        canPlace = false;

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }
    }

    private void OnDisable()
    {
        CancelPlacement();

        if (placementVisualizer != null)
        {
            placementVisualizer.Hide();
        }
    }
}