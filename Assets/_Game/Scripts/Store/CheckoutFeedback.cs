using System.Collections;
using UnityEngine;

public class CheckoutFeedback : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip scannerBeep;

    [SerializeField] private AudioClip moneyChime;

    [Header("Visual")]
    [SerializeField] private GameObject scannerLight;

    [Header("UI")]
    [SerializeField] private MoneyFeedbackUI moneyFeedbackUI;

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (scannerLight != null)
        {
            scannerLight.SetActive(false);
        }
    }

    public void PlaySaleFeedback(float amount)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine =
            StartCoroutine(
                SaleFeedbackRoutine(amount)
            );
    }

    private IEnumerator SaleFeedbackRoutine(float amount)
    {
        if (audioSource != null &&
            scannerBeep != null)
        {
            audioSource.PlayOneShot(
                scannerBeep,
                0.7f
            );
        }

        if (scannerLight != null)
        {
            scannerLight.SetActive(true);
        }

        yield return new WaitForSeconds(0.10f);

        if (scannerLight != null)
        {
            scannerLight.SetActive(false);
        }

        if (audioSource != null &&
            moneyChime != null)
        {
            audioSource.PlayOneShot(
                moneyChime,
                0.5f
            );
        }

        if (moneyFeedbackUI != null)
        {
            moneyFeedbackUI.ShowGain(amount);
        }

        feedbackRoutine = null;
    }
}