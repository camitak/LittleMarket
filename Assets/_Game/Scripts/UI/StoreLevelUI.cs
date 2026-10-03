using TMPro;
using UnityEngine;

public class StoreLevelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text storeLevelText;

    [Header("Store")]
    [SerializeField]
    private StoreLevelProgression
        storeLevelProgression;

    private void Update()
    {
        if (storeLevelText == null)
        {
            return;
        }

        if (storeLevelProgression == null)
        {
            storeLevelText.text =
                "STORE LV ?";

            return;
        }

        storeLevelText.text =
            storeLevelProgression
                .GetProgressText();
    }
}