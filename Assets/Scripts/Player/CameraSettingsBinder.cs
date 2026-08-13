using Game.UI;
using UnityEngine;

/// <summary>
/// Carries the Game-tab settings (invert, sensitivity, acceleration, shake) and the FOV from
/// <see cref="SettingsService"/> onto the player.
///
/// It exists so the settings UI never has to know about gameplay classes and gameplay never has to
/// read PlayerPrefs: the service raises one event, this component translates it. Put it on the same
/// object as the controller.
/// </summary>
[AddComponentMenu("Player/Camera Settings Binder")]
[DisallowMultipleComponent]
public class CameraSettingsBinder : MonoBehaviour
{
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private FirstPersonCameraFeel cameraFeel;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (cameraFeel == null) cameraFeel = GetComponentInChildren<FirstPersonCameraFeel>();
    }

    private void OnEnable()
    {
        SettingsService settings = SettingsService.Instance;
        if (settings != null) settings.OnSettingsChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        SettingsService settings = SettingsService.Instance;
        if (settings != null) settings.OnSettingsChanged -= Apply;
    }

    public void Apply()
    {
        SettingsService settings = SettingsService.Instance;
        if (settings == null) return;

        if (controller != null)
        {
            controller.invertX = settings.GetInvertX();
            controller.invertY = settings.GetInvertY();
            controller.lookSensitivityX = settings.GetSensitivityX();
            controller.lookSensitivityY = settings.GetSensitivityY();
            controller.lookAcceleration = settings.GetCameraAcceleration();
        }

        if (cameraFeel != null)
        {
            cameraFeel.shakeScale = settings.GetCameraShake();
            cameraFeel.baseFieldOfView = settings.GetFov();
        }
    }
}
