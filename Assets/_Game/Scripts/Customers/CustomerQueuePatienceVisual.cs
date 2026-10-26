
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomerQueuePatienceVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CustomerController customer;
    [SerializeField] private TextMeshPro patienceText;

    [Header("Visual Feedback")]
    [Min(0f)] [SerializeField] private float criticalPenalty = 15f;
    [Min(0f)] [SerializeField] private float bobHeight = 0.06f;
    [Min(0f)] [SerializeField] private float bobSpeed = 3f;

    [SerializeField] private Color warningColor =
        new Color32(246, 215, 122, 255);

    [SerializeField] private Color criticalColor =
        new Color32(196, 107, 107, 255);

    private Camera playerCamera;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (customer == null)
            customer = GetComponent<CustomerController>();

        if (patienceText == null)
            return;

        originalLocalPosition = patienceText.transform.localPosition;
        patienceText.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (customer == null || patienceText == null)
            return;

        float penalty = customer.CurrentCheckoutWaitPenalty;

        bool showWarning = customer.IsWaitingAtCheckout && penalty > 0f;

        if (patienceText.gameObject.activeSelf != showWarning)
            patienceText.gameObject.SetActive(showWarning);

        if (!showWarning)
            return;

        bool isCritical = penalty >= criticalPenalty;

        patienceText.text = isCritical ? "!!" : "!";
        patienceText.color = isCritical ? criticalColor : warningColor;

        patienceText.transform.localPosition =
            originalLocalPosition +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
            patienceText.transform.rotation = playerCamera.transform.rotation;
    }
}
