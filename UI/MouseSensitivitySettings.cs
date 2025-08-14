using UnityEngine;
using UnityEngine.UI;

public class MouseSensitivitySettings : MonoBehaviour
{
    public static float YawSensitivity { get; private set; }
    public static float PitchSensitivity { get; private set; }

    public const float DefaultYaw = 20f;
    public const float DefaultPitch = 20f;

    private const string yawKey = "MouseYawSensitivity";
    private const string pitchKey = "MousePitchSensitivity";

    [SerializeField] private Slider yawSlider;
    [SerializeField] private Slider pitchSlider;

    private void Awake()
    {
        LoadSensitivity();

        yawSlider.minValue = 1f;
        yawSlider.maxValue = 40f;
        yawSlider.value = YawSensitivity;

        pitchSlider.minValue = 1f;
        pitchSlider.maxValue = 40f;
        pitchSlider.value = PitchSensitivity;

        yawSlider.onValueChanged.AddListener(SetYawSensitivity);
        pitchSlider.onValueChanged.AddListener(SetPitchSensitivity);
    }

    public static void LoadSensitivity()
    {
        YawSensitivity = PlayerPrefs.GetFloat(yawKey, DefaultYaw);
        PitchSensitivity = PlayerPrefs.GetFloat(pitchKey, DefaultPitch);
    }

    public static void SetYawSensitivity(float value)
    {
        YawSensitivity = value;
        PlayerPrefs.SetFloat(yawKey, value);
        PlayerPrefs.Save();
    }

    public static void SetPitchSensitivity(float value)
    {
        PitchSensitivity = value;
        PlayerPrefs.SetFloat(pitchKey, value);
        PlayerPrefs.Save();
    }

    public static void ResetSensitivity()
    {
        SetYawSensitivity(DefaultYaw);
        SetPitchSensitivity(DefaultPitch);
    }

    public void RefreshSliders()
    {
        yawSlider.SetValueWithoutNotify(YawSensitivity);
        pitchSlider.SetValueWithoutNotify(PitchSensitivity);
    }
}
