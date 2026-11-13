using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Mouse Look")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float mouseSensitivity = 0.15f;

    public float MouseSensitivity => mouseSensitivity;

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Max(0.001f, sensitivity);
    }

    private CharacterController characterController;

    private float verticalVelocity;

    private float cameraPitch = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        CaptureCursor();
    }

    private void Update()
    {
        HandleMovement();

        HandleMouseLook();
    }

    public void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = false;
    }

    public void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;

        Cursor.visible = true;
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null) return;

        var movementInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) movementInput.y += 1f;

        if (Keyboard.current.sKey.isPressed) movementInput.y -= 1f;

        if (Keyboard.current.dKey.isPressed) movementInput.x += 1f;

        if (Keyboard.current.aKey.isPressed) movementInput.x -= 1f;

        movementInput = movementInput.normalized;

        var movement = transform.forward * movementInput.y + transform.right * movementInput.x;

        if (characterController.isGrounded)
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        movement.y = verticalVelocity;

        characterController.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        var mouseDelta = Mouse.current.delta.ReadValue();

        var mouseX = mouseDelta.x * mouseSensitivity;

        var mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
    
    public void AlignLookToWorldDirection(Vector3 worldDirection)
    {
        if (playerCamera == null || worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 forward = worldDirection.normalized;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(forward, Vector3.up);

        if (horizontalForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(horizontalForward.normalized, Vector3.up);

        float desiredPitch = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        cameraPitch = Mathf.Clamp(desiredPitch, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        verticalVelocity = 0f;
    }
}
