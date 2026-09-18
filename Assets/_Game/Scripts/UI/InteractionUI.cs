using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text interactionText;

    private void Awake()
    {
        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        interactionText.text = message;
        interactionText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        interactionText.gameObject.SetActive(false);
    }
}