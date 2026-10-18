using System.Collections;
using UnityEngine;

public class CheckoutFeedback : MonoBehaviour
{
    [Header("Audio")] [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip scannerBeep;

    [SerializeField] private AudioClip moneyChime;

    [Header("Visual")] [SerializeField] private GameObject scannerLight;

    [Header("UI")] [SerializeField] private MoneyFeedbackUI moneyFeedbackUI;

    private Coroutine scannerRoutine;

    private void Awake()
    {
        if (scannerLight != null)
            scannerLight.SetActive(
                false
            );
    }

    public void PlayScanFeedback()
    {
        if (scannerRoutine != null)
            StopCoroutine(
                scannerRoutine
            );

        scannerRoutine =
            StartCoroutine(
                ScannerFeedbackRoutine()
            );
    }

    public void PlayPaymentFeedback(
        float amount
    )
    {
        if (audioSource != null &&
            moneyChime != null)
            audioSource.PlayOneShot(
                moneyChime,
                0.5f
            );

        if (moneyFeedbackUI != null)
            moneyFeedbackUI.ShowGain(
                amount
            );
    }

    /*
     * Kept for compatibility with any older
     * code that still expects one combined sale
     * feedback call.
     */
    public void PlaySaleFeedback(
        float amount
    )
    {
        PlayScanFeedback();

        PlayPaymentFeedback(
            amount
        );
    }

    private IEnumerator
        ScannerFeedbackRoutine()
    {
        if (audioSource != null &&
            scannerBeep != null)
            audioSource.PlayOneShot(
                scannerBeep,
                0.7f
            );

        if (scannerLight != null)
            scannerLight.SetActive(
                true
            );

        yield return new WaitForSeconds(
            0.10f
        );

        if (scannerLight != null)
            scannerLight.SetActive(
                false
            );

        scannerRoutine =
            null;
    }
}