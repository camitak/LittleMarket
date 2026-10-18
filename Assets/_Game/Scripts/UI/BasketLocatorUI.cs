using TMPro;
using UnityEngine;

public class BasketLocatorUI : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private Camera playerCamera;

    [SerializeField] private GameObject locatorRoot;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private TMP_Text locatorText;

    [Header("Behaviour")] [Min(0.5f)] [SerializeField]
    private float defaultDuration = 5f;

    [Min(0.01f)] [SerializeField] private float arrowSmoothTime = 0.12f;

    private RestockBasket targetBasket;
    private float remainingSeconds;
    private float currentArrowAngle;
    private float arrowAngularVelocity;

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

        if (targetBasket.IsHeld || targetBasket.IsDocked)
        {
            Hide();
            return;
        }

        remainingSeconds -= Time.unscaledDeltaTime;

        if (remainingSeconds <= 0f)
        {
            Hide();
            return;
        }

        UpdateLocator();
    }

    public void Show(RestockBasket basket, float duration = -1f)
    {
        if (basket == null) return;

        targetBasket = basket;
        remainingSeconds = duration > 0f ? duration : defaultDuration;

        if (locatorRoot != null) locatorRoot.SetActive(true);

        UpdateLocator();
    }

    public void Hide()
    {
        targetBasket = null;
        remainingSeconds = 0f;
        arrowAngularVelocity = 0f;

        if (locatorRoot != null && locatorRoot.activeSelf) locatorRoot.SetActive(false);
    }

    private void UpdateLocator()
    {
        if (targetBasket == null || playerCamera == null) return;

        var cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0f;

        var directionToBasket =
            targetBasket.transform.position - playerCamera.transform.position;

        directionToBasket.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f ||
            directionToBasket.sqrMagnitude < 0.001f)
            return;

        cameraForward.Normalize();
        directionToBasket.Normalize();

        var worldAngle = Vector3.SignedAngle(
            cameraForward,
            directionToBasket,
            Vector3.up
        );

        // The UI arrow starts pointing upward.
        // Negative converts Unity's world yaw direction
        // into the equivalent 2D UI rotation.
        var targetArrowAngle = -worldAngle;

        currentArrowAngle = Mathf.SmoothDampAngle(
            currentArrowAngle,
            targetArrowAngle,
            ref arrowAngularVelocity,
            arrowSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        if (arrowRect != null)
            arrowRect.localRotation =
                Quaternion.Euler(0f, 0f, currentArrowAngle);

        if (locatorText != null)
        {
            var distance = Vector3.Distance(
                playerCamera.transform.position,
                targetBasket.transform.position
            );

            locatorText.text = $"BASKET  •  {distance:0}m";
        }
    }
}