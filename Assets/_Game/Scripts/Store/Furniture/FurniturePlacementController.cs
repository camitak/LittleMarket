using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class FurniturePlacementController : MonoBehaviour
{
    [Header("Player")] 
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InteractionUI interactionUI;

    [Header("Store")]
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private StoreBuildArea buildArea;
    [SerializeField] private FurnitureShop furnitureShop;

    [Header("Visuals")] 
    [SerializeField] private FurniturePlacementVisualizer placementVisualizer;

    [Header("Placement")] 
    [Min(1f)] [SerializeField] private float selectionRayDistance = 35f;
    [Min(1f)] [SerializeField] private float previewRayDistance = 30f;
    [Min(0f)] [SerializeField] private float bodyContactTolerance = 0.01f;

    [Header("Placement Mode")] 
    [FormerlySerializedAs("gridSnapEnabled")] 
    [SerializeField] private bool structuredPlacementByDefault = true;
    [Min(0.01f)] [SerializeField] private float gridSize = 0.5f;
    [Min(1f)] [SerializeField] private float freeRotationDegreesPerSecond = 90f;

    [Header("Magnetic Snap")] 
    [SerializeField] private bool magneticSnapEnabled = true;
    [Min(0.01f)] [SerializeField] private float socketCaptureDistance = 0.30f;
    [Min(0.01f)] [SerializeField] private float socketReleaseDistance = 0.55f;
    [Range(0f, 45f)] [SerializeField] private float maximumSocketFacingAngle = 15f;

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
    private FurnitureIdentity[] furnitureCandidates;
    private LayoutClearanceZone[] clearanceZones;

    private FurnitureSnapSocket[] selectedSnapSockets;
    private FurnitureSnapSocket[] snapSocketCandidates;
    private FurnitureSnapSocket activeSelectedSnapSocket;
    private FurnitureSnapSocket activeTargetSnapSocket;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private float previewYaw;
    private bool canPlace;
    private bool isNewFurniturePurchase;

    private FurnitureShop pendingPurchase;
    private bool repeatPurchasedPlacement;
    private int lastPlacementCancelledFrame = -1;
    
    private string transientStatusMessage = "";
    private float transientStatusUntil;
    private const float TransientStatusDuration = 1.75f;

    private Collider[] selectedColliders;
    private bool[] originallyEnabled;

    private MeshRenderer[] selectedRenderers;
    private MaterialPropertyBlock[] originalProperties;

    private LineRenderer accessOutline;
    private Material outlineMaterial;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public bool IsPlacementActive => selectedFurniture != null;

    public bool StructuredPlacementByDefault => structuredPlacementByDefault;

    public bool IsStructuredPlacementActive =>
        IsPlacementModeModifierHeld ? !structuredPlacementByDefault : structuredPlacementByDefault;

    public bool IsFreePlacementActive => !IsStructuredPlacementActive;
    
    public bool CancelledPlacementThisFrame => lastPlacementCancelledFrame == Time.frameCount;
    /*
     * Compatibility property.
     *
     * Grid participation now follows the complete
     * Structured/Free placement mode rather than
     * existing as an independent preference.
     */
    public bool GridSnapEnabled => IsStructuredPlacementActive;

    public bool IsPlacementModeTemporarilyInverted => IsPlacementModeModifierHeld;

    public float GridSize => Mathf.Max(0.01f, gridSize);

    public bool IsMagneticallySnapped => activeSelectedSnapSocket != null && activeTargetSnapSocket != null;

    // Read-only access for Layout Mode presentation.
    public FurnitureIdentity SelectedFurniture => selectedFurniture;

    private float EffectiveSocketReleaseDistance =>
        Mathf.Max(
            socketCaptureDistance,
            socketReleaseDistance
        );

    private bool IsPlacementModeModifierHeld =>
        Keyboard.current != null &&
        (
            Keyboard.current.leftCtrlKey.isPressed ||
            Keyboard.current.rightCtrlKey.isPressed
        );

    public bool TryGetSelectedBodyFootprint(out Vector3 worldCenter,
        out Quaternion rotation,
        out Vector2 halfExtents,
        out StoreAreaType placementArea)
    {
        worldCenter = Vector3.zero;
        rotation = Quaternion.identity;
        halfExtents = Vector2.zero;
        placementArea = StoreAreaType.SalesFloor;

        if (selectedFurniture == null)
        {
            return false;
        }

        rotation = selectedFurniture.transform.rotation;

        worldCenter = selectedFurniture.transform.position + rotation * CurrentBodyCenter;

        halfExtents = new Vector2(Mathf.Abs(CurrentBodyHalfExtents.x), Mathf.Abs(CurrentBodyHalfExtents.z));

        placementArea = CurrentPlacementArea;

        return true;
    }

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
   
    private bool CurrentRequiresOperationalAccess =>
        CurrentDefinition == null ||
        CurrentDefinition.RequiresOperationalAccess;

    
    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
        {
            return;
        }

        if (layoutMode != null &&
            layoutMode.IsActive &&
            Time.timeScale > 0f &&
            Cursor.lockState == CursorLockMode.None &&
            Keyboard.current.gKey.wasPressedThisFrame &&
            !IsPlacementModeModifierHeld)
        {
            structuredPlacementByDefault = !structuredPlacementByDefault;
            ClearActiveMagneticSnap();
        }

        if (selectedFurniture == null)
        {
            if (!CanStartPlacement() || layoutMode.IsCameraManipulating || IsPointerOverUI())
            {
                return;
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                TryCopyPointedFurniture();
                return;
            }

            // Keep Lesson 139's direct movement!
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryStartPlacement();
            }

            // Idle right-click is handled by StoreDesignFurnitureContextMenuUI.
            return;
        }

        if (Time.timeScale <= 0f ||
            layoutMode == null ||
            !layoutMode.IsActive ||
            Cursor.lockState != CursorLockMode.None)
        {
            CancelPlacement();
            return;
        }

        bool cancelPressed = Keyboard.current.qKey.wasPressedThisFrame ||
                             Mouse.current.rightButton.wasPressedThisFrame;

        if (cancelPressed)
        {
            CancelPlacement();
            return;
        }

        if (!IsStructuredPlacementActive && IsMagneticallySnapped)
        {
            ClearActiveMagneticSnap();
        }

        HandlePlacementRotation();

        bool pointerAvailable = !layoutMode.IsCameraManipulating && !IsPointerOverUI();

        if (pointerAvailable)
        {
            UpdatePreview();
        }

        bool confirmPressed = Mouse.current.leftButton.wasPressedThisFrame ||
                              Keyboard.current.eKey.wasPressedThisFrame;

        if (pointerAvailable && confirmPressed && canPlace)
        {
            ConfirmPlacement();
        }
    }

    
    private void HandlePlacementRotation()
    {
        if (Keyboard.current == null ||
            selectedFurniture == null)
        {
            return;
        }

        if (IsStructuredPlacementActive)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                previewYaw =
                    GetNextStructuredYaw(
                        previewYaw,
                        1
                    );
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                previewYaw =
                    GetNextStructuredYaw(
                        previewYaw,
                        -1
                    );
            }

            return;
        }

        float rotationInput = 0f;

        if (Keyboard.current.rKey.isPressed)
        {
            rotationInput += 1f;
        }

        if (Keyboard.current.tKey.isPressed)
        {
            rotationInput -= 1f;
        }

        if (Mathf.Approximately(
                rotationInput,
                0f))
        {
            return;
        }

        float rotationSpeed =
            Mathf.Max(
                1f,
                freeRotationDegreesPerSecond
            );

        previewYaw =
            Mathf.Repeat(
                previewYaw +
                rotationInput *
                rotationSpeed *
                Time.deltaTime,
                360f
            );

        /*
         * Free placement never participates in
         * magnetic socket snapping.
         */
        ClearActiveMagneticSnap();
    }

    private static float GetNextStructuredYaw(
        float currentYaw,
        int direction)
    {
        const float step = 90f;
        const float tolerance = 0.01f;

        float normalizedYaw =
            Mathf.Repeat(
                currentYaw,
                360f
            );

        if (direction > 0)
        {
            float nextStep =
                Mathf.Floor(
                    (normalizedYaw + tolerance) /
                    step
                ) + 1f;

            return Mathf.Repeat(
                nextStep * step,
                360f
            );
        }

        if (direction < 0)
        {
            float previousStep =
                Mathf.Ceil(
                    (normalizedYaw - tolerance) /
                    step
                ) - 1f;

            return Mathf.Repeat(
                previousStep * step,
                360f
            );
        }

        return normalizedYaw;
    }

    
    private void LateUpdate()
    {
        if (interactionUI == null ||
            layoutMode == null ||
            !layoutMode.IsActive ||
            Cursor.lockState != CursorLockMode.None)
        {
            return;
        }

        if (TryShowTransientStatus())
        {
            return;
        }

        if (selectedFurniture != null)
        {
            if (!canPlace)
            {
                interactionUI.ShowPrompt(
                    "Can't place here — keep furniture and customer access clear.\n" +
                    "R / T Rotate  •  RMB / Q Cancel");
                return;
            }

            if (isNewFurniturePurchase)
            {
                FurnitureDefinition definition = CurrentDefinition;

                interactionUI.ShowPrompt(definition != null
                    ? $"LMB / E Buy & place £{definition.PurchasePrice:0.00}  •  RMB Cancel"
                    : "LMB / E Place  •  RMB Cancel");
                return;
            }

            interactionUI.ShowPrompt("LMB / E Place  •  R / T Rotate  •  RMB Cancel");
            return;
        }

        if (!layoutMode.IsCameraManipulating &&
            CanStartPlacement() &&
            !IsPointerOverUI() &&
            TryFindPointedAtFurniture(out FurnitureIdentity furniture))
        {
            ShowFurnitureIdlePrompt(furniture);
            return;
        }

        interactionUI.HidePrompt();
    }


    private bool CanStartPlacement()
    {
        return PlacementCamera != null && Mouse.current != null && playerInteraction != null && interactionUI != null &&
               layoutMode != null && layoutMode.IsActive && buildArea != null && !playerInteraction.HasAnythingHeld &&
               Time.timeScale > 0f && Cursor.lockState == CursorLockMode.None;
    }

    public bool TryFindPointedAtFurniture(out FurnitureIdentity furniture)
    {
        furniture = null;

        Camera placementCamera = PlacementCamera;

        if (placementCamera == null || Mouse.current == null)
        {
            return false;
        }

        Ray ray = CreatePointerRay(placementCamera);

        
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            selectionRayDistance,
            ~0,
            QueryTriggerInteraction.Collide);


        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            FurnitureIdentity candidate = hit.collider.GetComponentInParent<FurnitureIdentity>();

            if (candidate == null)
            {
                continue;
            }

            furniture = candidate;
            return true;
        }

        return false;
    }
    
    
    private void TryCopyPointedFurniture()
    {
        if (furnitureShop == null ||
            !TryFindPointedAtFurniture(out FurnitureIdentity sourceFurniture))
        {
            return;
        }

        FurnitureDefinition definition = sourceFurniture.FurnitureDefinition;
        float sourceYaw = sourceFurniture.transform.eulerAngles.y;

        if (!furnitureShop.TryBeginPurchase(definition, out string reason, sourceYaw, false))
        {
            ShowTransientStatus(reason);
        }
    }

    
    
    private void ShowFurnitureIdlePrompt(FurnitureIdentity furniture)
    {
        if (furniture == null)
        {
            interactionUI.HidePrompt();
            return;
        }

        FurnitureDefinition definition = furniture.FurnitureDefinition;
        string furnitureName = definition != null ? definition.DisplayName : "furniture";
        bool canMove = furniture.CanMoveFurniture(out _);

        string prompt = canMove
            ? $"LMB Move {furnitureName}  •  RMB Options"
            : $"RMB Options: {furnitureName}";

        if (furnitureShop != null && furnitureShop.IsPurchasableCatalogItem(definition))
        {
            prompt += "  •  C Quick Copy";
        }

        interactionUI.ShowPrompt(prompt);
    }


    public void ShowTransientStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        transientStatusMessage = message;
        transientStatusUntil = Time.unscaledTime + TransientStatusDuration;
    }

    private bool TryShowTransientStatus()
    {
        if (string.IsNullOrWhiteSpace(transientStatusMessage))
        {
            return false;
        }

        if (Time.unscaledTime >= transientStatusUntil)
        {
            transientStatusMessage = "";
            transientStatusUntil = 0f;
            return false;
        }

        interactionUI.ShowPrompt(transientStatusMessage);
        return true;
    }

    
    public bool BeginPurchasedFurniturePreview(
        FurnitureIdentity furniture,
        FurnitureShop shop,
        float? initialYaw = null,
        bool repeatPlacement = true)
    {
        if (furniture == null ||
            furniture.FurnitureDefinition == null ||
            shop == null ||
            selectedFurniture != null ||
            !CanStartPlacement())
        {
            return false;
        }

        StartPreview(furniture, true, shop, initialYaw);
        repeatPurchasedPlacement = repeatPlacement;

        return true;
    }


    private void TryStartPlacement()
    {
        if (!TryFindPointedAtFurniture(out FurnitureIdentity furniture))
        {
            return;
        }

        if (!furniture.CanMoveFurniture(out _))
        {
            return;
        }

        StartPreview(furniture, false, null, null);
    }

    private void StartPreview(
        FurnitureIdentity furniture,
        bool purchased,
        FurnitureShop shop,
        float? initialYaw)
    {
        selectedFurniture = furniture;
        isNewFurniturePurchase = purchased;
        pendingPurchase = shop;

        transientStatusMessage = "";
        transientStatusUntil = 0f;

        originalPosition = furniture.transform.position;
        originalRotation = furniture.transform.rotation;

        previewYaw = initialYaw ?? furniture.transform.eulerAngles.y;

        canPlace = false;

        furnitureCandidates = FindObjectsByType<FurnitureIdentity>(FindObjectsSortMode.None);

        clearanceZones = FindObjectsByType<LayoutClearanceZone>(FindObjectsSortMode.None);

        selectedSnapSockets = furniture.GetComponentsInChildren<FurnitureSnapSocket>(true);

        snapSocketCandidates = FindObjectsByType<FurnitureSnapSocket>(FindObjectsSortMode.None);

        ClearActiveMagneticSnap();

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
        {
            return;
        }

        Ray ray = CreatePointerRay(placementCamera);

        bool foundFloor =
            buildArea.TryGetPlacementSurface(ray, previewRayDistance, CurrentPlacementArea, out Vector3 surfacePoint);

        Quaternion rotation = Quaternion.Euler(0f, previewYaw, 0f);

        Vector3 target = originalPosition;

        if (foundFloor)
        {
            if (IsStructuredPlacementActive)
            {
                float step = GridSize;

                target =
                    new Vector3(
                        Mathf.Round(
                            surfacePoint.x / step
                        ) * step,
                        surfacePoint.y,
                        Mathf.Round(
                            surfacePoint.z / step
                        ) * step
                    );
            }
            else
            {
                target = surfacePoint;
            }

            target = ApplyMagneticSocketSnap(target, rotation);

            selectedFurniture.transform.SetPositionAndRotation(target, rotation);
        }
        else
        {
            ClearActiveMagneticSnap();
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
                CurrentRequiresOperationalAccess,
                canPlace
            );
            
            Vector3 snapPosition = IsMagneticallySnapped ? activeTargetSnapSocket.transform.position : Vector3.zero;

            placementVisualizer.UpdateSnapGuide(snapPosition, IsMagneticallySnapped, canPlace);
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

    private Vector3 ApplyMagneticSocketSnap(Vector3 baseTarget, Quaternion selectedRotation)
    {
        if (!magneticSnapEnabled ||
            !IsStructuredPlacementActive ||
            selectedFurniture == null ||
            selectedSnapSockets == null ||
            selectedSnapSockets.Length == 0 ||
            snapSocketCandidates == null)
        {
            ClearActiveMagneticSnap();
            return baseTarget;
        }

        if (TryKeepActiveMagneticSnap(baseTarget, selectedRotation, out Vector3 retainedTarget))
        {
            return retainedTarget;
        }

        if (TryFindMagneticSnap(baseTarget, selectedRotation, out Vector3 snappedTarget,
                out FurnitureSnapSocket selectedSocket, out FurnitureSnapSocket targetSocket))
        {
            activeSelectedSnapSocket = selectedSocket;

            activeTargetSnapSocket = targetSocket;

            return snappedTarget;
        }

        ClearActiveMagneticSnap();

        return baseTarget;
    }

    private bool TryKeepActiveMagneticSnap(Vector3 baseTarget, Quaternion selectedRotation, out Vector3 snappedTarget)
    {
        snappedTarget = baseTarget;

        if (!IsActiveSnapPairValid())
        {
            ClearActiveMagneticSnap();
            return false;
        }

        if (!AreSocketsFacing(activeSelectedSnapSocket, activeTargetSnapSocket, selectedRotation))
        {
            ClearActiveMagneticSnap();
            return false;
        }

        Vector3 projectedSocketPosition =
            GetProjectedSelectedSocketPosition(activeSelectedSnapSocket, baseTarget, selectedRotation);

        float distance = HorizontalDistance(projectedSocketPosition, activeTargetSnapSocket.transform.position);

        if (distance > EffectiveSocketReleaseDistance)
        {
            ClearActiveMagneticSnap();
            return false;
        }

        snappedTarget = AlignRootToTargetSocket(activeSelectedSnapSocket, activeTargetSnapSocket, baseTarget,
            selectedRotation);

        return true;
    }

    private bool TryFindMagneticSnap(Vector3 baseTarget,
        Quaternion selectedRotation,
        out Vector3 snappedTarget,
        out FurnitureSnapSocket selectedSocket,
        out FurnitureSnapSocket targetSocket)
    {
        snappedTarget = baseTarget;
        selectedSocket = null;
        targetSocket = null;

        float bestDistance = Mathf.Max(0.01f, socketCaptureDistance);

        foreach (FurnitureSnapSocket source in selectedSnapSockets)
        {
            if (source == null)
            {
                continue;
            }

            Vector3 projectedSource = GetProjectedSelectedSocketPosition(source, baseTarget, selectedRotation);

            foreach (FurnitureSnapSocket candidate in snapSocketCandidates)
            {
                if (!CanUseTargetSocket(source, candidate))
                {
                    continue;
                }

                if (!AreSocketsFacing(source, candidate, selectedRotation))
                {
                    continue;
                }

                float distance = HorizontalDistance(projectedSource, candidate.transform.position);

                if (distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                selectedSocket = source;
                targetSocket = candidate;
            }
        }

        if (selectedSocket == null || targetSocket == null)
        {
            return false;
        }

        snappedTarget = AlignRootToTargetSocket(selectedSocket, targetSocket, baseTarget, selectedRotation);

        return true;
    }

    private bool CanUseTargetSocket(FurnitureSnapSocket source, FurnitureSnapSocket target)
    {
        if (source == null || target == null || !source.IsCompatibleWith(target))
        {
            return false;
        }

        FurnitureIdentity targetOwner = target.Owner;

        if (targetOwner == null || targetOwner == selectedFurniture || !targetOwner.gameObject.activeInHierarchy)
        {
            return false;
        }

        FurnitureDefinition definition = targetOwner.FurnitureDefinition;

        return definition != null && definition.PlacementArea == CurrentPlacementArea;
    }

    private bool IsActiveSnapPairValid()
    {
        if (activeSelectedSnapSocket == null || activeTargetSnapSocket == null)
        {
            return false;
        }

        return CanUseTargetSocket(activeSelectedSnapSocket, activeTargetSnapSocket);
    }

    private bool AreSocketsFacing(FurnitureSnapSocket source, FurnitureSnapSocket target, Quaternion selectedRotation)
    {
        if (source == null || target == null || selectedFurniture == null)
        {
            return false;
        }

        Vector3 sourceOutward = selectedRotation * source.GetLocalOutward(selectedFurniture);

        sourceOutward.y = 0f;

        Vector3 targetInward = -target.WorldOutward;

        targetInward.y = 0f;

        if (sourceOutward.sqrMagnitude < 0.0001f || targetInward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float angle = Vector3.Angle(sourceOutward.normalized, targetInward.normalized);

        return angle <= maximumSocketFacingAngle;
    }

    private Vector3 GetProjectedSelectedSocketPosition(FurnitureSnapSocket socket,
        Vector3 baseTarget,
        Quaternion selectedRotation)
    {
        Vector3 localPosition = socket.GetLocalPosition(selectedFurniture);

        return baseTarget + selectedRotation * localPosition;
    }

    private Vector3 AlignRootToTargetSocket(FurnitureSnapSocket source,
        FurnitureSnapSocket target,
        Vector3 baseTarget,
        Quaternion selectedRotation)
    {
        Vector3 projectedSource = GetProjectedSelectedSocketPosition(source, baseTarget, selectedRotation);

        Vector3 delta = target.transform.position - projectedSource;

        // Furniture remains floor-anchored.
        delta.y = 0f;

        return baseTarget + delta;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private void ClearActiveMagneticSnap()
    {
        activeSelectedSnapSocket = null;
        activeTargetSnapSocket = null;

        if (placementVisualizer != null)
        {
            placementVisualizer.UpdateSnapGuide(Vector3.zero, false, false);
        }
    }

    private bool ValidatePlacement(
        Vector3 position,
        Quaternion rotation)
    {
        /*
         * Every placeable has a physical body.
         */
        if (!buildArea.IsFootprintInside(
                position,
                rotation,
                CurrentBodyCenter,
                CurrentBodyHalfExtents,
                CurrentPlacementArea))
        {
            return false;
        }

        if (OverlapsProtectedClearance(
                position,
                rotation,
                CurrentBodyCenter,
                CurrentBodyHalfExtents,
                false))
        {
            return false;
        }

        /*
         * A decoration may not have its own service
         * area, but its BODY still cannot occupy the
         * access area of functional furniture.
         */
        if (OverlapsOtherFurnitureAccess(
                position,
                rotation))
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

        /*
         * Front-access validation applies only to
         * placeables whose definition actually needs
         * operational access.
         */
        if (!CurrentRequiresOperationalAccess)
        {
            return true;
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

        if (OverlapsProtectedClearance(
                position,
                rotation,
                CurrentAccessCenter,
                CurrentAccessHalfExtents,
                true))
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

    private bool OverlapsOtherFurnitureAccess(Vector3 position, Quaternion rotation)
    {
        if (furnitureCandidates == null)
        {
            return false;
        }

        Vector3 selectedBodyCenter = position + rotation * CurrentBodyCenter;

        Vector2 selectedBodyHalf = new(Mathf.Abs(CurrentBodyHalfExtents.x), Mathf.Abs(CurrentBodyHalfExtents.z));

        foreach (FurnitureIdentity candidate in furnitureCandidates)
        {
            if (candidate == null || candidate == selectedFurniture || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            FurnitureDefinition definition = candidate.FurnitureDefinition;

            if (definition == null ||
                !definition.RequiresOperationalAccess ||
                definition.PlacementArea != CurrentPlacementArea)
            {
                continue;
            }

            Transform candidateTransform = candidate.transform;

            Vector3 candidateAccessCenter = candidateTransform.TransformPoint(definition.AccessCenter);

            Vector3 candidateScale = candidateTransform.lossyScale;

            Vector2 candidateAccessHalf = new(Mathf.Abs(definition.AccessHalfExtents.x * candidateScale.x),
                Mathf.Abs(definition.AccessHalfExtents.z * candidateScale.z));

            if (LayoutFootprintUtility.OverlapsHorizontal(selectedBodyCenter, rotation, selectedBodyHalf,
                    candidateAccessCenter, candidateTransform.rotation, candidateAccessHalf))
            {
                return true;
            }
        }

        return false;
    }

    private bool OverlapsProtectedClearance(Vector3 position,
        Quaternion rotation,
        Vector3 offset,
        Vector3 halfExtents,
        bool isAccessFootprint)
    {
        if (clearanceZones == null)
        {
            return false;
        }

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
        bool isAccessFootprint)
    {
        Vector3 center = position + rotation * offset;

        Vector3 overlapHalfExtents = halfExtents;

        if (!isAccessFootprint && bodyContactTolerance > 0f)
        {
            overlapHalfExtents.x = Mathf.Max(0.01f, overlapHalfExtents.x - bodyContactTolerance);

            overlapHalfExtents.z = Mathf.Max(0.01f, overlapHalfExtents.z - bodyContactTolerance);
        }

        Collider[] hits = Physics.OverlapBox(center, overlapHalfExtents, rotation, ~0, QueryTriggerInteraction.Ignore);

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

            if (isAccessFootprint && hit.transform.IsChildOf(transform))
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
        if (!CurrentRequiresOperationalAccess ||
            accessOutline != null)
        {
            return;
        }

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

    private void UpdateAccessOutline(
        Color color)
    {
        if (!CurrentRequiresOperationalAccess)
        {
            DestroyAccessOutline();
            return;
        }

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
        {
            return;
        }

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
        {
            return;
        }

        foreach (MeshRenderer renderer in selectedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new();

            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor(BaseColorId, color);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ConfirmPlacement()
    {
        FurnitureDefinition definition = CurrentDefinition;
        string furnitureName = definition != null ? definition.DisplayName : "Furniture";

        bool wasNewFurniturePurchase = isNewFurniturePurchase;
        bool shouldRepeatPurchase = repeatPurchasedPlacement;
        
        FurnitureShop purchaseShop = pendingPurchase;
        
        float repeatYaw = previewYaw;

        if (wasNewFurniturePurchase)
        {
            if (definition == null || purchaseShop == null || !purchaseShop.TryCompletePurchase(definition))
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

        if (wasNewFurniturePurchase && shouldRepeatPurchase &&
            definition != null && purchaseShop != null)
        {
            string repeatReason;

            if (purchaseShop.TryBeginPurchase(definition, out repeatReason, repeatYaw))
            {
                Debug.Log($"{furnitureName} placed. Repeat placement ready.", this);
                return;
            }

            if (!string.IsNullOrWhiteSpace(repeatReason))
            {
                ShowTransientStatus($"{furnitureName} placed. {repeatReason}");
            }
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
        {
            return;
        }
        
        lastPlacementCancelledFrame = Time.frameCount;

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
        {
            return;
        }

        if (restorePosition)
        {
            selectedFurniture.transform.SetPositionAndRotation(originalPosition, originalRotation);
        }

        if (selectedColliders != null)
        {
            for (int i = 0; i < selectedColliders.Length; i++)
            {
                if (selectedColliders[i] == null)
                {
                    continue;
                }

                selectedColliders[i].enabled = originallyEnabled[i];
            }
        }

        if (selectedRenderers != null)
        {
            for (int i = 0; i < selectedRenderers.Length; i++)
            {
                if (selectedRenderers[i] == null)
                {
                    continue;
                }

                selectedRenderers[i].SetPropertyBlock(originalProperties[i]);
            }
        }

        ClearActiveMagneticSnap();

        if (placementVisualizer != null)
        {
            placementVisualizer.Hide();
        }

        DestroyAccessOutline();

        selectedFurniture = null;
        furnitureCandidates = null;
        clearanceZones = null;

        selectedSnapSockets = null;
        snapSocketCandidates = null;

        isNewFurniturePurchase = false;
        repeatPurchasedPlacement = false;
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