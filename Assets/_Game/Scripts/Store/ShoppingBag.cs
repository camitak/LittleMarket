using System.Collections;
using UnityEngine;

public class ShoppingBag : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    [Header("Scan Feedback")]
    [Min(0f)]
    [SerializeField] private float pulseAmount = 0.06f;

    [Min(0.05f)]
    [SerializeField] private float pulseDuration = 0.18f;

    private Vector3 baseVisualScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        if (visualRoot != null)
        {
            baseVisualScale = visualRoot.localScale;
        }
    }

    public void RegisterScannedItem()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        visualRoot.localScale = baseVisualScale;
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    public void AttachToCustomer(Transform carryPoint)
    {
        if (carryPoint == null)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (visualRoot != null)
        {
            visualRoot.localScale = baseVisualScale;
        }

        transform.SetParent(carryPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private IEnumerator PulseRoutine()
    {
        float duration = Mathf.Max(0.05f, pulseDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI) * pulseAmount;

            visualRoot.localScale =
                baseVisualScale * (1f + pulse);

            yield return null;
        }

        visualRoot.localScale = baseVisualScale;
        pulseRoutine = null;
    }
}