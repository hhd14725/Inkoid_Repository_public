using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private LobbyUI lobbyUI;

    public event Action<List<RoomInfo>> OnRoomListUpdated;
    public event Action OnLobbyEntering;
    public event Action OnLobbyEntered;

    // 델리게이트 레퍼런스 저장.
    private Action _lobbyJoinedHandler;
    private Action<List<RoomInfo>> _roomListUpdatedHandler;
    private Action<short, string> _createFailedHandler;
    private Action<short, string> _joinFailedHandler;

    void OnEnable()
    {
        UIManager.Instance.RegisterLobbyUI(lobbyUI, this);
        var nm = NetworkManager.Instance;

        _lobbyJoinedHandler = () => OnLobbyEntered?.Invoke();
        _roomListUpdatedHandler = rooms => OnRoomListUpdated?.Invoke(rooms);
        //_createFailedHandler = (c, m) => { //Debug.LogError($"방 생성 실패: {m}"); OnLobbyEntered?.Invoke(); };
        //_joinFailedHandler = (c, m) => { //Debug.LogError($"방 참가 실패: {m}"); OnLobbyEntered?.Invoke(); };

        nm.OnLobbyJoinedEvent += _lobbyJoinedHandler;
        nm.OnRoomListUpdatedEvent += _roomListUpdatedHandler;
        //nm.OnCreateRoomFailedEvent += _createFailedHandler;
        //nm.OnJoinRoomFailedEvent += _joinFailedHandler;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Instance;
        nm.OnLobbyJoinedEvent -= _lobbyJoinedHandler;
        nm.OnRoomListUpdatedEvent -= _roomListUpdatedHandler;
        //nm.OnCreateRoomFailedEvent -= _createFailedHandler;
        //nm.OnJoinRoomFailedEvent -= _joinFailedHandler;
    }
    void Start()
    {
        // 씬에 진입하자마자 로비에 들어가서 최신 방 목록을 받습니다.
        if (PhotonNetwork.IsConnectedAndReady)
        {
            NetworkManager.Instance.JoinLobby();
        }
        else
        {
            // 아직 연결이 안 된 상태라면, 연결 완료(ConnectedToMasterServer) 이벤트를 받아서 호출
            PhotonNetwork.NetworkingClient.StateChanged += OnPhotonStateChanged;
        }
    }
    private void OnPhotonStateChanged(ClientState fromState, ClientState toState)
    {
        if (toState == ClientState.ConnectedToMasterServer)
        {
            NetworkManager.Instance.JoinLobby();
            PhotonNetwork.NetworkingClient.StateChanged -= OnPhotonStateChanged;
        }
    }

    public void HandleCreateRoom(string name, byte maxP, int time, Map map)
    {
        OnLobbyEntering?.Invoke();
        NetworkManager.Instance.CreateRoom(name, maxP, time, map);
    }

    public void HandleJoinRoom(string name)
    {
        OnLobbyEntering?.Invoke();
        // Photon의 연결 상태 확인하고 방 입장 코드 실행 >> 기존에 뜨던 JoinFail 에러가 사라질 것
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(name);
        }
    }
}