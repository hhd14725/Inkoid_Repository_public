using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private WaitingRoomUI waitingRoomUI;

    public event Action<List<Player>> OnPlayerListChanged;
    public event Action<bool> OnStartButtonVisible;

    // 델리게이트 필드로 분리
    private Action<Player> onPlayerJoinedHandler;
    private Action<Player, Hashtable> onPlayerPropsHandler;
    private Action<Player> onPlayerLeftHandler;
    private Action<Player> onMasterSwitchedHandler;
    //private Action onAllPlayersReadyHandler;

    bool matchStarting = false;
    
    private Coroutine countdownCoroutine;

    void OnEnable()
    {
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        UIManager.Instance.RegisterWaitingRoomUI(waitingRoomUI, this);
        var nm = NetworkManager.Instance;

        // 1) 델리게이트 인스턴스 생성
        onPlayerJoinedHandler = player => RefreshPlayerList();
        onPlayerPropsHandler = (player, props) =>
        {
            RefreshPlayerList();
            UpdateStartButtonVisibility();
        };
        onPlayerLeftHandler = player => RefreshPlayerList();
        onMasterSwitchedHandler = newMaster => HandleMasterSwitched(newMaster);
        //onAllPlayersReadyHandler = () => OnStartButtonVisible?.Invoke(true);

        // 2) 같은 인스턴스로 구독
        nm.OnPlayerJoinedRoomEvent += onPlayerJoinedHandler;
        nm.OnPlayerPropertiesUpdatedEvent += onPlayerPropsHandler;
        nm.OnRoomPropertiesUpdatedEvent += OnRoomPropsChanged;
        nm.OnPlayerLeftRoomEvent += onPlayerLeftHandler;
        nm.OnMasterClientSwitchedEvent += onMasterSwitchedHandler;
        nm.OnRoomPropertiesUpdatedEvent += HandleRoomPropertiesUpdated;
        nm.OnSendMessageEvent += SendMessageToAll;

        //if (PhotonNetwork.IsMasterClient)
        //    nm.OnAllPlayersReadyEvent += onAllPlayersReadyHandler;

        // 초기 UI 세팅
        OnStartButtonVisible?.Invoke(false);
        RefreshPlayerList();
        UpdateStartButtonVisibility();

        //타이머 등록
        NetworkManager.Instance.OnGameStartedEvent += GameStartTimerTextChanger;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        var nm = NetworkManager.Instance;
        waitingRoomUI.OnMapChanged -= HandleMapChanged;
        waitingRoomUI.OnReadyToggled -= HandleReadyToggled;
        waitingRoomUI.OnStartMatchClicked -= HandleStartMatchClicked;
        waitingRoomUI.OnKickPlayer -= HandleKickPlayer;
        // 3) 동일한 인스턴스로 해제
        nm.OnPlayerJoinedRoomEvent -= onPlayerJoinedHandler;
        nm.OnPlayerPropertiesUpdatedEvent -= onPlayerPropsHandler;
        nm.OnRoomPropertiesUpdatedEvent -= OnRoomPropsChanged;
        nm.OnPlayerLeftRoomEvent -= onPlayerLeftHandler;
        nm.OnMasterClientSwitchedEvent -= onMasterSwitchedHandler;
        nm.OnRoomPropertiesUpdatedEvent -= HandleRoomPropertiesUpdated;
        nm.OnSendMessageEvent -= SendMessageToAll;

        //if (PhotonNetwork.IsMasterClient)
        //    nm.OnAllPlayersReadyEvent -= onAllPlayersReadyHandler;
        
        //타이머 해제
        NetworkManager.Instance.OnGameStartedEvent -= GameStartTimerTextChanger;
    }

    private void OnRoomPropsChanged(Hashtable props)
    {
        RefreshPlayerList();
    }

    // 강퇴 처리 
    public void HandleKickPlayer(Player kickPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        //PhotonNetwork.CloseConnection(kickPlayer); 이거는 pun2가 다른이들을 강제로 연결을 끊는것을 막아둔상태라 사용불가.
        string msg = $"{kickPlayer.NickName} 님이 강퇴되었습니다.";
        photonView.RPC(nameof(RPC_SendMessage), RpcTarget.All, msg);
        //강퇴시키면 플레이어는 스스로 나간다.
        photonView.RPC(nameof(RPC_ForceLeaveRoom), kickPlayer);
    }

    [PunRPC]
    void RPC_ForceLeaveRoom()
    {
        waitingRoomUI.OnExitClicked();
    }
    [PunRPC]
    void RPC_SendMessage(string message)
    {
        waitingRoomUI.ShowMessage(message);
    }

    // 호스트가 드롭다운을 변경했을 때
    public void HandleMapChanged(Map map)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { CustomPropKey.MapName, map.ToString() } }
        );
    }

    // 새 호스트가 지정됐을 때
    private void HandleMasterSwitched(Player newMaster)
    {
        if(SceneManager.GetActiveScene().name != "WaitingRoom") return;

        OnStartButtonVisible -= waitingRoomUI.ChangeHost;
        OnStartButtonVisible += waitingRoomUI.ChangeHost;
        RefreshPlayerList();
        UpdateStartButtonVisibility();
        var roomProps = new Hashtable { { CustomPropKey.MatchStartingLock, false }, { CustomPropKey.InGameFlag, false } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        PhotonNetwork.CurrentRoom.IsOpen = true;
        waitingRoomUI.ReleaseLock();

        if (PhotonNetwork.LocalPlayer == newMaster)
        {
            // 자동 ready
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { { CustomPropKey.IsReady, true } }
            );
            // Ready 버튼 숨기기
            waitingRoomUI.SetReadyButtonActive(false);
        }

        //맵드롭다운 권한
        waitingRoomUI.MapDropdownInteractable = (PhotonNetwork.LocalPlayer == newMaster);
    }

    public void HandleReadyToggled(bool isReady)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new Hashtable { { CustomPropKey.IsReady, isReady } }
        );
    }

    public void HandleStartMatchClicked()
    {
        // 수정: 호스트 여부 및 중복 방지
        if (!PhotonNetwork.IsMasterClient || matchStarting)
            return;

        PhotonNetwork.CurrentRoom.IsOpen = false; //start누를때 외부난입 못하도록 방 잠금

        // 매치 시작 중 플래그 올리기 --start버튼 누르면 클라이언트들이 다른 행동 못하도록 막기위한 기초세팅
        var props = new Hashtable { { CustomPropKey.MatchStartingLock, true } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        //해당 플래그가 있으면 UI에서 매치 시작중 막을것들을 막는다. 원격으로호출하는건 클라이언트가 HandleRoompropertiesUpdate에서 받는다. 
        //해당 부분은 오로지 맵변경을 호스트 로컬에서 확실히 하기위하여 직접 호출하는 부분이다.
        waitingRoomUI.LockForMatchStart();
        //해당 이벤트가 연결되어있으니 구독해제
        OnStartButtonVisible -= waitingRoomUI.ChangeHost;

        // 수정: 다시 한번 준비 상태 확인 (호스트 혼자 있으면 넘어감)
        bool onlyHost = PhotonNetwork.PlayerList.Length == 1;
        bool everyoneReady = PhotonNetwork.PlayerList
            .Where(p => !p.IsMasterClient)
            .All(p => p.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var v) && (bool)v);

        if (!onlyHost && !everyoneReady)
        {
            //Debug.LogWarning("아직 모두 준비되지 않았습니다.");
            return;
        }

        matchStarting = true; // 수정: 중복 Start 방지
        //OnStartButtonVisible?.Invoke(false); // 여기서 맵 드롭다운을 true 해버리니 주석처리, 추후 문제안되면 삭제
        NetworkManager.Instance.StartMatch();
    }

    private void RefreshPlayerList()
    {
        var list = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
        OnPlayerListChanged?.Invoke(list);
    }

    private void UpdateStartButtonVisibility()
    {
        // 수정: 항상 호출해서 버튼 누를 수 있는 상태만 관리
        if (!PhotonNetwork.IsMasterClient)
        {
            OnStartButtonVisible?.Invoke(false);
            return;
        }

        bool onlyHost = PhotonNetwork.PlayerList.Length == 1;
        bool everyoneReady = PhotonNetwork.PlayerList
            .Where(p => !p.IsMasterClient)
            .All(p => p.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var v) && (bool)v);

        OnStartButtonVisible?.Invoke(onlyHost || everyoneReady);
    }

    private void HandleRoomPropertiesUpdated(Hashtable props)
    {
        if (props.ContainsKey(CustomPropKey.MapName)
            && props[CustomPropKey.MapName] is string mStr
            && Enum.TryParse<Map>(mStr, out var map))
        {
            waitingRoomUI.SetMap(map);
        }

        if (props.TryGetValue(CustomPropKey.MatchStartingLock, out var M) && (bool)M)
        {
            waitingRoomUI.LockForMatchStart();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "WaitingRoom") return;

        // 방에 완전히 들어온 클라이언트만 처리
        if (!PhotonNetwork.InRoom) return;

        // 이제는 안전하게 SetCustomProperties
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new Hashtable { { CustomPropKey.IsReady, false } }
        );

        var newProps = new Hashtable { { CustomPropKey.InGameFlag, false } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);
        PhotonNetwork.CurrentRoom.IsOpen = true;


        var roomProps = new Hashtable { { CustomPropKey.MatchStartingLock, false } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        // 여기서 락 해제 호출
        waitingRoomUI.ReleaseLock();

        //맵 관련 초기화
        if (PhotonNetwork.CurrentRoom.CustomProperties
                .TryGetValue(CustomPropKey.MapName, out var mObj)
            && mObj is string mStr
            && Enum.TryParse<Map>(mStr, out var map))
        {
            waitingRoomUI.SetMap(map);
        }
        // 드롭다운 활성화 권한도 초기화
        waitingRoomUI.MapDropdownInteractable = PhotonNetwork.IsMasterClient;

        // 여기서 만들면 기존의 다른 방의 플레이어 엔트리들이 동기화되기 전에 만들어버림 >> 클라이언트끼리 엔트리 순서가 맞지 않음
        CreatePlayerEntry();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CustomPropKey.IsTutorial, out var isTutorialObj))
        {
            HandleStartMatchClicked();
        }
    }

    void SendMessageToAll(string message)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_SendMessage), RpcTarget.All, message);
    }

    void CreatePlayerEntry()
    {
        GameObject tmpEntry = PhotonNetwork.Instantiate(waitingRoomUI.playerEntryPrefab.name, Vector3.zero, Quaternion.identity);
        if (tmpEntry.TryGetComponent(out PhotonView pvEntry))
        {
            waitingRoomUI.entryPhotonViewID = pvEntry.ViewID;
            // PhotonNetwork.Instantiate에는 생성과 동시에 부모에게 집어넣어주는 기능이 없어서 RPC로 상속
            photonView.RPC(nameof(SetEntryParent_RPC), RpcTarget.AllBuffered, waitingRoomUI.entryPhotonViewID);
        }
    }

    [PunRPC]
    void SetEntryParent_RPC(int entryViewID)
    {
        PhotonView entryPV = PhotonView.Find(entryViewID);
        entryPV.transform.SetParent(waitingRoomUI.playerListParent);
        entryPV.transform.localPosition = Vector3.zero;
        entryPV.transform.localScale = Vector3.one;
    }

    //public void ForceShowStartButton()
    //{
    //    OnStartButtonVisible?.Invoke(true);
    //}

    private void GameStartTimerTextChanger(int num)
    {
        photonView.RPC(nameof(RPC_GameStartTimerTextChanger), RpcTarget.All, num);
    }
    
    [PunRPC]
    private void RPC_GameStartTimerTextChanger(int num)
    {
        // 만약 이미 실행 중인 코루틴이 있다면 중지시킵니다.
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        // 새로운 코루틴을 시작하고 참조를 저장합니다.
        countdownCoroutine = StartCoroutine(CountdownCoroutine(num));
    }

// 카운트다운을 처리하는 코루틴 메서드
    private IEnumerator CountdownCoroutine(int time)
    {
        waitingRoomUI.gameStartTimerText.gameObject.SetActive(true);

        int remainingTime = time;

        // remainingTime이 0보다 클 때까지 반복
        while (remainingTime > 0)
        {
            // 남은 시간을 텍스트로 표시
            waitingRoomUI.gameStartTimerText.text = $"게임 시작  {remainingTime}초 전 입니다!";

            // 1초 동안 대기
            yield return new WaitForSeconds(1f);

            // 남은 시간 1초 감소
            remainingTime--;
        }

        // 카운트다운 종료 후 텍스트 변경
        waitingRoomUI.gameStartTimerText.text = "게임 시작!";

        // 1초 후에 텍스트를 사라지게
        yield return new WaitForSeconds(1f);
        waitingRoomUI.gameStartTimerText.gameObject.SetActive(false);
        countdownCoroutine = null; // 코루틴이 끝났으므로 참조를 null로 설정
    }
}