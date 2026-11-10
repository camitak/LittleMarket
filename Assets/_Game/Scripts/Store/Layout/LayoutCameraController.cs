using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LayoutCameraController : MonoBehaviour
{
    [Header("Camera")] 
    [SerializeField] private Camera layoutCamera;

    [Header("Working View")] 
    [Min(1f)] [SerializeField] private float workingDistance = 9f;

    [Header("Overview / Target Framing")] 
    [Range(5f, 85f)] [SerializeField] private float overviewPitch = 62f;
    [SerializeField] private float overviewYaw = 0f;
    [Min(1f)] [SerializeField] private float overviewPadding = 1.08f;
    [Min(1f)] [SerializeField] private float fallbackDistance = 4f;

    [Header("Orbit Limits")] 
    [Range(5f, 85f)] [SerializeField] private float minimumPitch = 15f;
    [Range(15f, 89f)] [SerializeField] private float maximumPitch = 88f;
    [Min(1f)] [SerializeField] private float minimumDistance = 4f;
    [Min(1f)] [SerializeField] private float maximumDistance = 32f;

    [Header("Movement")]
    [Min(0.1f)] [SerializeField] private float panSpeed = 7f;
    [Min(1f)] [SerializeField] private float boostMultiplier = 2f;
    [Min(0.01f)] [SerializeField] private float panSmoothTime = 0.08f;

    [Header("Mouse")] 
    [Min(0.01f)] [SerializeField] private float yawSensitivity = 0.12f;
    [Min(0.01f)] [SerializeField] private float pitchSensitivity = 0.10f;
    [Min(0.001f)] [SerializeField] private float scrollZoomSensitivity = 2f;
    [Min(0.01f)] [SerializeField] private float orbitSmoothTime = 0.06f;
    [Min(0.01f)] [SerializeField] private float zoomSmoothTime = 0.10f;

    private Vector3 targetPivotPosition;
    private Vector3 pivotSmoothVelocity;

    private float currentYaw;
    private float targetYaw;
    private float yawSmoothVelocity;

    private float currentPitch;
    private float targetPitch;
    private float pitchSmoothVelocity;

    private float currentDistance;
    private float targetDistance;
    private float distanceSmoothVelocity;

    public Camera LayoutCamera => layoutCamera;

    public Vector3 PivotPosition => transform.position;

    public bool IsOrbiting => Mouse.current != null && Mouse.current.middleButton.isPressed && !IsPointerOverUI();
    
    public void RotateViewLeft()
    {
        RotateViewBy(-45f);
    }

    public void RotateViewRight()
    {
        RotateViewBy(45f);
    }

    private void RotateViewBy(float degrees)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f)
        {
            return;
        }

        targetYaw = Mathf.Repeat(targetYaw + degrees, 360f);
    }
    
    private void OnValidate()
    {
        maximumPitch = Mathf.Max(minimumPitch, maximumPitch);

        overviewPitch = Mathf.Clamp(overviewPitch, minimumPitch, maximumPitch);

        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);

        workingDistance = Mathf.Clamp(workingDistance, minimumDistance, maximumDistance);

        fallbackDistance = Mathf.Clamp(fallbackDistance, minimumDistance, maximumDistance);

        overviewPadding = Mathf.Max(1f, overviewPadding);

        panSmoothTime = Mathf.Max(0.01f, panSmoothTime);

        orbitSmoothTime = Mathf.Max(0.01f, orbitSmoothTime);

        zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
    }

    /*
     * Normal Store Design entry.
     *
     * Focus the local working zone, but do not
     * attempt to fit all of it on screen.
     */
    public void ActivateForWorkingBounds(Bounds targetBounds)
    {
        if (layoutCamera == null)
        {
            return;
        }

        gameObject.SetActive(true);

        SetImmediateState(GetBoundsPivot(targetBounds), overviewYaw, overviewPitch, workingDistance);
    }

    /*
     * Full-bounds activation remains available for
     * situations where seeing an entire target is
     * intentionally useful.
     */
    public void ActivateForBounds(Bounds targetBounds)
    {
        if (layoutCamera == null)
        {
            return;
        }

        gameObject.SetActive(true);

        SetImmediateState(GetBoundsPivot(targetBounds), overviewYaw, overviewPitch,
            CalculateFramingDistance(targetBounds));
    }

    public void ActivateFrom(Transform source)
    {
        if (layoutCamera == null)
        {
            return;
        }

        gameObject.SetActive(true);

        Vector3 sourcePosition = source != null ? source.position : transform.position;

        Vector3 pivot = new(sourcePosition.x, 0f, sourcePosition.z);

        float yaw = source != null ? source.eulerAngles.y : overviewYaw;

        SetImmediateState(pivot, yaw, overviewPitch, fallbackDistance);
    }

    /*
     * Smoothly return to a comfortable editing
     * distance around a local region.
     */
    public void FocusWorkingBounds(Bounds targetBounds)
    {
        targetPivotPosition = GetBoundsPivot(targetBounds);

        targetDistance = Mathf.Clamp(workingDistance, minimumDistance, maximumDistance);
    }

    public void FocusWorkingBoundsImmediate(Bounds targetBounds)
    {
        if (layoutCamera == null)
        {
            return;
        }

        /*
         * Keep the player's current viewing angle,
         * but move the workspace immediately.
         *
         * Purchase previews need the correct floor
         * visible before FurniturePlacementController
         * performs its first preview raycast.
         */
        SetImmediateState(GetBoundsPivot(targetBounds), currentYaw, currentPitch, workingDistance);
    }

    /*
     * Smooth target framing for future expansion
     * selection, jump-to-item tools, etc.
     */
    public void FrameBounds(Bounds targetBounds)
    {
        targetPivotPosition = GetBoundsPivot(targetBounds);

        targetDistance = CalculateFramingDistance(targetBounds);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        HandleMovement(deltaTime);

        HandleRotation();
        HandleKeyboardViewRotation();
        HandleZoom();

        SmoothCameraState(deltaTime);

        ApplyCameraOrbit();
    }
    
    private void HandleKeyboardViewRotation()
    {
        if (Keyboard.current == null || Time.timeScale <= 0f || IsPointerOverUI())
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            RotateViewLeft();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            RotateViewRight();
        }
    }

    private void HandleMovement(float deltaTime)
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1f;
        }

        input = input.normalized;

        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 forward = yawRotation * Vector3.forward;

        Vector3 right = yawRotation * Vector3.right;

        Vector3 movement = forward * input.y + right * input.x;

        movement.y = 0f;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float speed = panSpeed;

        bool boostHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        if (boostHeld)
        {
            speed *= boostMultiplier;
        }

        targetPivotPosition += movement * speed * deltaTime;

        /*
         * Store Design pivot stays on the floor
         * plane established by the current focus.
         */
        targetPivotPosition.y = transform.position.y;
    }

    private void HandleRotation()
    {
        if (!IsOrbiting || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        targetYaw += mouseDelta.x * yawSensitivity;

        targetPitch -= mouseDelta.y * pitchSensitivity;

        targetPitch = Mathf.Clamp(targetPitch, minimumPitch, maximumPitch);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null || IsPointerOverUI())
        {
            return;
        }

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        targetDistance -= scroll * scrollZoomSensitivity;

        targetDistance = Mathf.Clamp(targetDistance, minimumDistance, maximumDistance);
    }

    private void SmoothCameraState(float deltaTime)
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPivotPosition, ref pivotSmoothVelocity,
            Mathf.Max(0.01f, panSmoothTime), Mathf.Infinity, deltaTime);

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawSmoothVelocity,
            Mathf.Max(0.01f, orbitSmoothTime), Mathf.Infinity, deltaTime);

        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchSmoothVelocity,
            Mathf.Max(0.01f, orbitSmoothTime), Mathf.Infinity, deltaTime);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceSmoothVelocity,
            Mathf.Max(0.01f, zoomSmoothTime), Mathf.Infinity, deltaTime);
    }

    private void SetImmediateState(Vector3 pivot, float yaw, float pitch, float distance)
    {
        targetPivotPosition = pivot;

        transform.position = pivot;

        targetYaw = yaw;
        currentYaw = yaw;

        targetPitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

        currentPitch = targetPitch;

        targetDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);

        currentDistance = targetDistance;

        pivotSmoothVelocity = Vector3.zero;

        yawSmoothVelocity = 0f;
        pitchSmoothVelocity = 0f;
        distanceSmoothVelocity = 0f;

        ApplyCameraOrbit();
    }

    private float CalculateFramingDistance(Bounds targetBounds)
    {
        float horizontalRadius = new Vector2(targetBounds.extents.x, targetBounds.extents.z).magnitude;

        float verticalFovRadians = layoutCamera.fieldOfView * Mathf.Deg2Rad;

        float horizontalFovRadians =
            2f * Mathf.Atan(Mathf.Tan(verticalFovRadians * 0.5f) * Mathf.Max(0.1f, layoutCamera.aspect));

        float limitingHalfFov = Mathf.Min(verticalFovRadians, horizontalFovRadians) * 0.5f;

        float requiredDistance = horizontalRadius / Mathf.Max(0.1f, Mathf.Sin(limitingHalfFov));

        return Mathf.Clamp(requiredDistance * overviewPadding, minimumDistance, maximumDistance);
    }

    private void ApplyCameraOrbit()
    {
        if (layoutCamera == null)
        {
            return;
        }

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        float radians = currentPitch * Mathf.Deg2Rad;

        float height = Mathf.Sin(radians) * currentDistance;

        float backwardDistance = Mathf.Cos(radians) * currentDistance;

        layoutCamera.transform.localPosition = new Vector3(0f, height, -backwardDistance);

        layoutCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
    
    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null ||
            Mouse.current == null)
        {
            return false;
        }

        return EventSystem.current
            .IsPointerOverGameObject(
                Mouse.current.deviceId
            );
    }

    private static Vector3 GetBoundsPivot(Bounds bounds)
    {
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}