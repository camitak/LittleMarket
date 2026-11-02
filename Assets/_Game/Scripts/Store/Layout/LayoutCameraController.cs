using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LayoutCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera layoutCamera;

    [Header("Orbit")]
    [Range(20f, 85f)]
    [SerializeField] private float startingPitch = 55f;

    [Range(20f, 85f)]
    [SerializeField] private float minimumPitch = 35f;

    [Range(20f, 85f)]
    [SerializeField] private float maximumPitch = 75f;

    [Min(1f)]
    [SerializeField] private float startingDistance = 10f;

    [Min(1f)]
    [SerializeField] private float minimumDistance = 5f;

    [Min(1f)]
    [SerializeField] private float maximumDistance = 18f;

    [Header("Movement")]
    [Min(0.1f)]
    [SerializeField] private float panSpeed = 7f;

    [Min(1f)]
    [SerializeField] private float boostMultiplier = 2f;

    [Header("Mouse")]
    [Min(0.01f)]
    [SerializeField] private float yawSensitivity = 0.12f;

    [Min(0.01f)]
    [SerializeField] private float pitchSensitivity = 0.10f;

    [Min(0.001f)]
    [SerializeField] private float scrollZoomSensitivity = 0.01f;

    private float pitch;
    private float distance;

    public Camera LayoutCamera =>
        layoutCamera;

    public void ActivateFrom(
        Transform source)
    {
        if (layoutCamera == null)
            return;

        Vector3 sourcePosition =
            source != null
                ? source.position
                : transform.position;

        transform.position =
            new Vector3(
                sourcePosition.x,
                0f,
                sourcePosition.z
            );

        float startingYaw =
            source != null
                ? source.eulerAngles.y
                : 0f;

        transform.rotation =
            Quaternion.Euler(
                0f,
                startingYaw,
                0f
            );

        pitch =
            Mathf.Clamp(
                startingPitch,
                minimumPitch,
                maximumPitch
            );

        distance =
            Mathf.Clamp(
                startingDistance,
                minimumDistance,
                maximumDistance
            );

        gameObject.SetActive(true);

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

        Vector2 input =
            Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        input =
            input.normalized;

        Vector3 movement =
            transform.forward * input.y +
            transform.right * input.x;

        movement.y = 0f;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float speed =
            panSpeed;

        if (Keyboard.current.leftShiftKey.isPressed)
        {
            speed *= boostMultiplier;
        }

        transform.position +=
            movement *
            speed *
            Time.deltaTime;
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        transform.Rotate(
            Vector3.up,
            mouseDelta.x * yawSensitivity,
            Space.World
        );

        pitch -=
            mouseDelta.y *
            pitchSensitivity;

        pitch =
            Mathf.Clamp(
                pitch,
                minimumPitch,
                maximumPitch
            );
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll
                .ReadValue()
                .y;

        distance -=
            scroll *
            scrollZoomSensitivity;

        distance =
            Mathf.Clamp(
                distance,
                minimumDistance,
                maximumDistance
            );
    }

    private void ApplyCameraOrbit()
    {
        if (layoutCamera == null)
            return;

        float radians =
            pitch *
            Mathf.Deg2Rad;

        float height =
            Mathf.Sin(radians) *
            distance;

        float backwardDistance =
            Mathf.Cos(radians) *
            distance;

        layoutCamera.transform.localPosition =
            new Vector3(
                0f,
                height,
                -backwardDistance
            );

        layoutCamera.transform.localRotation =
            Quaternion.Euler(
                pitch,
                0f,
                0f
            );
    }
}