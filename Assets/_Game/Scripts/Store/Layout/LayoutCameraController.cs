using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LayoutCameraController : MonoBehaviour
{
    [Header("Camera")] 
    [SerializeField] private Camera layoutCamera;

    [Header("Overview")] 
    [Range(5f, 85f)] [SerializeField] private float overviewPitch = 62f;
    [SerializeField] private float overviewYaw = 0f;
    [Min(1f)] [SerializeField] private float overviewPadding = 1.08f;
    [Min(1f)] [SerializeField] private float fallbackDistance = 18f;

    [Header("Orbit Limits")] 
    [Range(5f, 85f)] [SerializeField] private float minimumPitch = 15f;
    [Range(15f, 89f)] [SerializeField] private float maximumPitch = 88f;
    [Min(1f)] [SerializeField] private float minimumDistance = 4f;
    [Min(1f)] [SerializeField] private float maximumDistance = 32f;

    [Header("Movement")] 
    [Min(0.1f)] [SerializeField] private float panSpeed = 7f;
    [Min(1f)] [SerializeField] private float boostMultiplier = 2f;

    [Header("Mouse")] 
    [Min(0.01f)] [SerializeField] private float yawSensitivity = 0.12f;
    [Min(0.01f)] [SerializeField] private float pitchSensitivity = 0.10f;
    [Min(0.001f)] [SerializeField] private float scrollZoomSensitivity = 0.035f;

    private float pitch;
    private float distance;

    public Camera LayoutCamera => layoutCamera;

    public bool IsOrbiting => Mouse.current != null && Mouse.current.middleButton.isPressed;

    private void OnValidate()
    {
        maximumPitch = Mathf.Max(minimumPitch, maximumPitch);

        overviewPitch = Mathf.Clamp(overviewPitch, minimumPitch, maximumPitch);

        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);

        fallbackDistance = Mathf.Clamp(fallbackDistance, minimumDistance, maximumDistance);

        overviewPadding = Mathf.Max(1f, overviewPadding);
    }

    public void ActivateForBounds(Bounds targetBounds)
    {
        if (layoutCamera == null)
            return;

        gameObject.SetActive(true);

        transform.position = new Vector3(targetBounds.center.x, targetBounds.max.y, targetBounds.center.z);

        transform.rotation = Quaternion.Euler(0f, overviewYaw, 0f);

        pitch = Mathf.Clamp(overviewPitch, minimumPitch, maximumPitch);

        distance = CalculateFramingDistance(targetBounds);

        ApplyCameraOrbit();
    }

    public void ActivateFrom(Transform source)
    {
        if (layoutCamera == null)
            return;

        gameObject.SetActive(true);

        Vector3 sourcePosition = source != null ? source.position : transform.position;

        transform.position = new Vector3(sourcePosition.x, 0f, sourcePosition.z);

        float yaw = source != null ? source.eulerAngles.y : overviewYaw;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        pitch = Mathf.Clamp(overviewPitch, minimumPitch, maximumPitch);

        distance = Mathf.Clamp(fallbackDistance, minimumDistance, maximumDistance);

        ApplyCameraOrbit();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        ApplyCameraOrbit();
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        input = input.normalized;

        Vector3 movement = transform.forward * input.y + transform.right * input.x;

        movement.y = 0f;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float speed = panSpeed;

        if (Keyboard.current.leftShiftKey.isPressed)
        {
            speed *= boostMultiplier;
        }

        transform.position += movement * speed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        if (!IsOrbiting)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        transform.Rotate(Vector3.up, mouseDelta.x * yawSensitivity, Space.World);

        pitch -= mouseDelta.y * pitchSensitivity;

        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        distance -= scroll * scrollZoomSensitivity;

        distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
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
            return;

        float radians = pitch * Mathf.Deg2Rad;

        float height = Mathf.Sin(radians) * distance;

        float backwardDistance = Mathf.Cos(radians) * distance;

        layoutCamera.transform.localPosition = new Vector3(0f, height, -backwardDistance);

        layoutCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}