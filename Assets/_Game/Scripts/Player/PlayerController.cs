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

    private CharacterController characterController;

    private float verticalVelocity;

    private float cameraPitch = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector2 movementInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            movementInput.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            movementInput.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            movementInput.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            movementInput.x -= 1f;

        movementInput = movementInput.normalized;

        Vector3 movement =
            transform.forward * movementInput.y +
            transform.right * movementInput.x;

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        movement.y = verticalVelocity;

        characterController.Move(
            movement * moveSpeed * Time.deltaTime
        );
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // LEFT / RIGHT
        transform.Rotate(Vector3.up * mouseX);

        // UP / DOWN
        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -80f,
            80f
        );

        playerCamera.transform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}