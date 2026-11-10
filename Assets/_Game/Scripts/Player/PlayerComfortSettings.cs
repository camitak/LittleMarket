using UnityEngine;

[DisallowMultipleComponent] public class PlayerComfortSettings : MonoBehaviour
{
    public const float MinimumFieldOfView = 65f;
    public const float MaximumFieldOfView = 95f;
    public const float MinimumMouseSensitivity = 0.02f;
    public const float MaximumMouseSensitivity = 0.18f;

    private const string FieldOfViewKey = "LittleMarket.Comfort.FieldOfView.v1";
    private const string MouseSensitivityKey = "LittleMarket.Comfort.MouseSensitivity.v1";

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;

    private float defaultFieldOfView;
    private float defaultMouseSensitivity;
    private bool isReady;
    private bool isDirty;

    public bool IsReady => isReady;
    public float FieldOfView => playerCamera != null ? playerCamera.fieldOfView : 0f;
    public float MouseSensitivity => playerController != null ? playerController.MouseSensitivity : 0f;
    public float DefaultFieldOfView => defaultFieldOfView;
    public float DefaultMouseSensitivity => defaultMouseSensitivity;

    private void Awake()
    {
        if (playerController == null || playerCamera == null)
        {
            Debug.LogError("Player comfort settings need a PlayerController and player Camera.", this);
            return;
        }

        defaultFieldOfView = playerCamera.fieldOfView;
        defaultMouseSensitivity = playerController.MouseSensitivity;

        if (PlayerPrefs.HasKey(FieldOfViewKey))
        {
            playerCamera.fieldOfView = Mathf.Clamp(
                PlayerPrefs.GetFloat(FieldOfViewKey), MinimumFieldOfView, MaximumFieldOfView);
        }

        if (PlayerPrefs.HasKey(MouseSensitivityKey))
        {
            playerController.SetMouseSensitivity(Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey),
                MinimumMouseSensitivity, MaximumMouseSensitivity));
        }

        isReady = true;
    }

    public void SetFieldOfView(float fieldOfView)
    {
        if (!isReady) return;

        float value = Mathf.Clamp(fieldOfView, MinimumFieldOfView, MaximumFieldOfView);
        if (Mathf.Approximately(playerCamera.fieldOfView, value)) return;

        playerCamera.fieldOfView = value;
        isDirty = true;
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        if (!isReady) return;

        float value = Mathf.Clamp(sensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
        if (Mathf.Approximately(playerController.MouseSensitivity, value)) return;

        playerController.SetMouseSensitivity(value);
        isDirty = true;
    }

    public void ResetToAuthoredDefaults()
    {
        if (!isReady) return;

        SetFieldOfView(defaultFieldOfView);
        SetMouseSensitivity(defaultMouseSensitivity);
    }

    public void SavePreferences()
    {
        if (!isReady || !isDirty) return;

        PlayerPrefs.SetFloat(FieldOfViewKey, FieldOfView);
        PlayerPrefs.SetFloat(MouseSensitivityKey, MouseSensitivity);
        PlayerPrefs.Save();
        isDirty = false;
    }

    private void OnApplicationQuit()
    {
        SavePreferences();
    }
}
