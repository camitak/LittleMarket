using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyFeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text gainText;

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private RectTransform rectTransform;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.7f;

    [SerializeField] private float riseDistance = 45f;

    private Vector2 startPosition;

    private Coroutine currentAnimation;

    private void Awake()
    {
        startPosition =
            rectTransform.anchoredPosition;

        canvasGroup.alpha = 0f;
    }

    public void ShowGain(float amount)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation =
            StartCoroutine(
                AnimateGain(amount)
            );
    }

    private IEnumerator AnimateGain(float amount)
    {
        gainText.text =
            "+£" + amount.ToString("0.00");

        rectTransform.anchoredPosition =
            startPosition;

        rectTransform.localScale =
            Vector3.one * 0.85f;

        canvasGroup.alpha = 1f;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / animationDuration
                );

            rectTransform.anchoredPosition =
                startPosition
                + Vector2.up
                * riseDistance
                * t;

            float scale;

            if (t < 0.25f)
            {
                scale = Mathf.Lerp(
                    0.85f,
                    1.08f,
                    t / 0.25f
                );
            }
            else
            {
                scale = Mathf.Lerp(
                    1.08f,
                    1f,
                    (t - 0.25f) / 0.75f
                );
            }

            rectTransform.localScale =
                Vector3.one * scale;

            float fade =
                Mathf.InverseLerp(
                    0.35f,
                    1f,
                    t
                );

            canvasGroup.alpha =
                1f - fade;

            yield return null;
        }

        canvasGroup.alpha = 0f;

        rectTransform.localScale =
            Vector3.one;

        rectTransform.anchoredPosition =
            startPosition;

        currentAnimation = null;
    }
}