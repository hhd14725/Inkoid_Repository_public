using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;

public class WaitingRoomUI : MonoBehaviour, IWaitingRoomUI
{
    [field:SerializeField] public Transform playerListParent { get; private set; }
    [field: SerializeField] public GameObject playerEntryPrefab { get; private set; }
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private WaitingRoomChangeWeaponUI waitingRoomChangeWeaponUI;
    //[SerializeField] private Button ChangeTeamButton;

    [Header("맵 변경 UI")] [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private Image mapPreviewImage;

    public event Action<bool> OnReadyToggled;
    public event Action OnStartMatchClicked;
    public event Action<Map> OnMapChanged;

    private Stack<PlayerEntry> pool = new Stack<PlayerEntry>();
    private bool isReady = false;

    // WaitingRoomUI에 해당 클라이언트에서 생성한 PlayerEntry 오브젝트의 포톤뷰 ID
    public int entryPhotonViewID { get; set; }

    //강퇴이벤트
    public event Action<Player> OnKickPlayer;
    [SerializeField] private TMP_Text messageText;
    
    //게임 시작할때 잠깐 있다 시작하는거 보여주는 텍스트
    [SerializeField] public TextMeshProUGUI gameStartTimerText;

    // 이모티콘 버튼들
    [SerializeField] Button[] emoticonButtons;
    // 팀변경 전체 잠금상태
    private bool teamChangeLocked = false;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //if (playerListParent == null) //Debug.LogError("WaitingRoomUI: playerListParent 미할당", this);
        //if (playerEntryPrefab == null) //Debug.LogError("WaitingRoomUI: playerEntryPrefab 미할당", this);
        //if (readyButton == null) //Debug.LogError("WaitingRoomUI: readyButton 미할당", this);
        //if (startButton == null) //Debug.LogError("WaitingRoomUI: startButton 미할당", this);

        // 맵 드롭다운 항목 전체 참가자에 대해 초기화 > 방장이 맵 바꾸면 모든 참가자 클라이언트의 텍스트도 바뀌게끔
        InitMapSelectDropdown();

        // 호스트(마스터 클라이언트)는 자동 준비 상태이므로 준비 버튼 숨김.
        if (PhotonNetwork.IsMasterClient)
        {
            // 준비 버튼 비활성화 및 준비 상태
            readyButton.gameObject.SetActive(false);
            isReady = true;
            OnReadyToggled?.Invoke(true);
        }
        else
        {
            startButton.gameObject.SetActive(false); // 수정: 클라이언트는 Start 버튼 안보이게

            // 준비 토글 버튼
            readyButton.onClick.AddListener(() =>
            {
                isReady = !isReady;
                var label = readyButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = isReady ? "Cancel" : "Ready";
                OnReadyToggled?.Invoke(isReady);
                waitingRoomChangeWeaponUI.waitingRoomChangeWeaponUIButtons.OnReadyToggle(isReady);
            });

            // 방장이 아니면 맵 변경 드롭다운에 상호작용하지 못하게끔
            mapDropdown.interactable = false;
        }

        // 시작 버튼은 마스터 전용 > 마스터가 변경될 수 있기에 모든 클라에서 이벤트 등록해주기
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() =>
        {
            OnStartMatchClicked?.Invoke();
        });

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        messageText.text = "";
    }

    // private void OnEnable()
    // {
    //     OnReadyToggled += SetChangeButtonActive;
    // }
    //
    // private void OnDisable()
    // {
    //     OnReadyToggled -= SetChangeButtonActive;
    // }

    private void OnMapDropdownChanged(int idx)
    {
        var m = (Map)idx;
        RefreshMapPreview();
        OnMapChanged?.Invoke(m);
    }

    private void RefreshMapPreview()
    {
        var m = (Map)mapDropdown.value;
        mapPreviewImage.sprite = DataManager.Instance.MapPreviews[m];
    }

    public void SetReadyButtonActive(bool isActive)
    {
        if (readyButton == null) return;
        readyButton.gameObject.SetActive(isActive);
    }

    public void SetMap(Map map)
    {
        mapDropdown.value = (int)map;
        RefreshMapPreview();
    }

    public bool MapDropdownInteractable
    {
        set => mapDropdown.interactable = value;
    }

    public void ChangeHost(bool canStart)
    {
        // 방장으로 될 때 호출 > 방장이 될 때 필요한 로직은 여기에
        // 방장인지 아닌지에 따라 버튼 보이기/숨기기
        if (!PhotonNetwork.IsMasterClient)
        {
            startButton.gameObject.SetActive(false);
            return;
        }

        // 방장인 경우 버튼 보이기
        startButton.gameObject.SetActive(true);
        // 방장인 경우 맵 선택 드롭다운 활성화
        mapDropdown.interactable = true;
        // 호스트 넘겨받았다는 프로퍼티 설정 > 호스트 마크가 보이기 시작
        // 여기 의심 > 방 나갈 때 SetCustomProperties 설정?
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { CustomPropKey.IsHost, true } });

        // 준비 여부에 따라 클릭 가능 여부 설정
        startButton.interactable = canStart;
    }

    public void UpdatePlayerListDisplay(List<Player> players)
    {

        // 기존 엔트리 풀링 >> PlayerEntryPrefab에 포톤뷰를 추가함으로써 로컬에서 풀링할 필요가 없어짐 (8/8 유빈)
        PlayerEntry[] playerEntries = playerListParent.GetComponentsInChildren<PlayerEntry>();
        for (int i = 0; i < playerEntries.Length; i++)
        {
            try
            {
                // 바뀐 상태 업데이트
                playerEntries[i].Setup(players[i], waitingRoomChangeWeaponUI);
                playerEntries[i].OnKickClicked -= OnEntryKick;
                playerEntries[i].OnKickClicked += OnEntryKick;
            }
            catch
            {
                ////Debug.Log("플레이어가 나간 후 플레이어 리스트 동기화까지 지연으로 예외 발생");
            }
        }

        var entries = playerListParent.GetComponentsInChildren<PlayerEntry>();
        foreach (var e in entries)
            e.SetTeamChangeLocked(teamChangeLocked);

        // 이거 쓰니까 추방 버튼이 너무 깜빡이는데
        //foreach (Transform t in playerListParent)
        //{
        //    var e = t.GetComponent<PlayerEntry>();
        //    if (e != null)
        //    {
        //        e.gameObject.SetActive(false);
        //        pool.Push(e);
        //    }
        //    else
        //    {
        //        Destroy(t.gameObject);
        //    }
        //}
        //// 재사용 또는 새 생성
        //foreach (var p in players)
        //{
        //    PlayerEntry e;
        //    if (pool.Count > 0)
        //    {
        //        e = pool.Pop();
        //        e.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        // 여기서 각각 만들어주면 isMine이나 islocal도 의도대로 동작하지 않음

        //        var go = Instantiate(playerEntryPrefab, playerListParent);
        //        e = go.GetComponent<PlayerEntry>();
        //        //if (e == null)
        //            //Debug.LogError("playerEntryPrefab에 PlayerEntry 컴포넌트 없음", go);
        //    }

        //    e.transform.SetParent(playerListParent, false);
        //    e.transform.SetAsLastSibling();

        //    e.Setup(p, waitingRoomChangeWeaponUI);

        //    // 강퇴 이벤트 연결 
        //    e.OnKickClicked -= OnEntryKick;
        //    e.OnKickClicked += OnEntryKick;
        //}
    }

    //강퇴기능 추가
    public void OnEntryKick(Player kickplayer)
    {
        OnKickPlayer?.Invoke(kickplayer);
    }

    public void ShowMessage(string message)
    {
        StopAllCoroutines();
        StartCoroutine(DoShowMessage(message));
    }

    private IEnumerator DoShowMessage(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(5f);
        messageText.text = "";
    }

    public void OnExitClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            // 나가는 플레이어에 대해 RPC 큐에 쌓인 것 중 미실행 구문 제거
            PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);

            // 팀 밸런싱을 위해 등록한 키를 나가면서 제거 (추방도 OnExitClicked() 실행해서 제거하게끔 함) 8/8 유빈 수정
            // LeaveRoom() 내에서는 PhotonNetwork.CurrentRoom.CustomProperties가 Null이 뜨네요
            string key = NetworkManager.Instance.keyPrefix + PhotonNetwork.LocalPlayer.UserId;
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
            {
                PhotonNetwork.CurrentRoom.CustomProperties[key] = null;
            }
            //else
                //Debug.LogError("키가 맞지 않아 커스텀 프로퍼티에서 제거할 수 없었습니다.");

            PhotonNetwork.LeaveRoom(); // 방을 떠납니다
        }
    }

    void InitMapSelectDropdown()
    {
        // 맵 선택 드롭다운
        var names = new List<string>(Enum.GetNames(typeof(Map)));
        // 튜토리얼이 아닌 경우에만 선택에서 제거 >> CustomPropKey.IsTutorial 키가 없을 때, 튜토가 아님
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CustomPropKey.IsTutorial, out object isTutorial))
        {
            names.Remove(Map.TutorialScene.ToString());
        }

        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(names);
        mapDropdown.onValueChanged.AddListener(OnMapDropdownChanged);

        // 룸 프로퍼티에서 초기값 읽어오기
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CustomPropKey.MapName, out object mObj)
            && Enum.TryParse((string)mObj, out Map m0))
        {
            mapDropdown.value = (int)m0;
        }

        RefreshMapPreview();
    }

    // //레디 했다면 팀변경 버튼 비 활성화
    // private void SetChangeButtonActive(bool isActive)
    // {
    //     ChangeTeamButton.interactable = !isActive;
    // }
    //
    // //실제 팀 바꾸기 버튼 누를때 실행 할 함수
    // private void OnChangeTeamButton()
    // {
    //     foreach (var p in playerListParent.GetComponentsInChildren<PlayerEntry>())
    //     {
    //         //if(p._player.)
    //     }
    // }

    //방장이 시작키 누를시 상호작용 다 막아버림.
    public void LockForMatchStart()
    {
        readyButton.interactable = false;
        exitButton.interactable = false;
        startButton.interactable = false; // 수정: 클릭 직후 비활성화
        mapDropdown.interactable = false; // 맵 변경불가
        //ChangeTeamButton.interactable = false;
        foreach(Button button in emoticonButtons)
        {
            button.interactable = false;
        }
        waitingRoomChangeWeaponUI.waitingRoomChangeWeaponUIButtons.OnReadyToggle(true); // Start 버튼 누른 이후 방장도 무기 수정 불가

        teamChangeLocked = true;
        foreach (var entry in playerListParent.GetComponentsInChildren<PlayerEntry>())
            entry.SetTeamChangeLocked(true);
    }

    public void ReleaseLock()
    {
        // 매치 시작 중 잠금 해제
        readyButton.interactable = true;
        exitButton.interactable = true;
        mapDropdown.interactable = PhotonNetwork.IsMasterClient;
        startButton.interactable = PhotonNetwork.IsMasterClient && startButton.interactable;
        foreach (Button button in emoticonButtons)
        {
            button.interactable = true;
        }
        waitingRoomChangeWeaponUI.waitingRoomChangeWeaponUIButtons.OnReadyToggle(false);

        teamChangeLocked = false;
        foreach (var entry in playerListParent.GetComponentsInChildren<PlayerEntry>())
            entry.SetTeamChangeLocked(false);  // 레디 상태면 내부에서 알아서 false 처리됨
    }
}