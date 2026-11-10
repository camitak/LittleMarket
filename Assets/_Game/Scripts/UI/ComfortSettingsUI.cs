using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent] public class ComfortSettingsUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private PlayerComfortSettings comfortSettings;

    [Header("Panels")]
    [SerializeField] private GameObject pauseMainPanel;
    [SerializeField] private GameObject comfortPanel;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button resetDefaultsButton;

    [Header("Field of View")]
    [SerializeField] private Slider fieldOfViewSlider;
    [SerializeField] private TMP_Text fieldOfViewValueText;

    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityValueText;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(ResetDefaults);
        if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.AddListener(ChangeFieldOfView);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.AddListener(ChangeMouseSensitivity);

        if (comfortPanel != null) comfortPanel.SetActive(false);
    }

    public void Open()
    {
        if (isOpen || comfortSettings == null || !comfortSettings.IsReady || pauseMainPanel == null ||
            comfortPanel == null || fieldOfViewSlider == null || mouseSensitivitySlider == null)
        {
            return;
        }

        fieldOfViewSlider.minValue = PlayerComfortSettings.MinimumFieldOfView;
        fieldOfViewSlider.maxValue = PlayerComfortSettings.MaximumFieldOfView;
        fieldOfViewSlider.wholeNumbers = false;

        mouseSensitivitySlider.minValue = PlayerComfortSettings.MinimumMouseSensitivity;
        mouseSensitivitySlider.maxValue = PlayerComfortSettings.MaximumMouseSensitivity;
        mouseSensitivitySlider.wholeNumbers = false;

        fieldOfViewSlider.SetValueWithoutNotify(comfortSettings.FieldOfView);
        mouseSensitivitySlider.SetValueWithoutNotify(comfortSettings.MouseSensitivity);
        RefreshValueLabels();

        isOpen = true;
        pauseMainPanel.SetActive(false);
        comfortPanel.SetActive(true);
        comfortPanel.transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        comfortSettings?.SavePreferences();
        comfortPanel.SetActive(false);
        pauseMainPanel.SetActive(true);
    }

    private void ChangeFieldOfView(float value)
    {
        if (!isOpen || comfortSettings == null) return;

        comfortSettings.SetFieldOfView(value);
        RefreshValueLabels();
    }

    private void ChangeMouseSensitivity(float value)
    {
        if (!isOpen || comfortSettings == null) return;

        comfortSettings.SetMouseSensitivity(value);
        RefreshValueLabels();
    }

    private void ResetDefaults()
    {
        if (!isOpen || comfortSettings == null) return;

        comfortSettings.ResetToAuthoredDefaults();
        fieldOfViewSlider.SetValueWithoutNotify(comfortSettings.FieldOfView);
        mouseSensitivitySlider.SetValueWithoutNotify(comfortSettings.MouseSensitivity);
        RefreshValueLabels();
    }

    private void RefreshValueLabels()
    {
        if (comfortSettings == null) return;

        if (fieldOfViewValueText != null)
        {
            fieldOfViewValueText.text = $"{comfortSettings.FieldOfView:0.0}°";
        }

        if (mouseSensitivityValueText != null)
        {
            mouseSensitivityValueText.text = $"{comfortSettings.MouseSensitivity:0.000}";
        }
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (backButton != null) backButton.onClick.RemoveListener(Close);
        if (resetDefaultsButton != null) resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
        if (fieldOfViewSlider != null) fieldOfViewSlider.onValueChanged.RemoveListener(ChangeFieldOfView);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.RemoveListener(ChangeMouseSensitivity);
    }
}
