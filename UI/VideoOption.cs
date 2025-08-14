using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoOption : MonoBehaviour
{
    [SerializeField] private Button ChangeScreenChaek; // 적용 버튼
    [SerializeField] private Toggle fullScreenToggle;   // 전체화면 토글
    [SerializeField] private TMP_Dropdown resolutionDropdown; // 해상도 드롭다운

    private FullScreenMode screenMode;
    private List<Resolution> resolutions = new List<Resolution>();
    private int resolutionNum;

    // 가장 많이 사용되는 해상도들만 허용 (16:9 기준)
    private readonly HashSet<(int width, int height)> commonResolutions = new HashSet<(int, int)>
    {
        (1920, 1080),
        (1600, 900),
        (1366, 768),
        (1280, 720)
    };

    void Awake()
    {
        screenMode = Screen.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; // 초기 설정
        fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggleChanged);
    }

    void Start()
    {
        InitUI();

        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            resolutionNum = index;
        });

        ChangeScreenChaek.onClick.AddListener(ApplyResolution);
    }

    void InitUI()
    {
        resolutions.Clear();
        resolutionDropdown.options.Clear();

        int defaultIndex = 0;
        int index = 0;

        foreach (var res in Screen.resolutions)
        {
            if ((int)res.refreshRateRatio.value != 60) continue; // 60Hz 필터
            if (!commonResolutions.Contains((res.width, res.height))) continue; // 상용 해상도만 허용

            resolutions.Add(res);

            string label = $"{res.width} x {res.height}  {(int)res.refreshRateRatio.value}Hz";
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(label));

            if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
            {
                defaultIndex = index;
                resolutionNum = index;
            }

            index++;
        }

        resolutionDropdown.value = defaultIndex;
        resolutionDropdown.RefreshShownValue();

        fullScreenToggle.isOn = Screen.fullScreen;
    }

    void OnFullScreenToggleChanged(bool isOn)
    {
        screenMode = isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        //Debug.Log($"전체화면 토글 변경됨: {screenMode}");
    }

    void ApplyResolution()
    {
        if (resolutionNum < 0 || resolutionNum >= resolutions.Count)
        {
            //Debug.LogError($"잘못된 인덱스 접근: {resolutionNum}");
            return;
        }

        Resolution selected = resolutions[resolutionNum];
        RefreshRate refreshRate = selected.refreshRateRatio;

        //Debug.Log($"해상도 적용: {selected.width}x{selected.height}, {screenMode}, {refreshRate.value}Hz");
        Screen.SetResolution(selected.width, selected.height, screenMode, refreshRate);
    }
}
