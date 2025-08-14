using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // 플레이어 정보를 해시테이블에 저장하는 키의 접두어 keyPrefix + playerID, 접두어를 통해 해당 키가 팀 정보임을 판정(모두 가져올 때 해당 키워드 포함)
    readonly public string keyPrefix = "team_";

    public static NetworkManager Instance { get; private set; }

    // 이벤트
    public event System.Action OnConnectedEvent;
    public event System.Action<DisconnectCause> OnDisconnectedEvent;
    public event System.Action OnLobbyJoinedEvent;
    public event System.Action<List<RoomInfo>> OnRoomListUpdatedEvent;
    public event System.Action<short, string> OnCreateRoomFailedEvent;
    public event System.Action<short, string> OnJoinRoomFailedEvent;
    public event System.Action<Player> OnPlayerJoinedRoomEvent;
    public event System.Action<Player, Hashtable> OnPlayerPropertiesUpdatedEvent;
    public  System.Action<Player> AfterOnPlayerPropertiesUpdatedEvent;
    public event System.Action OnAllPlayersReadyEvent;
    
    public System.Action OnAllPlayersInGameReadyEvent;
    
    public System.Action<int> OnGameStartedEvent;

    public event System.Action<Player> OnPlayerLeftRoomEvent;
    public event System.Action<Player> OnMasterClientSwitchedEvent;

    public event Action<Hashtable> OnRoomPropertiesUpdatedEvent;

    public event Action<string> OnSendMessageEvent;

    private int gameStartTime = 3;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // 마스터 서버 연결
    public void ConnectToMaster(string steamId, string authTicketHex)
    {
        var authValues = new AuthenticationValues(steamId);
        authValues.AddAuthParameter("ticket", authTicketHex);
        PhotonNetwork.AuthValues = authValues;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() => OnConnectedEvent?.Invoke();
    public override void OnDisconnected(DisconnectCause cause) => OnDisconnectedEvent?.Invoke(cause);

    // 로비 입장
    public void JoinLobby()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            //Debug.LogWarning("로비에 들어갈 수 없는 상태입니다. 연결을 기다립니다.");
            return;
        }
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby() => OnLobbyJoinedEvent?.Invoke();
    public override void OnRoomListUpdate(List<RoomInfo> roomList) => OnRoomListUpdatedEvent?.Invoke(roomList);

    // 방 생성
    public void CreateRoom(string roomName, byte maxPlayers, int matchTimeSeconds, Map map)
    {
        var options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = new Hashtable 
            { 
                { CustomPropKey.MatchTimeSeconds, matchTimeSeconds }, 
                { CustomPropKey.InGameFlag, false }, 
                { CustomPropKey.MapName, map.ToString() },
                { CustomPropKey.ReturnToWaitingRoomTime, 10 },
            },
            CustomRoomPropertiesForLobby = new[] { CustomPropKey.MatchTimeSeconds, CustomPropKey.InGameFlag, CustomPropKey.MapName }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }
    public override void OnCreateRoomFailed(short returnCode, string message) => OnCreateRoomFailedEvent?.Invoke(returnCode, message);
    public override void OnJoinRoomFailed(short returnCode, string message) => OnJoinRoomFailedEvent?.Invoke(returnCode, message);

    // 방 입장 (호스트·참가자 공통)
    public override void OnJoinedRoom()
    {
        //byte colorA = 0, colorB = 0;
        //// 색상 동기화
        //var roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        //if (roomProps.TryGetValue(CustomPropKey.TeamColorA, out var aObj) &&
        //    roomProps.TryGetValue(CustomPropKey.TeamColorB, out var bObj))
        //{
        //    colorA = (byte)aObj;
        //    colorB = (byte)bObj;
        //    DataManager.Instance.InitTeamColors(colorA, colorB);
        //}
        
        // 초기 커스텀 프로퍼티 설정
        var props = new Hashtable
        {
            { CustomPropKey.Sequence,           PhotonNetwork.LocalPlayer.ActorNumber },
            { CustomPropKey.PlayerId,           PhotonNetwork.LocalPlayer.UserId },
            { CustomPropKey.Nickname,           PhotonNetwork.LocalPlayer.NickName },
            { CustomPropKey.TeamId,             (byte)0 },
            //{ CustomPropKey.TeamColorA,         colorA },
            //{ CustomPropKey.TeamColorB,         colorB },
            { CustomPropKey.CharacterSelection, 0 },
            { CustomPropKey.WeaponSelection,    (int)PrefabIndex_Weapon.Shooter },
            { CustomPropKey.IsReady,            false },
            { CustomPropKey.InGameFlag,         false },
            { CustomPropKey.InGameStartReady,   false },
            { CustomPropKey.IsHost,             false },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        //// 방 속성에서 팀 매핑 정리
        var roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        //// 나간 플레이어의 팀 데이터를 룸 프로퍼티에서 빼주기 
        //>> 누군가가 들어오지 않으면 키가 초기화되지 않아 에러 
        //>> 나가기 버튼(강퇴도 포함) 메서드에 해당 기능 이동하여 논리 에러 해결
        //var stale = roomProps.Keys
        //    .Cast<string>()
        //    .Where(k => k.StartsWith(keyPrefix))
        //    .Where(k => !PhotonNetwork.PlayerList.Any(p => keyPrefix + p.UserId == k))
        //    .ToList();
        //foreach (var k in stale) roomProps.Remove(k);
        //PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);


        // 내 매핑 결정
        string myKey = keyPrefix + PhotonNetwork.LocalPlayer.UserId;
        byte team;
        if (roomProps.TryGetValue(myKey, out object value))
        {
            team = Convert.ToByte(roomProps[myKey]);
        }
        else
        {
            // 첫 할당: 호스트=A(0), 그 외 균형 할당
            if (PhotonNetwork.LocalPlayer.IsMasterClient) team = 0;
            else
            {
                int cntA = PhotonNetwork.PlayerList.Count(p =>
                    p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out var t) && (byte)t == 0);
                int cntB = PhotonNetwork.PlayerList.Count(p =>
                    p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out var t) && (byte)t == 1);
                team = (byte)(cntA <= cntB ? 0 : 1);
            }
            roomProps[myKey] = team;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
        // 플레이어 속성으로 반영
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new Hashtable { { CustomPropKey.TeamId, team } }
        );
        //Debug.Log($"[OnJoinedRoom] Assigned Team {(team == 0 ? "A" : "B")} for {PhotonNetwork.LocalPlayer.NickName}");

        // UI 업데이트용 이벤트
        OnPlayerJoinedRoomEvent?.Invoke(PhotonNetwork.LocalPlayer);
        //Debug.Log($"[OnJoinedRoom] LocalPlayer.CustomProperties: " +
        //  $"{PhotonNetwork.LocalPlayer.CustomProperties.ToStringFull()}");
        // WaitingRoom 씬으로 전환
        PhotonNetwork.LoadLevel("WaitingRoom");
    }

    // 새로운 플레이어 입장
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        //// 마스터 클라이언트가 참가한 플레이어의 팀 프로퍼티 초기화
        //if (PhotonNetwork.IsMasterClient)
        //{
        //    var teamProps = new Hashtable
        //    {
        //        { CustomPropKey.TeamId,    (byte)0 },
        //        { CustomPropKey.IsHost,    false },
        //    };
        //    newPlayer.SetCustomProperties(teamProps);
        //}
        OnPlayerJoinedRoomEvent?.Invoke(newPlayer);
        //Debug.Log($"[OnPlayerEnteredRoom] {newPlayer.NickName} joined with props: " +
         // $"{newPlayer.CustomProperties.ToStringFull()}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
    }

    // 플레이어 프로퍼티 변경.
    public override void OnPlayerPropertiesUpdate(Player player, Hashtable changed)
    {
        OnPlayerPropertiesUpdatedEvent += OnPlayerPropertiesUpdatedEventInvoke;
        OnPlayerPropertiesUpdatedEvent?.Invoke(player, changed);
        OnPlayerPropertiesUpdatedEvent -= OnPlayerPropertiesUpdatedEventInvoke;

        if (changed.ContainsKey(CustomPropKey.TeamColorA) &&
            changed.ContainsKey(CustomPropKey.TeamColorB))
        {
            byte colorA = (byte)changed[CustomPropKey.TeamColorA];
            byte colorB = (byte)changed[CustomPropKey.TeamColorB];

            // 모든 클라이언트에서 적용
            DataManager.Instance.InitTeamColors(colorA, colorB);
            ////Debug.Log($"[TeamColor Update] A: {colorA}, B: {colorB}");
        }

        // 모든 비마스터 플레이어가 준비했으면 매치 시작
        if (changed.ContainsKey(CustomPropKey.IsReady))
        {
            var others = PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient);
            bool everyoneReady = !others.Any()|| others.All(p => p.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var val)&& val is bool b && b);

            if (everyoneReady)
                OnAllPlayersReadyEvent?.Invoke();
        }
        ////Debug.Log($"[OnPlayerPropertiesUpdate] {player.NickName} changed props: " +$"{changed.ToStringFull()}");
        
        //InGameStartReady가 바뀌면 발동
        if (changed.ContainsKey(CustomPropKey.InGameStartReady))
        {
            var others = PhotonNetwork.PlayerList;
            bool everyoneReady = !others.Any() || others.All(p =>
                p.CustomProperties.TryGetValue(CustomPropKey.InGameStartReady, out var val)
                && val is bool b && b);

            // 모든 플레이어가 인게임에서 준비됬으면 게임 스타트 이벤트 발동
            if (everyoneReady)
            {
                //Debug.Log("플레이어 인게임 준비 확인!");
                OnAllPlayersInGameReadyEvent?.Invoke();
            }
        }
    }
    //마스터클라이언트 변경시 start버튼 위임 로직 받기용
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        OnMasterClientSwitchedEvent?.Invoke(newMasterClient);
    }

    // 매치 시작 요청 (마스터)
    public void StartMatch()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // 팀 밸런싱
        TeamBalancing();
        // 최종으로 맞춰진 팀에 대해 랜덤한 색상 분배
        TeamColorSetting();
        StartCoroutine(StartMatchWithDelay());
    }

    private IEnumerator StartMatchWithDelay()
    {
        //  게임씬 카운트다운 길이(초) 
        const int inGameCountdownSeconds = 5; // GameStartTimerUI.NumSprites.Count 와 반드시 동일하기

        // 씬 로드 전에 시작시각들을 룸 프로퍼티에 박아둠 
        double buffer = 2.0f; // 씬 로드/스폰 지연 흡수용
        double t0 = PhotonNetwork.Time + gameStartTime + buffer + inGameCountdownSeconds; // 실제 게임 시작 시각
        double pre = t0 - inGameCountdownSeconds;                                          // 스프라이트 카운트다운 시작 시각

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable {{ CustomPropKey.PreMatchStartTime, pre },{ CustomPropKey.MatchStartTime,    t0  }});

        OnGameStartedEvent?.Invoke(gameStartTime);
        yield return new WaitForSeconds(gameStartTime);

        var newProps = new Hashtable { { CustomPropKey.InGameFlag, true }};
        PhotonNetwork.CurrentRoom.SetCustomProperties(newProps);
        PhotonNetwork.CurrentRoom.IsOpen = false;
        //맵 여러개중 하나  꺼내서 로드
        if (PhotonNetwork.CurrentRoom.CustomProperties
         .TryGetValue(CustomPropKey.MapName, out var mObj)
         && mObj is string mapName)
        {
            PhotonNetwork.LoadLevel(mapName);
        }
        else
        {
            // 없으면 기본 씬 fallback
            PhotonNetwork.LoadLevel("Stadium");
        }
        //PhotonNetwork.LoadLevel("GameScene");
    }

    private void TeamBalancing()
    {
        // 해당 메서드는 마스터 클라이언트에서만 실행됨

        // 그 방의 플레이어들의 팀 정보를 받아오기 (Start 버튼을 누른 뒤이기에 인원 변동은 없을 것으로 예상)
        // 룸 프로퍼티에서 keyPrefix로 시작하는 키들을 받아와서 리스트로
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        List<string> keys = roomProps.Keys
                            .Cast<string>()
                            .Where(k => k.StartsWith(keyPrefix))
                            .ToList();
        // 각 팀의 팀원 수 카운트
        List<string> numTeamA = new List<string>(),
                     numTeamB = new List<string>();

        for (int i = 0; i < keys.Count; i++)
        {
            string key_tmp = keys[i];
            if (roomProps.ContainsKey(key_tmp))
            {
                try
                {
                    byte team_tmp = Convert.ToByte(roomProps[key_tmp]);
                    if (team_tmp == 0)
                        numTeamA.Add(key_tmp);
                    else if (team_tmp == 1)
                        numTeamB.Add(key_tmp);
                }
                catch { }
            }
        }

        int balanceLoopCount = ((int)Mathf.Abs(numTeamA.Count - numTeamB.Count))/2;
        //Debug.Log("balanceLoopCount = " + balanceLoopCount);

        // 팀원 수가 1명 초과로 차이가 나면 아래 루프가 동작하여 팀 밸런싱
        for (int i = 0; i < balanceLoopCount; i++)
        {
            string message,
                   playerToMove = "",
                   uidToMove,
                   nickNameToMove = "";
            int tmpMoved = -1;
            // A팀이 더 많다면
            if (numTeamA.Count > numTeamB.Count)
            {
                // A팀에서 랜덤하게 1명을 빼서 B팀으로
                playerToMove = numTeamA[UnityEngine.Random.Range(0, numTeamA.Count)];
                numTeamA.Remove(playerToMove);
                numTeamB.Add(playerToMove);
                roomProps[playerToMove] = tmpMoved = 1;
            }
            // B팀이 더 많다면
            else if (numTeamA.Count < numTeamB.Count)
            {
                // B팀에서 랜덤하게 1명을 빼서 A팀으로
                playerToMove = numTeamB[UnityEngine.Random.Range(0, numTeamB.Count)];
                numTeamB.Remove(playerToMove);
                numTeamA.Add(playerToMove);
                roomProps[playerToMove] = tmpMoved = 0;
            }
            else
                continue;

            // 바꾼 룸 프로퍼티 적용
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            if (tmpMoved >= 0)
            {
                // keyPrefix를 제외하여 추출한 해당 플레이어 iD
                uidToMove = playerToMove.Replace(keyPrefix, "");
                // 플레이어ID로부터 해당 플레이어 닉네임을 가져오기
                for (int j = 0; j < PhotonNetwork.PlayerList.Length; j++)
                {
                    if (PhotonNetwork.PlayerList[j].CustomProperties.TryGetValue(CustomPropKey.PlayerId, out object id))
                    {
                        if (uidToMove == id.ToString())
                        {
                            PhotonNetwork.PlayerList[j].CustomProperties.TryGetValue(CustomPropKey.Nickname, out object nickName);
                            nickNameToMove = nickName.ToString();
                            // 변경된 팀 값을 써주기
                            PhotonNetwork.PlayerList[j].SetCustomProperties(new Hashtable
                            {
                                { CustomPropKey.TeamId, (byte)tmpMoved },
                            });
                            break;
                        }
                    }
                }
                // 메시지로 어떤 플레이어가 다른 팀으로 갔는지 밸런싱 메시지 전체에 알려주기
                string teamClass = tmpMoved == 0 ? "A" : "B";
                message = $"인원이 맞지 않아 자동으로 팀을 재배정 하였습니다.\n{nickNameToMove} 님이 {teamClass}팀으로 자동 배정되었습니다.";
                OnSendMessageEvent?.Invoke(message);
            }
        }
    }

    // 팀에 따라 색상 랜덤 설정 및 그 값을 부여
    private void TeamColorSetting()
    {
        // 빈 배열로 초기화
        TeamColorSort[] banColors = Array.Empty<TeamColorSort>();
        // 선택한 맵을 받아옴(string)
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CustomPropKey.MapName, out var mObj) && mObj is string mapName)
        {
            // 문자열 >> TeamColorSort(Enum) 변환
            if (Enum.TryParse<Map>(mapName, out Map map))
            {
               banColors = DataManager.Instance.mapPreviewsList[(int)map].banColors;
            }
        }

        // A, B팀이 어떤 색상을 들지 결정 
        int colorA = 0, colorB = 0;
        // A팀 색상으로부터 인접한(비슷한) 색상 둘
        int[] banForB = new int[2];

        // 팀 색상 팔레트 중에서 서로 다른 팀 색상을 뽑음
        int teamColorNum = DataManager.Instance.TeamColorsData.Length;
        // 마지막 색상 인덱스
        int teamColorNum_last = teamColorNum - 1;
        // 랜덤 색상이 정해졌을 때 true
        bool isRandomColorOk = false;
        while (!isRandomColorOk)
        {
            // 기본으로 루프를 탈출하게끔
            isRandomColorOk = true;
            // 팀A 색상을 랜덤하게 뽑기
            colorA = UnityEngine.Random.Range(0, teamColorNum);
            // 맵에서 사용불가인 색상이면 다시 루프
            for (int i = 0; i < banColors.Length; i++)
            {
                if(colorA == (int)banColors[i])
                {
                    isRandomColorOk = false;
                    break;
                }
            }
        }

        // A팀 색상에 따라 인접한 색상이 선택되지 않도록
        if(colorA == 0)
        {
            banForB[0] = 1;
            banForB[1] = teamColorNum_last;
        }
        if (colorA == teamColorNum_last)
        {
            banForB[0] = 0;
            banForB[1] = teamColorNum_last - 1;
        }
        else
        {
            banForB[0] = colorA + 1;
            banForB[1] = colorA - 1;
        }

        // B팀 색상 값을 A색상으로 초기화
        colorB = colorA;
        // 팀B 색상 랜덤 뽑기 시작
        isRandomColorOk = false;
        int countMax = 1000, count = 0;
        while (!isRandomColorOk)
        {
            isRandomColorOk = true;
            colorB = (byte)UnityEngine.Random.Range(0, teamColorNum);

            // 팀A 색상 or 이와 유사한 색상들일 때, 다시 루프
            if (colorB == colorA || colorB == banForB[0] || colorB == banForB[1])
            {
                isRandomColorOk = false;
                continue;
            }

            // 맵에서 사용불가인 색상이면 다시 루프
            for (int i = 0; i < banColors.Length; i++)
            {
                // 맵에서 사용 불가능한 색상들
                if (colorB == (int)banColors[i])
                {
                    isRandomColorOk = false;
                    break;
                }
            }
            // 금지 색상이 많을 경우 무한 루프.. > 빠져나갈 수 있도록 방어코드
            if (++count > countMax)
                break;
        }

        var ordered = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        int blueCount = Mathf.CeilToInt(ordered.Length / 2f);
        for (int i = 0; i < ordered.Length; i++)
        {
            byte teamId = (byte)(i < blueCount ? 0 : 1);
            //byte color = teamId == 0 ? teamColorA : teamColorB;
            ordered[i].SetCustomProperties(new Hashtable
            {
                //{ CustomPropKey.TeamId,    teamId },
                { CustomPropKey.TeamColorA, (byte)colorA },
                { CustomPropKey.TeamColorB, (byte)colorB },
            });
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        // 방을 나간 뒤 로비 씬으로 전환
        PhotonNetwork.LoadLevel("Lobby");
    }

    public void OnChangeTeamColor(byte teamColorA, byte teamColorB)
    {
        // 현재는 마스터 클라이언트만 색상 변경이 가능하게끔
        if (!PhotonNetwork.IsMasterClient) return;

        // 변경된 팀 색상을 방에 적용
        var props = new Hashtable
        {
            { CustomPropKey.TeamColorA, teamColorA },
            { CustomPropKey.TeamColorB, teamColorB }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
    
    private void OnPlayerPropertiesUpdatedEventInvoke(Player player, Hashtable changed)
    {
        AfterOnPlayerPropertiesUpdatedEvent?.Invoke(player);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        OnRoomPropertiesUpdatedEvent?.Invoke(propertiesThatChanged);
    }
    public void SetPlayerTeam(Player player, byte team)
    {
        // 1) 플레이어 속성
        player.SetCustomProperties(new Hashtable { { CustomPropKey.TeamId, team } });
        // 2) 방 속성 매핑
        string key = keyPrefix + player.UserId;
        var roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        roomProps[key] = team;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public void CreateTutorialRoom()
    {
        string roomName = $"TutorialRoom_{UnityEngine.Random.Range(1000, 9999)}";

        var roomProps = new Hashtable
    {
        { CustomPropKey.MatchTimeSeconds, 999 },          // 하드코딩 시간
        { CustomPropKey.InGameFlag, false },
        { CustomPropKey.MapName, Map.TutorialScene.ToString() },       // 튜토리얼 실제 게임 씬 이름
        { CustomPropKey.ReturnToWaitingRoomTime, 10 },
        { CustomPropKey.IsTutorial, true }                // 분기 플래그
    };

        var options = new RoomOptions
        {
            MaxPlayers = 1,
            IsOpen = false,       // 외부 유입 차단
            IsVisible = false,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new[]
            {
            CustomPropKey.MatchTimeSeconds,
            CustomPropKey.InGameFlag,
            CustomPropKey.MapName,
            CustomPropKey.IsTutorial
        }
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }
}
