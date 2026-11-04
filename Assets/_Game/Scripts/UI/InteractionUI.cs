using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionUI : MonoBehaviour
{
    [Header("Prompt")] 
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text interactionText;

    private void Awake()
    {
        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        if (interactionText == null)
        {
            return;
        }

        interactionText.text = message;

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
            return;
        }

        interactionText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
            return;
        }

        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}