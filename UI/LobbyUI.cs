using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class LobbyUI : MonoBehaviour, ILobbyUI
{
    [Header("방 생성")] [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private TMP_Dropdown matchTimeDropdown;
    [SerializeField] private Button createRoomButton;

    [Header("맵 선택")] [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private Image mapPreviewImage;

    [Header("대기방 목록")] [SerializeField] private Button refreshButton;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;

    [Header("설정")] [SerializeField] private Button SettingButton;
    [SerializeField] private Button SettingCloseButton;
    [SerializeField] private GameObject SettingPopup;

    [Header("연출")] [SerializeField] Image[] inkImages;

    [Header("튜토리얼")] [SerializeField] private Button TutorialPanelButton;
    [SerializeField] private Button TutorialSceneButton;
    //[SerializeField] private Button TutorialCloseButton;
    [SerializeField] private GameObject TutorialPanel;

    Sequence lobbyProduction;

    public event Action<string, byte, int, Map> OnCreateRoomClicked;
    public event Action<string> OnJoinRoomClicked;

    private List<int> maxOptions = new() { 6, 4, 2 };
    private List<int> durationOptions;

#if UNITY_EDITOR
    private int MIN_MATCH_DURATION_SEC = 10
        , MAX_MATCH_DURATION_SEC = 300
        , MATCH_DURATION_INTERVAL_SEC = 10;
#else
    private int MIN_MATCH_DURATION_SEC =120
        , MAX_MATCH_DURATION_SEC = 240
        , MATCH_DURATION_INTERVAL_SEC = 60;
#endif

    private bool tutorialBusy;

    void Awake()
    {
        SettingPopup.SetActive(false);
        SettingButton.onClick.AddListener(SettingPopupOpen);
        SettingCloseButton.onClick.AddListener(SettingPopupClose);

        // 맵 드롭다운 초기화 
        var options = new List<string>(Enum.GetNames(typeof(Map)));
        // 튜토리얼은 안보이게
        options.Remove(Map.TutorialScene.ToString());
        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(options);
        mapDropdown.onValueChanged.AddListener(_ => RefreshMapPreview());
        RefreshMapPreview();

        TutorialPanel.SetActive(false);
        TutorialPanelButton.onClick.AddListener(ShowTutorialPanel);
        //TutorialCloseButton.onClick.AddListener(HideTutorialPanel);

        if (TutorialSceneButton != null)                             // 추가: 시작 버튼 연결
            TutorialSceneButton.onClick.AddListener(OnClick_TutorialStart);


        // 최대 인원 설정 드롭다운 초기화
        maxPlayersDropdown.ClearOptions();
        maxPlayersDropdown.AddOptions(maxOptions.ConvertAll(x => x.ToString()));

        // 매치 시간 드롭다운 초기화
        durationOptions = new List<int>();
        var timeOptions = new List<string>();
        for (int t = MIN_MATCH_DURATION_SEC; t <= MAX_MATCH_DURATION_SEC; t += MATCH_DURATION_INTERVAL_SEC)
        {
            durationOptions.Add(t);
            timeOptions.Add($"{t / 60}:{(t % 60):D2}");
        }

        matchTimeDropdown.ClearOptions();
        matchTimeDropdown.AddOptions(timeOptions);

        createRoomButton.onClick.AddListener(() =>
        {
            string roomName = string.IsNullOrEmpty(roomNameInputField.text)
                ? $"Room{UnityEngine.Random.Range(1000, 9999)}"
                : roomNameInputField.text;
            byte maxPlayers = (byte)maxOptions[maxPlayersDropdown.value];
            int matchTime = durationOptions[matchTimeDropdown.value];
            var selectedMap = (Map)mapDropdown.value;
            OnCreateRoomClicked?.Invoke(roomName, maxPlayers, matchTime, selectedMap);
        });

        refreshButton.onClick.AddListener(() =>
            NetworkManager.Instance.JoinLobby()
        );
    }

    private void Start()
    {
        // 타이틀 씬에서 뿌린 물감이 사라지면서 로비 UI가 드러나게
        lobbyProduction = DOTween.Sequence();
        for (int i = 0; i < inkImages.Length; i++)
        {
            lobbyProduction.Join(inkImages[i].DOFade(0, 0.3f).SetDelay(i * 0.1f));
        }
    }

    // 맵 프리뷰 이미지 갱신 메서드 
    private void RefreshMapPreview()
    {
        var m = (Map)mapDropdown.value;
        mapPreviewImage.sprite = DataManager.Instance.MapPreviews[m];
    }

    //public void CreateRoomPopup()
    //{        
    //    createRoomPopupPanel.SetActive(true);
    //}
    //public void CreateRoomPopupClose()
    //{
    //    createRoomPopupPanel.SetActive(false);
    //}
    public void SettingPopupOpen()
    {
        SettingPopup.SetActive(true);
    }

    public void SettingPopupClose()
    {
        SettingPopup.SetActive(false);
    }

    public void ShowTutorialPanel()
    {
        TutorialPanel.SetActive(true);
    }

    public void HideTutorialPanel()
    {
        TutorialPanel.SetActive(false);
    }

    public void DisplayRoomList(List<RoomInfo> rooms)
    {
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        foreach (var room in rooms)
        {
            if (!room.IsVisible || room.RemovedFromList) continue;
            bool inGame = room.CustomProperties.TryGetValue(
                CustomPropKey.InGameFlag, out var ig
            ) && (bool)ig;

            var entry = Instantiate(roomListItemPrefab, roomListContent);
            var label = entry.GetComponentInChildren<TMP_Text>();
            var button = entry.GetComponentInChildren<Button>();
            ColorUtility.TryParseHtmlString("#D5D5D5", out Color disabledButtonColor);
            ColorUtility.TryParseHtmlString("#004562", out Color availButtonColor);
            button.GetComponent<Image>().color = inGame
                ? disabledButtonColor
                : availButtonColor;

            var clickButton = entry.transform.Find("ClickArrowImage")?.GetComponent<Image>();
            ColorUtility.TryParseHtmlString("#9D9D9D", out Color disableArrowColor);
            ColorUtility.TryParseHtmlString("#DFFFFE", out Color availArrowColor);

            clickButton.color = inGame
                ? disableArrowColor
                : availArrowColor;


            ColorUtility.TryParseHtmlString("#989898", out Color FontColor);
            ColorUtility.TryParseHtmlString("#77CBFF", out Color FontOriginalColor);
            label.text = inGame
                ? $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers}) [Playing]"
                : $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";

            label.color = inGame
                ? FontColor
                : FontOriginalColor;

            // 맵 이름
            var mapNameText = entry.transform.Find("MapNameText")?.GetComponent<TMP_Text>();

            mapNameText.color = inGame
                ? FontColor
                : FontOriginalColor;

            // 미리보기 이미지
            var previewImg = entry.transform.Find("MapPreviewImage")?.GetComponent<Image>();

            // 맵 프로퍼티로부터 채우기
            if (room.CustomProperties.TryGetValue(CustomPropKey.MapName, out var mObj)
                && mObj is string mapNameStr
                && Enum.TryParse<Map>(mapNameStr, out var map))
            {
                if (mapNameText != null) mapNameText.text = map.ToString();
                if (previewImg != null) previewImg.sprite = DataManager.Instance.MapPreviews[map];
            }


            button.interactable = !inGame;
            if (!inGame)
                button.onClick.AddListener(() =>
                    OnJoinRoomClicked?.Invoke(room.Name)
                );
        }
    }

    public void SetLobbyButtonsInteractable(bool canCreate, bool canRefresh)
    {
        createRoomButton.interactable = canCreate;
        refreshButton.interactable = canRefresh;
    }

    private void OnClick_TutorialStart() // 추가
    {
        if (tutorialBusy) return;

        if (!Photon.Pun.PhotonNetwork.IsConnectedAndReady)
        {
            //Debug.LogWarning("Photon 연결 대기 중입니다.");
            return;
        }

        tutorialBusy = true;
        if (TutorialSceneButton != null) TutorialSceneButton.interactable = false;

        // 닉네임 비었을 때 기본값
        if (string.IsNullOrEmpty(Photon.Pun.PhotonNetwork.NickName))
            Photon.Pun.PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);

        // NetworkManager에 구현된 튜토리얼 방 생성 메서드 호출
        NetworkManager.Instance.CreateTutorialRoom();
    }
}