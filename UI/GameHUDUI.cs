using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class GameHUDUI : MonoBehaviourPunCallbacks, IGameHUDUI
{
    //상단 양팀 플레이어 정보(닉네임, 무기변경, 생존유무)
    [Header("Player Status")]
    [SerializeField] private Transform teamAListParent;
    [SerializeField] private Transform teamBListParent;
    [SerializeField] private GameObject playerStatusPrefab;

    //킬로그 관련
    [Header("Kill Log")]
    [SerializeField] private Transform killLogParent;
    [SerializeField] private GameObject killLogPrefab;

    [Header("Result Player K/D")]
    //결과창 플레이어킬뎃관련
    public Transform playerBlueListParent;
    public Transform playerRedListParent;
    public GameObject resultEntryPrefab;
    [Header("Result Capture")]
    //결과창 구조물 개별 점유율 관련
    public Transform groupBlueListParent;
    public Transform groupRedListParent;
    public Transform groupNeutralListParent;
    public GameObject groupResultEntryPrefab;
    [Header("Result Popup")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] public GameObject resultPopup;
    [SerializeField] private TMP_Text winnerText, teamResultTextA, teamResultTextB;
    //[SerializeField] private Button returnButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private TMP_Text returnCountdownText;

    [HideInInspector] public bool isPopupActive = false;
    //10초뒤 자동이동 이벤트로 넘겨줌.
    [HideInInspector] public event Action OnResultPopupShown;

    [Header("Capture Score UI")]
    [SerializeField] private TMP_Text bluePointsText;
    [SerializeField] private TMP_Text neutralPointsText;
    [SerializeField] private TMP_Text redPointsText;

    // 이하 팀 색상에 따른 색상 변경용으로 추가 (7/22 유빈)
    [Header("Info Texts")]
    [SerializeField] private TMP_Text blueInfoText;
    [SerializeField] private TMP_Text redInfoText;

    [Header("Result Images")]
    [SerializeField] private Image blueUserInfo;
    [SerializeField] private Image redUserInfo;
    [SerializeField] private Image blueStructureInfo;
    [SerializeField] private Image redStructureInfo;
    
    //시간에 따른 텍스트 넣어줄곳
    [Header("AnnouncementText")]
    [SerializeField]
    public TextMeshProUGUI AnnouncementText;

    public GameObject ResultPopup { get { return resultPopup; } }

    // 닉네임 → 표시된 UI 엔트리
    private Dictionary<int, PlayerStatusUI> _playerEntries = new Dictionary<int, PlayerStatusUI>();

    // 해당 맵의 색칠 가능한 오브젝트 그룹들
    StructureGroup[] structureGroups;
    // 현재 플레이어 표시 정보를 담을 리스트
    List<ResultInfo> tmpInfos = new List<ResultInfo>();
    // 각 플레이어 정보 표시를 위한 UI들
    ResultEntryUI[] resultEntryUIs;
    // 각 팀 점유율을 보여주기 위한 UI들
    GroupResultEntryUI[] groupResultEntryUI;
    // 게임 시작 시 플레이어 수
    int numInitPlayer;
    // Tab 키를 눌렀을 때 전투 현황을 보여줄 수 있는 단축키
    InputAction tabAction;

    //결과,상황창 팝업이 코루틴 주기 갱신하기위한 변수들
    [Header("Refresh")]
    [Tooltip("스코어보드 갱신 주기(초)")]
    [SerializeField] private float scoreboardRefreshInterval = 0.33f;
    private Coroutine scoreboardCoroutine;
    private bool scoreboardDirty;


    void Awake()
    {
        if (resultPopup != null)
            resultPopup.SetActive(false);
        //else
            //Debug.LogError("NULL: PlayerHUDUI 프리팹에 resultPopup 할당 확인하기");

        tabAction = new InputAction(binding: "<Keyboard>/tab");

        //방장이 모두 방으로 이동시키던 구버전 테스트용

        //if (returnButton != null)
        //{
        //    if (PhotonNetwork.IsMasterClient)
        //    {
        //        returnButton.onClick.AddListener(ToWaitingRoom);
        //    }
        //    else
        //    {
        //        // 숨기거나, interactable만 꺼도 됩니다.
        //        returnButton.gameObject.SetActive(false);
        //    }
        //}
    }
    public override void OnEnable()
    {
        base.OnEnable();
        tabAction.Enable();
        tabAction.performed += _ => ShowCaptureScoreBoard();
        tabAction.canceled += _ => CloseCaptureScoreBoard();
    }

    public override void OnDisable()
    {
        tabAction.performed -= _ => ShowCaptureScoreBoard();
        tabAction.canceled -= _ => CloseCaptureScoreBoard();
        tabAction.Disable();
        base.OnDisable();
    }

    void Start()
    {
        InitializePlayerList();
        InitColors();
        InitCaptureScoreBoard();
    }


    // 룸에 들어있는 모든 플레이어를 순회하면서
    // 좌/우 부모 컨테이너에 PlayerStatusPrefab을 띄우는 함수에요.
    private void InitializePlayerList()
    {
        // 기존에 띄운 엔트리 지우기
        foreach (Transform child in teamAListParent) Destroy(child.gameObject);
        foreach (Transform child in teamBListParent) Destroy(child.gameObject);
        _playerEntries.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int actorNum = p.ActorNumber;
            string nick = p.NickName;

            // CustomProperties에서 팀 아이디(byte) 꺼내기
            byte team = 0;
            if (p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out var t))
                team = (byte)t;

            // CustomProperties에서 무기 인덱스(int) 꺼내서 이름으로 변환
            int weaponIndex = 0;
            if (p.CustomProperties.TryGetValue(CustomPropKey.WeaponSelection, out var w))
                weaponIndex = (int)w;

            // 어느 부모 아래에 둘지 결정
            var parent = (team == 0) ? teamAListParent : teamBListParent;

            // 인스턴스화 & 초기화
            var go = Instantiate(playerStatusPrefab, parent);
            var entryUI = go.GetComponent<PlayerStatusUI>();
            // 닉네임, 무기, 팀 표시
            entryUI.SetData(nick, weaponIndex, team);
            _playerEntries[actorNum] = entryUI;
        }
    }

    // 팀 색상에 맞게 바뀌어야 할 UI 색상 변경
    void InitColors()
    {
        // HUD UI의 스코어 보드, 결과창의 표시 색상
        bluePointsText.color = blueInfoText.color = blueUserInfo.color = blueStructureInfo.color = DataManager.Instance.colorTeamA;
        redPointsText.color = redInfoText.color = redUserInfo.color = redStructureInfo.color = DataManager.Instance.colorTeamB;
    }

    // 리스폰이나 무기 변경 시, 해당 플레이어의 무기 텍스트를 갱신할 때 호출
    public void UpdatePlayerWeapon(int playerID, int weaponIndex)
    {
        if (_playerEntries.TryGetValue(playerID, out var ui))
            ui.UpdateWeapon(weaponIndex);
    }
    // 킬이 등록될 때마다 호출되어 킬 로그를 띄워 주기
    public void AddKillFeedEntry(int killerID, int victimID)
    {
        // 찾은 정보의 수
        int numSerched = 0;
        string killerNick = "", victimNick = "";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int actorNum = p.ActorNumber;
            if(actorNum == killerID)
            {
                killerNick = p.NickName;
                numSerched++;
            }
            else if(actorNum == victimID)
            {
                victimNick = p.NickName;
                numSerched++;
            }

            // 죽인 플레이어, 죽은 플레이어 둘의 닉네임을 찾았다면 탐색을 멈추게끔
            if (numSerched > 1)
                break;
        }

        var go = Instantiate(killLogPrefab, killLogParent);
        go.GetComponent<KillLogUI>().SetData(killerNick, victimNick);
    }

    // 죽고/살아나는 순간 그 플레이어 UI를 회색/원래대로 복구
    public void SetPlayerAlive(int playerID, bool alive)
    {
        if (_playerEntries.TryGetValue(playerID, out var ui))
            ui.SetAlive(alive);
    }

    public void UpdateTimeDisplay(int remainingSeconds)
    {
        if (timeText != null)
            timeText.text = Mathf.Max(remainingSeconds, 0).ToString();
    }

    public void UpdateCaptureUI(int blue, int neutral, int red)
    {
        if (bluePointsText != null) bluePointsText.text = blue.ToString();
        if (neutralPointsText != null) neutralPointsText.text = neutral.ToString();
        if (redPointsText != null) redPointsText.text = red.ToString();
    }

    // 처음 시작할 때 스코어를 표시할 플레이어 리스트 항목 생성
    public void InitCaptureScoreBoard()
    {
        // 플레이어 정보 표시 관련
        // 게임 시작 때의 플레이어 수
        numInitPlayer = PhotonNetwork.PlayerList.Length;
        // 플레이어의 정보를 표시할 UI들
        resultEntryUIs = new ResultEntryUI[numInitPlayer];
        tmpInfos.Clear();

        for (int i = 0; i < numInitPlayer; i++)
        {
            int actornum = PhotonNetwork.PlayerList[i].ActorNumber;
            tmpInfos.Add(StatsManager.Instance.GetOrCreate(actornum));

            // 해당 팀에 항목 추가
            Transform parent = tmpInfos[i].Team == 0
                                ? playerBlueListParent
                                : playerRedListParent;

            GameObject go = Instantiate(resultEntryPrefab, parent);
            resultEntryUIs[i] = go.GetComponent<ResultEntryUI>();
        }

        // 색칠 가능한 구조물 정보 표시 관련
        // 해당 씬에 있는 StructureGroup들을 받아오기
        structureGroups = FindObjectsOfType<StructureGroup>();
        // 해당 씬 내 색칠할 수 있는 구조물 그룹 수만큼, A,B,중립 각각 점유율 패널 생성
        int numStructureGroups = structureGroups.Length;
        // 각 팀별(중립 포함 = 2) 구조물 점유율을 표시할 UI
        groupResultEntryUI = new GroupResultEntryUI[numStructureGroups];
        for (int i = 0; i < numStructureGroups; i++)
        {
            // 매번 갯수 다르게 생성/파괴하기보다 해당 맵의 구조물 그룹 수만큼 미리 생성해두고 제어
            // 우선 중립을 부모로 생성(Vertical Layout Group 컴포넌트로 자동 정렬)
            groupResultEntryUI[i] = Instantiate(groupResultEntryPrefab, groupNeutralListParent).GetComponent<GroupResultEntryUI>();
        }

        // 로비로 나가기 버튼, 자동 방으로 나가기 카운트 다운 비활성화
        leaveLobbyButton.gameObject.SetActive(false);
        returnCountdownText.transform.parent.gameObject.SetActive(false);
    }

    void ShowCaptureScoreBoard()
    {
        // 정보 최신화
        UpdateCaptureScoreBoard();

        //모든 정보들 다 받아오면, 현황판 팝업 보이도록
        resultPopup.SetActive(true);
        StartScoreboardLoop();
    }

    void CloseCaptureScoreBoard()
    {
        StopScoreboardLoop();
        resultPopup.SetActive(false);
    }
    private void StartScoreboardLoop()
    {
        if (scoreboardCoroutine != null) return;
        scoreboardCoroutine = StartCoroutine(ScoreboardAutoRefreshLoop());
    }

    private void StopScoreboardLoop()
    {
        if (scoreboardCoroutine != null)
        {
            StopCoroutine(scoreboardCoroutine);
            scoreboardCoroutine = null;
        }
    }

    private IEnumerator ScoreboardAutoRefreshLoop()
    {
        // 팝업이 열려있는 동안만 주기 갱신
        while (resultPopup != null && resultPopup.activeSelf)
        {
            // 더티 플래그가 있으면 즉시 반영해도 됨(없으면 그냥 주기 갱신)
            UpdateCaptureScoreBoard();
            yield return new WaitForSeconds(scoreboardRefreshInterval);
        }
        scoreboardCoroutine = null;
    }

    // 외부에서 “변경됨”을 알릴 수 있도록(선택 사용)
    public void MarkScoreboardDirty() => scoreboardDirty = true;

    void UpdateCaptureScoreBoard(List<ResultInfo> resultInfos = null)
    {
        // 기존에 담긴 정보를 초기화 
        tmpInfos.Clear();
        // 플레이어 정보 가져오기
        for (int i = 0; i < numInitPlayer; i++)
        {
            // 중간에 나간 플레이어라면, 해당 항목을 표시하지 않도록
            if (i >= PhotonNetwork.PlayerList.Length)
            {
                resultEntryUIs[i].gameObject.SetActive(false);
            }
            // 방에 있는 플레이어라면,
            else if(resultInfos == null)
            {
                int actornum = PhotonNetwork.PlayerList[i].ActorNumber;
                tmpInfos.Add(StatsManager.Instance.GetOrCreate(actornum));
                Transform parent = tmpInfos[i].Team == 0
                                    ? playerBlueListParent
                                    : playerRedListParent;
                // 패널에 플레이어 정보 써주기
                if (resultEntryUIs[i].transform.parent != parent)
                    resultEntryUIs[i].transform.SetParent(parent, false);
                resultEntryUIs[i].SetData(tmpInfos[i]);
            }
            else
            {
                Transform parent = resultInfos[i].Team == 0
                                    ? playerBlueListParent
                                    : playerRedListParent;
                // 패널에 플레이어 정보 써주기
                resultEntryUIs[i].SetData(resultInfos[i]);
            }
        }

        // 구조물 정보를 기입
        for (int i = 0; i < structureGroups.Length; i++)
        {
            StructureGroup g = structureGroups[i];
            byte winner = g.PercentA > g.PercentB ? (byte)0
              : g.PercentB > g.PercentA ? (byte)1
              : (byte)255; // 무승부

            Transform parent;
            if (winner == 0)
                parent = groupBlueListParent;
            else if (winner == 1)
                parent = groupRedListParent;
            else
                parent = groupNeutralListParent;

            // 어느 팀이 점유중인가에 따라 패널위치 이동
            if (groupResultEntryUI[i].transform.parent != parent)
                groupResultEntryUI[i].transform.SetParent(parent, false);
            // 점유율 표시
            groupResultEntryUI[i].SetData(g.name, g.PercentA, g.PercentB, winner);
        }

        scoreboardDirty = false; // 주기 갱신과 겸용 시 플래그 리셋
    }


    public void ShowEndPopup()
    {
        isPopupActive = true;
        tabAction.performed -= _ => ShowCaptureScoreBoard();
        tabAction.canceled -= _ => CloseCaptureScoreBoard();
        tabAction.Disable();

        // 종료 시점에서 더 경신되지 않은 데이터
        StatsManager.Instance.FreezeStats();

        // 경기 종료 사운드
        SoundManager.Instance.ChangeBgm(SoundManager.Instance.EndingBgmClip);

        // 점유율, 플레이어 정보 표시
        List<ResultInfo> frozen = StatsManager.Instance.GetFrozenResults();
        UpdateCaptureScoreBoard(frozen);

        // 인스펙터에 할당하면서 해당 찾는 구문이 필요 없어짐
        //var winnerText = resultPopup.transform.Find("WinnerText")?.GetComponent<TMP_Text>();
        int b = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("BluePoints", out var vb) ? (int)vb : 0;
        int r = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RedPoints", out var vr) ? (int)vr : 0;
        if (winnerText != null && teamResultTextA != null && teamResultTextB != null)
        {
            if (b > r)
            {
                winnerText.text = "팀 A 승리!";
                teamResultTextA.text = "승리!";
                teamResultTextB.text = "패배..";
            }
            else if (r > b)
            {
                winnerText.text = "팀 B 승리!";
                teamResultTextA.text = "패배..";
                teamResultTextB.text = "승리!";

            }
            else
            {
                winnerText.text = "무승부!";
                teamResultTextA.text = teamResultTextB.text = "";
            }
        }


        //모든 정보들 다 받아오면, 결과창 팝업 보이도록
        resultPopup.SetActive(true);
        //로비로 탈주버튼
        leaveLobbyButton.gameObject.SetActive(true);
        leaveLobbyButton.onClick.RemoveAllListeners();
        leaveLobbyButton.onClick.AddListener(() =>
        {
            if(PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        });

        returnCountdownText.transform.parent.gameObject.SetActive(true);

        // 마우스 출현 및 버튼 조작이 가능하게끔 락 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //게임 끝나고 플레이어 조작 금지
        foreach (var VARIABLE in GameManager.Instance.players)
        {
            VARIABLE.Value.playerAction.Disable();
        }

        OnResultPopupShown?.Invoke();

        StartScoreboardLoop();
    }

    public void SetReturnCountdown(int secondsLeft)
    {
        returnCountdownText.text = $"방으로 이동까지 {secondsLeft}초";
    }

    //방장이 모두 방으로 이동시키던 구버전 테스트용

    //public void ToWaitingRoom()
    //{
    //    PhotonNetwork.CurrentRoom.SetCustomProperties(
    //     new Hashtable { { CustomPropKey.InGameFlag, false } }
    // );
    //    if (resultPopup != null)
    //        resultPopup.SetActive(false);
    //    isPopupActive = false;
    //    // PhotonNetwork.LoadLevel 로 전환하여 모든 클라이언트 동기화
    //    PhotonNetwork.LoadLevel("WaitingRoom");
    //}


    // 룸에 새 플레이어가 들어오거나 나갈 때 목록을 갱신해줘야함.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        InitializePlayerList();
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        InitializePlayerList();
    }
}
