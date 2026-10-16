using TMPro;
using UnityEngine;

public class BasketLocatorUI :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private GameObject locatorRoot;

    [SerializeField]
    private RectTransform locatorRect;

    [SerializeField]
    private RectTransform arrowRect;

    [SerializeField]
    private TMP_Text locatorText;

    [Header("Behaviour")]
    [Min(30f)]
    [SerializeField]
    private float edgePadding = 120f;

    [Min(0.5f)]
    [SerializeField]
    private float defaultDuration = 5f;

    [SerializeField]
    private Vector3 targetWorldOffset =
        new Vector3(
            0f,
            0.45f,
            0f
        );

    private RestockBasket targetBasket;

    private float remainingSeconds;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (targetBasket == null)
        {
            Hide();

            return;
        }

        /*
         * Once you physically find/pick up the
         * basket, or it reaches home, the locator
         * has finished its job.
         */
        if (targetBasket.IsHeld ||
            targetBasket.IsDocked)
        {
            Hide();

            return;
        }

        remainingSeconds -=
            Time.unscaledDeltaTime;

        if (remainingSeconds <= 0f)
        {
            Hide();

            return;
        }

        UpdateLocatorPosition();
    }

    public void Show(
        RestockBasket basket,
        float duration = -1f
    )
    {
        if (basket == null)
        {
            return;
        }

        targetBasket =
            basket;

        remainingSeconds =
            duration > 0f
                ? duration
                : defaultDuration;

        if (locatorRoot != null)
        {
            locatorRoot.SetActive(
                true
            );
        }

        UpdateLocatorPosition();
    }

    public void Hide()
    {
        targetBasket =
            null;

        remainingSeconds =
            0f;

        if (locatorRoot != null &&
            locatorRoot.activeSelf)
        {
            locatorRoot.SetActive(
                false
            );
        }
    }

    private void UpdateLocatorPosition()
    {
        if (targetBasket == null ||
            playerCamera == null ||
            locatorRect == null)
        {
            return;
        }

        Vector3 targetPosition =
            targetBasket.transform.position
            + targetWorldOffset;

        Vector3 viewportPosition =
            playerCamera.WorldToViewportPoint(
                targetPosition
            );

        /*
         * Convert target position into a direction
         * relative to the centre of the screen.
         */
        Vector2 direction =
            new Vector2(
                viewportPosition.x - 0.5f,
                viewportPosition.y - 0.5f
            );

        /*
         * Unity mirrors projected positions when
         * the target is behind the camera.
         *
         * Flip the direction so the arrow still
         * tells us which way to turn.
         */
        if (viewportPosition.z < 0f)
        {
            direction =
                -direction;
        }

        if (direction.sqrMagnitude
            < 0.0001f)
        {
            direction =
                Vector2.up;
        }

        direction.Normalize();

        float halfWidth =
            Mathf.Max(
                1f,
                Screen.width * 0.5f
                - edgePadding
            );

        float halfHeight =
            Mathf.Max(
                1f,
                Screen.height * 0.5f
                - edgePadding
            );

        float horizontalScale =
            Mathf.Abs(direction.x)
            > 0.0001f
                ? halfWidth
                  / Mathf.Abs(direction.x)
                : float.PositiveInfinity;

        float verticalScale =
            Mathf.Abs(direction.y)
            > 0.0001f
                ? halfHeight
                  / Mathf.Abs(direction.y)
                : float.PositiveInfinity;

        float edgeScale =
            Mathf.Min(
                horizontalScale,
                verticalScale
            );

        Vector2 screenCenter =
            new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );

        Vector2 screenPosition =
            screenCenter
            + direction * edgeScale;

        /*
         * GameUI is Screen Space - Overlay,
         * so screen coordinates can be assigned
         * directly.
         */
        locatorRect.position =
            screenPosition;

        if (arrowRect != null)
        {
            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                )
                * Mathf.Rad2Deg
                - 90f;

            arrowRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }

        if (locatorText != null)
        {
            float distance =
                Vector3.Distance(
                    playerCamera.transform.position,
                    targetBasket.transform.position
                );

            locatorText.text =
                "BASKET  •  "
                + distance.ToString("0")
                + "m";
        }
    }
}