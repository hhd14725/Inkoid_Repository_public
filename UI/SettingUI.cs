using Photon.Pun;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button ControllScreenButton;
    [SerializeField] private GameObject EachUI;
    [SerializeField] private Button SoundScreenButton;
    [SerializeField] private GameObject SoundUI;
    [SerializeField] private Button FovScreenButton;
    [SerializeField] private GameObject FovUI;
    [SerializeField] private Button ScreenChangeButton;
    [SerializeField] private GameObject ScreenChangeUI;
    [SerializeField] private Button CreditScreenButton;
    [SerializeField] private GameObject CreditUI;
    [SerializeField] private Button GetOutTheGameButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button FovReset;

    [Header("슬라이더")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        closeButton.onClick.AddListener(() =>
        {
            var escLoader = FindObjectOfType<EscPopupLoader>();
            if (escLoader != null) escLoader.Toggle();
            //GameObject watingroom =  GameObject.Find("WaitingRoomUI");
            //if (watingroom == null)
            //{
            //    Cursor.lockState = CursorLockMode.Locked;
            //    Cursor.visible = false;
            //}

            //gameObject.SetActive(false);
        });

        ControllScreenButton.onClick.AddListener(EachScreenOpen);
        SoundScreenButton.onClick.AddListener(SoundScreenOpen);
        FovScreenButton.onClick.AddListener(FovScreenOpen);
        CreditScreenButton.onClick.AddListener(CreditScreenOpen);
        GetOutTheGameButton.onClick.AddListener(GetOutTheGame);
        ScreenChangeButton.onClick.AddListener(ScreenChangeButtonClick);
        FovReset.onClick.AddListener(ResetMouseSensitivity);
    }

    private void Start()
    {
        bgmSlider.wholeNumbers = false;
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 0.1f;
        bgmSlider.onValueChanged.RemoveAllListeners();
        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBgmVolume);

        sfxSlider.wholeNumbers = false;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;
        sfxSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSfxVolume);
    }

    // private void OnEnable()
    // {
    //     Cursor.lockState = CursorLockMode.None;
    //     Cursor.visible = true;
    // }
    //
    // private void OnDisable()
    // {
    //     Cursor.lockState = CursorLockMode.Locked;
    //     Cursor.visible = false;
    // }

    public void EachScreenOpen()
    {
        EachUI.SetActive(true);
        SoundUI.SetActive(false);
        FovUI.SetActive(false);
        CreditUI.SetActive(false);
        ScreenChangeUI.SetActive(false);
    }

    public void SoundScreenOpen()
    {
        SoundUI.SetActive(true);
        EachUI.SetActive(false);
        FovUI.SetActive(false);
        CreditUI.SetActive(false);
        ScreenChangeUI.SetActive(false);

        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
    }

    public void FovScreenOpen()
    {
        FovUI.SetActive(true);
        SoundUI.SetActive(false);
        EachUI.SetActive(false);
        CreditUI.SetActive(false);
        ScreenChangeUI.SetActive(false);
    }

    public void CreditScreenOpen()
    {
        CreditUI.SetActive(true);
        EachUI.SetActive(false);
        SoundUI.SetActive(false);
        FovUI.SetActive(false);
        ScreenChangeUI.SetActive(false);
    }

    public void ScreenChangeButtonClick()
    {
        ScreenChangeUI.SetActive(true);
        CreditUI.SetActive(false);
        EachUI.SetActive(false);
        SoundUI.SetActive(false);
        FovUI.SetActive(false);
    }
    private void ResetMouseSensitivity()
    {
        MouseSensitivitySettings.ResetSensitivity(); // 값 초기화

        MouseSensitivitySettings found = FindObjectOfType<MouseSensitivitySettings>(); // 슬라이더 UI 갱신
        if (found != null)
        {
            found.RefreshSliders();
        }
    }

    private void GetOutTheGame()
    {
        string current = SceneManager.GetActiveScene().name;
        if (Enum.TryParse<Map>(current, out _)&& PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (current == "Lobby")
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
