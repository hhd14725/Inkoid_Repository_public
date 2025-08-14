using ExitGames.Client.Photon;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntry : MonoBehaviourPunCallbacks
{
    [SerializeField] private Image teamColorImage;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("무기 선택 표시")]
    //[SerializeField] private TextMeshProUGUI playerCurrentWeaponText;
    [SerializeField]
    private Image playerCurrentWeaponImage;

    [Header("준비/호스트 아이콘")] [SerializeField]
    private GameObject readyIconObject;

    [SerializeField] private GameObject hostIconObject;

    //강퇴 버튼
    [Header("강퇴 버튼")] [SerializeField] private Button kickButton;

    //팀 교체 버튼
    [Header("팀 교체 버튼")] [SerializeField] private Button changeTeamButton;

    //팀 보여주기
    [Header("현재 팀 text")] [SerializeField] private TextMeshProUGUI currentTeamText;

    public event Action<Player> OnKickClicked;
    //public event Action unsubscribe;

    private WaitingRoomChangeWeaponUI _waitingRoomChangeWeaponUI;

    public Player _player { get; private set; }

    // 방에 입장한 플레이어들의 수를 룸 프로퍼티에 등록해줄 키
    const string key_numPlayersIn = "countPlayersIn";

    private bool _teamChangeLocked = false;


    //private void Awake()
    //{
    //    // 처음 한번만 체크하면 되는 것으로 변경
    //    if (playerNameText == null) //Debug.LogError("PlayerEntry: playerNameText 미할당.", this);
    //    if (teamColorImage == null) //Debug.LogError("PlayerEntry: teamColorImage 미할당", this);
    //    if (readyIconObject == null) //Debug.LogError("PlayerEntry: readyIconObject 미할당", this);
    //    if (kickButton == null) //Debug.LogError("PlayerEntry: kickButton 미할당", this);
    //}
    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        NetworkManager.Instance.AfterOnPlayerPropertiesUpdatedEvent -= PlayerEntryChangeWeaponText;
    }

    // 대기방의 변화가 있을 때 호출
    public void Setup(Player player, WaitingRoomChangeWeaponUI waitingRoomChangeWeaponUI)
    {
        _waitingRoomChangeWeaponUI = waitingRoomChangeWeaponUI;
        _player = player;
        UpdateTeamVisuals();
        playerNameText.text = player.NickName;

        //if (_player.IsLocal)
        //{
        //    changeTeamButton.gameObject.SetActive(true);
        //    // 리스너 중복 등록을 방지하기 위해 항상 제거 후 추가합니다.                                              
        //    changeTeamButton.onClick.RemoveAllListeners();
        //    changeTeamButton.onClick.AddListener(ChangeTeam);
        //}
        //else
        //{
        //    changeTeamButton.gameObject.SetActive(false);
        //}
        changeTeamButton.onClick.RemoveAllListeners();
        if (_player.IsLocal)
        {
            changeTeamButton.gameObject.SetActive(true);
            changeTeamButton.onClick.AddListener(ChangeTeam);
        }
        else
        {
            changeTeamButton.gameObject.SetActive(false);
        }
        ApplyTeamChangeInteractable();
        // // 대기방에서의 팀 색상 표시
        // // 대기방에서도 팀을 변경할 수 있게 해주면 즉시 반영이 됨
        // Color c = Color.blue;
        // if (player.CustomProperties.TryGetValue(CustomPropKey.TeamId, out object co))
        // {
        //     Team team = (Team)((byte)co);
        //     switch (team)
        //     {
        //         case Team.TeamA:
        //             c = DataManager.Instance.colorTeamA;
        //             break;
        //         case Team.TeamB:
        //             c = DataManager.Instance.colorTeamB;
        //             break;
        //         default:
        //             break;
        //     }
        // }
        // teamColorImage.color = c;

        // 마스터 클라이언트라면 이를 기억 > 마스터 클라이언트에서는 자동으로 전부 true로 넣어버림 > wtf > 그럼 OnJoinedRoom에서 한번만 주도록 하면?

        bool isHost;
        // 레디 동기화
        if (player.CustomProperties.TryGetValue(CustomPropKey.IsHost, out isHost))
        {
            if (isHost)
            {
                hostIconObject.SetActive(isHost);
                readyIconObject.SetActive(false);
            }
            else
            {
                hostIconObject.SetActive(false);
                bool r = player.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var val) && val is bool b && b;
                readyIconObject.SetActive(r);
            }
        }

        transform.SetParent(transform.parent, false);

        NetworkManager.Instance.AfterOnPlayerPropertiesUpdatedEvent += PlayerEntryChangeWeaponText;
        PlayerEntryChangeWeaponText(player);

        bool canKick = PhotonNetwork.IsMasterClient && player != PhotonNetwork.LocalPlayer;
        kickButton.gameObject.SetActive(canKick);
        kickButton.interactable = canKick;
        kickButton.onClick.RemoveAllListeners();
        if (canKick)
            kickButton.onClick.AddListener(() => OnKickClicked?.Invoke(_player));
    }

    public void PlayerEntryChangeWeaponText(Player player)
    {
        if (_player.ActorNumber == player.ActorNumber)
        {
            int index = 0;

            if (player.CustomProperties.TryGetValue(CustomPropKey.WeaponSelection, out object val))
            {
                index = (int)val;
            }

            // 텍스트로 선택 무기 이름 표시 변경
            //playerCurrentWeaponText.text = DataManager.Instance.WeaponPrefabs[index].name;
            // 선택한 무기 이미지도 함께 표시하도록 추가 > 투머치인 것 같아서 무기 아이콘만 표시하도록
            if (playerCurrentWeaponImage && DataManager.Instance)
                playerCurrentWeaponImage.sprite = DataManager.Instance.WeaponImages[index];
        }
    }

    private void ChangeTeam()
    {
        // 1. 현재 팀 정보 가져오기 (없을 경우 TeamA를 기본값으로)
        Team currentTeam = Team.TeamA;
        if (_player.CustomProperties.TryGetValue(CustomPropKey.TeamId, out object teamObj))
        {
            currentTeam = (Team)(byte)teamObj;
        }

        // 2. 팀 전환 (A팀 -> B팀, B팀 -> A팀)
        Team newTeam = (currentTeam == Team.TeamA) ? Team.TeamB : Team.TeamA;

        // 3. 변경할 프로퍼티를 Hashtable에 담기
        Hashtable props = new Hashtable { { CustomPropKey.TeamId, (byte)newTeam } };

        // 4. SetCustomProperties를 호출하여 모든 클라이언트에 변경 사항 전송
        _player.SetCustomProperties(props);

        // 5. UI에 반영(아 생각해보니 나만 바꾸면 끝이 아니네 다 뿌려줘야해)
        //currentTeamText.text = currentTeam.ToString();
        NetworkManager.Instance.SetPlayerTeam(_player, (byte)newTeam);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (this == null) return;
        if (targetPlayer != _player) return;

        // 팀 정보가 변경되었을 때만 팀 UI를 업데이트합니다.                                                    
        if (changedProps.ContainsKey(CustomPropKey.TeamId))
        {
            UpdateTeamVisuals();
        }

        if (changedProps.ContainsKey(CustomPropKey.IsReady))
        {
            // 레디 바뀌면 상태 재계산
            ApplyTeamChangeInteractable();
            //// 로컬 플레이어일 경우, 준비 상태에 따라 팀 변경 버튼의 활성화 여부를 결정합니다.
            //if (_player.IsLocal)
            //{
            //    UpdateChangeTeamButtonState();
            //}

            // 다른 플레이어에게도 준비 상태 아이콘이 표시되어야 하므로, 아이콘 업데이트 로직도 추가합니다.
            bool isHost = _player.CustomProperties.TryGetValue(CustomPropKey.IsHost, out var hostVal) &&
                          hostVal is bool h && h;
            if (!isHost)
            {
                bool isReady = _player.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var readyVal) &&
                               readyVal is bool r && r;
                readyIconObject.SetActive(isReady);
            }
        }

        if (changedProps.ContainsKey(CustomPropKey.IsHost))
        {
            // 호스트 여부 바뀌어도 적용
            ApplyTeamChangeInteractable();
            //if (_player.IsLocal)
            //{
            //    changeTeamButton.interactable = true;
            //}
        }
    }

    // 팀 관련 UI를 업데이트하는 로직을 함수로 분리합니다.
    private void UpdateTeamVisuals()
    {
        if (_player == null) return;

        Team team = Team.TeamA; // 기본값
        if (_player.CustomProperties.TryGetValue(CustomPropKey.TeamId, out object co))
        {
            team = (Team)(byte)co;
        }

        currentTeamText.text = team.ToString();

        // Color c = Color.gray;
        // switch (team)
        // {
        //     case Team.TeamA: c = DataManager.Instance.colorTeamA; break;
        //     case Team.TeamB: c = DataManager.Instance.colorTeamB; break;
        // }
        // teamColorImage.color = c;
    }

    private void UpdateChangeTeamButtonState()
    {
        //if (changeTeamButton == null)
        //    return;

        //if (_player != null && _player.IsLocal)
        //{
        //    bool isReady = _player.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var val) && val is bool b &&
        //                   b;
        //    changeTeamButton.interactable = !isReady;
        //}
        ApplyTeamChangeInteractable();
    }

    //호스트가 시작버튼 눌렀을때 킥버튼 막기.
    public void DisableKick()
    {
        if (kickButton != null)
        {
            kickButton.interactable = false;
        }
    }

    public void SetTeamChangeLocked(bool locked)
    {
        _teamChangeLocked = locked;
        ApplyTeamChangeInteractable();
    }

    private void ApplyTeamChangeInteractable()
    {
        if (changeTeamButton == null || _player == null) return;

        // 로컬플레이어만 팀 변경 버튼 노출
        changeTeamButton.gameObject.SetActive(_player.IsLocal);

        // 레디 여부
        bool isReady = _player.CustomProperties.TryGetValue(CustomPropKey.IsReady, out var v)
                       && v is bool b && b;

        // 잠금중이면 무조건 false, 아니면 (로컬 & not ready)
        changeTeamButton.interactable = (_player.IsLocal && !isReady) || !_teamChangeLocked;
    }

}