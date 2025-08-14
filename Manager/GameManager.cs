using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SpawnPointHandler spawnPointHandler;
    [SerializeField] public PlayerRespawnManager playerRespawnManager;

    [field:SerializeField, Header("HUD & Timer")] public GameHUDUI hudUI {  get; private set; }
    [SerializeField] private GameTimer timerPrefab;

    private GameTimer timerInstance;
    // GameStartManager에서 RPC로 타이머를 시작하기 위해
    public GameTimer TimerInstance => timerInstance;

    //아직 로컬으로만 저장되어있고 Info같은거 최신화 안 되어 있음!
    public Dictionary<int,PlayerHandler> players = new Dictionary<int, PlayerHandler>();

    public bool isGamePlaying = false;

    // 중복으로 접근하지 못하게 락을 거는 용도
    
    
    
    static readonly object _lock = new object();
    void Awake()
    {
        if (Instance == null)
        {
            lock (_lock)
            {
                Instance = this;
            }
        }
        else
            Destroy(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Enum.TryParse<Map>(scene.name, out _)) // 원본 : scene.name == "GameScene" , 이제 enum 활용
        {
            // 1) 타이머 인스턴스 생성 & UI 구독
            timerInstance = Instantiate(timerPrefab);
            UIManager.Instance.RegisterGameHUDUI(hudUI, timerInstance);

            // 2) 플레이어 스폰
            StartCoroutine(SpawnLocalPlayerWhenReady());
            
            //// 3) CP가 완전히 반영될 때까지 대기 후 타이머 시작
            //StartCoroutine(WaitForMatchStartAndBeginTimer());
        }
        else
        {
            // 게임 씬이 아니면(예: WaitingRoom) 이 매니저를 파괴합니다.
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
        }
    }

    //private IEnumerator WaitForMatchStartAndBeginTimer()
    //{
    //    // 한 프레임 대기(씬 로드, CP 반영 타이밍 보완용)
    //    yield return null;

    //    // 추가로 필요하다면 최대 N초까지 기다리도록 할 수 있습니다:
    //    float timeout = Time.realtimeSinceStartup + 3f;
    //    while (Time.realtimeSinceStartup < timeout)
    //    {
    //        var props = PhotonNetwork.CurrentRoom.CustomProperties;
    //        if (props.TryGetValue(CustomPropKey.InGameFlag, out var ig) && (bool)ig
    //            && props.TryGetValue(CustomPropKey.MatchTimeSeconds, out var ms))
    //        {
    //            int duration = (int)ms;
    //            //Debug.Log($"[GameManager] Starting local timer: {duration} seconds");
    //            timerInstance.StartLocalTimer(duration);
    //            yield break;
    //        }
    //        yield return null;
    //    }
    //    //Debug.LogError("[GameManager] 타이머 시작 실패: InGameFlag 또는 MatchTimeSeconds 누락");
    //}


    //// ② 룸 커스텀프로퍼티가 바뀔 때 호출
    //public override void OnRoomPropertiesUpdate(Hashtable propsChanged)
    //{
    //    // 매치 시작 플래그 && 시간 설정값이 있으면
    //    if (propsChanged.ContainsKey(CustomPropKey.InGameFlag)
    //        && (bool)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.InGameFlag])
    //    {
    //        int duration = (int)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.MatchTimeSeconds];
    //        timerInstance?.StartLocalTimer(duration);
    //    }
    //}

    // 준비되면 플레이어 하나 생성(여기서 초기화!)
    private IEnumerator SpawnLocalPlayerWhenReady()
    {
        yield return null;
        var localPlayer = PhotonNetwork.LocalPlayer;
        PlayerInfo playerInfo;
        // 팀 배정 및 CustomProperties 등록
        if (localPlayer.CustomProperties.Count == 0 || !localPlayer.CustomProperties.ContainsKey(CustomPropKey.TeamId))
        {
            playerInfo = new PlayerInfo();
            playerInfo.Nickname = localPlayer.NickName;
            playerInfo.TeamId = (byte)Random.Range(0, 2); // 랜덤 팀 배정
            // 팀 정보 네트워크에 공유
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerInfo.ToHashtable());
        }
        else
        {
            playerInfo = PlayerInfo.FromHashtable(localPlayer.CustomProperties);
        }
        //같은팀내에서 actorNumber 순으로 정렬
        var teamPlayers = PhotonNetwork.PlayerList
          .Where(p => p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out var t) &&
                      (byte)t == playerInfo.TeamId)
          .OrderBy(p => p.ActorNumber)
          .ToList();
        int indexInTeam = teamPlayers.FindIndex(p => p == localPlayer);

        // 스폰 위치와 회전
        var spawnPosition = spawnPointHandler.GetSpawnPoint(playerInfo.TeamId, indexInTeam);
        var spawnRotation = spawnPointHandler.GetSpawnRotation(playerInfo.TeamId, indexInTeam);
        string playerPrefabPath = "Player/PlayerPrefab";
        var playerObject = PhotonNetwork.Instantiate(playerPrefabPath, spawnPosition, spawnRotation);
        var playerHandler = playerObject.GetComponent<PlayerHandler>();
        players.Add(playerHandler.Info.SequenceNumber, playerHandler);
        var initialStatus = new PlayerStatus(100f, 100f, 100f, 100f, 30f, 50f, 30f, 100f);
        playerHandler.InitializeStatus(initialStatus, playerInfo);
        playerRespawnManager.SetOnDeathEvent(playerHandler);
        playerRespawnManager.spawnPointHandler = spawnPointHandler;
        // 무기 장착
        PhotonView playerPhotonView = playerObject.GetComponent<PhotonView>();
        if (playerPhotonView.IsMine)
        {
            playerPhotonView.RPC(
                nameof(playerHandler.EquipWeapon),
                RpcTarget.AllViaServer,
                playerInfo.WeaponSelection
            );
        }
        // InGameStartReady 등록 (게임 준비 완료)
        Hashtable props = new Hashtable
        {
            { CustomPropKey.InGameStartReady, true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}