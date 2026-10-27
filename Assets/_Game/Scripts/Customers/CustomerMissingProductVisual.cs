
using TMPro;
using UnityEngine;

// Presentation only: the customer's existing controller
// remains responsible for missing-product penalties.
[DisallowMultipleComponent]
public class CustomerMissingProductVisual : MonoBehaviour
{
    [Header("3D Text Above Customer")]
    [SerializeField] private TextMeshPro reactionText;

    [Header("Reaction Animation")]
    [Min(0.1f)]
    [SerializeField] private float visibleSeconds = 1.3f;

    [Min(0f)]
    [SerializeField] private float riseHeight = 0.08f;

    [Min(0.05f)]
    [SerializeField] private float fadeOutSeconds = 0.35f;

    [SerializeField] private Color reactionColor =
        new Color32(196, 107, 107, 255);

    private Vector3 restingLocalPosition;
    private Camera playerCamera;
    private float secondsRemaining;

    private void Awake()
    {
        if (reactionText == null)
        {
            Debug.LogWarning(
                "CustomerMissingProductVisual needs a 3D TextMeshPro reference.",
                this
            );
            return;
        }

        restingLocalPosition =
            reactionText.transform.localPosition;

        reactionText.gameObject.SetActive(false);
    }

    public void ShowReaction()
    {
        if (reactionText == null)
            return;

        secondsRemaining = visibleSeconds;

        reactionText.text = ":(";
        reactionText.color = reactionColor;

        reactionText.transform.localPosition =
            restingLocalPosition;

        reactionText.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (reactionText == null ||
            !reactionText.gameObject.activeSelf)
            return;

        secondsRemaining -= Time.deltaTime;

        if (secondsRemaining <= 0f)
        {
            reactionText.gameObject.SetActive(false);
            return;
        }

        float progress = 1f -
            secondsRemaining / Mathf.Max(0.1f, visibleSeconds);

        reactionText.transform.localPosition =
            restingLocalPosition +
            Vector3.up * (riseHeight * progress);

        Color color = reactionColor;

        color.a *= Mathf.Clamp01(
            secondsRemaining / fadeOutSeconds
        );

        reactionText.color = color;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
        {
            reactionText.transform.rotation =
                playerCamera.transform.rotation;
        }
    }

    private void OnDisable()
    {
        if (reactionText != null)
            reactionText.gameObject.SetActive(false);
    }
}
