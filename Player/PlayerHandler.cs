using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerHandler : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    // 나중에 런칭을 목적으로 둔다면 private set 프로퍼티로 바꾸던지 할 필요가 있음. 최소한의 보안
    public PlayerInfo Info;
    public PlayerStatus Status { get; private set; }

    public PlayerCameraController playerCameraController;

    //public Renderer characterRenderer;
    public SkinnedMeshRenderer characterRenderer;
    [SerializeField] private string characterRendererText = "Body";
    readonly string LayerName_TeamA = "TeamA", LayerName_TeamB = "TeamB";

    public PlayerAction playerAction;
    public bool isDash = false;
    public float dashInkCost = 10f;
    public bool isAttack = false;
    public bool isTryGrapple = false;
    public bool isDamaged = true;

    //피격 애니메이션 전용
    public event Action<float> OnDamageTaken;   //  피격 애니메이션 전용 추가함
    // 데미지 받을때 상호작용용 콜백
    public Action OnDamaged;

    //아이템관련 이벤트
    //public event Action<float> OnHpUIChanged;   //  HP UI 전용 추가함
    public event Action<float> OnInkChanged;
    public event Action<float> OnHealthChanged;
    public Action<float> OnBulletSizeChanged;    // 총알 크기 변경
    public Action<float> OnBrushSizeChanged;     // 브러시(물감) 크기 변경
    public event Action<PlayerHandler> OnDeath;
    public event Action OnBeforeDeath;

    public PhotonView playerPhotonView;

    public Transform WeaponTransform;

    public Weapon_Base weapon;

    [SerializeField] private GameObject deathEffectPrefab;

    //public HealthBar healthBar; // 추가함


    //  injectInk FillAmount 제어용
    private Renderer _injectorRenderer;
    private int _fillAmountPropID;
    private int _teamColorPropID;
    private Color _teamColor;
    private MaterialPropertyBlock _mpb;

    private PlayerInkHandler _inkHandler;

    private int Barriers = 1 << (int)EnumLayer.LayerType.BarrierTeamA
                           | 1 << (int)EnumLayer.LayerType.BarrierTeamB;
    
    //대쉬 최대 속도 제어용(두트윈 써보기)
    private float originMaxSpeed;
    private float boostMaxSpeed = 60f;
    private float raiseDuration = 0f;  // 올라가는 데 걸리는 시간
    private float holdDuration = 0.5f;     // 최고점에서 유지하는 시간
    private float lowerDuration = 0.5f;    // 다시 내려오는 데 걸리는 시간
    
    // 현재 실행 중인 시퀀스를 저장하기 위한 변수
    private Sequence mySequence;

    private PlayerEffectHandler _effectHandler;
    private void Awake()
    {
        playerAction = new PlayerAction();
        playerCameraController = GetComponent<PlayerCameraController>();
        //characterRenderer = transform.Find(characterRendererText).GetComponent<Renderer>();
        if (TryGetComponent(out playerPhotonView))
        {
            if (photonView.IsMine)
            {
                RPCManager.Instance.SetPlayerPhotonView(playerPhotonView);
            }
        }
        else
        {
            playerPhotonView = GetComponent<PhotonView>();
        }
        if (characterRenderer == null)
            characterRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // injectorInk 렌더러 찾기
        var inj = transform.Find("injector/injectorInk");
        if (inj != null)
            _injectorRenderer = inj.GetComponent<Renderer>();

        // 셰이더 프로퍼티 ID 캐싱
        _fillAmountPropID = Shader.PropertyToID("_FillAmount");
        _teamColorPropID = Shader.PropertyToID("_TeamColor");

        // MPB 초기화
        _mpb = new MaterialPropertyBlock();

        // 잉크 핸들러 캐싱
        _inkHandler = GetComponent<PlayerInkHandler>();
        _effectHandler = GetComponent<PlayerEffectHandler>();
    }

    private void Start()
    {
        if (!photonView.IsMine) return;
        PlayerColorSync();
        // if (healthBar != null)
        // {
        //     healthBar.Init(this.transform); // 반드시 Init() 호출해야 IsMine 판단 실행됨
        //
        //     if (!photonView.IsMine)
        //     {
        //         healthBar.SetHealth(Status.Health, Status.MaximumHealth);
        //     }
        // }
        UpdateInkFill(_inkHandler.Ink);

        originMaxSpeed = Status.MaximumMoveSpeed;
    }

    private void OnEnable()
    {
        _inkHandler.OnInkChanged += UpdateInkFill;

        if (!photonView.IsMine) return;
        //playerAction.Enable();

        playerAction.PlayerActionMap.Dash.started += Dash;
        playerAction.PlayerActionMap.Attack.started += AttackStart;
        playerAction.PlayerActionMap.Attack.canceled += AttackCancel;
        playerAction.PlayerActionMap.Grapple.started += GrappleStart;
        playerAction.PlayerActionMap.Grapple.canceled += GrappleCancel;
#if UNITY_EDITOR
        playerAction.PlayerActionMap.TestKill.started += TestKill;
        playerAction.PlayerActionMap.FullInk.started += FullInk;
#endif

        //다시 켜질때 무조건 리스폰 장소에서 켜지니까 데미지 않받게
        isDamaged = false;
        //Debug.Log("데미지 첨에 금지");
        //Debug.Log("OnEnable");
    }

    private void OnDisable()
    {
        _inkHandler.OnInkChanged -= UpdateInkFill;
        if (!photonView.IsMine) return;
        playerAction.Disable();

        playerAction.PlayerActionMap.Dash.started -= Dash;
        playerAction.PlayerActionMap.Attack.started -= AttackStart;
        playerAction.PlayerActionMap.Attack.canceled -= AttackCancel;
        playerAction.PlayerActionMap.Grapple.started -= GrappleStart;
        playerAction.PlayerActionMap.Grapple.canceled -= GrappleCancel;
#if UNITY_EDITOR
        playerAction.PlayerActionMap.TestKill.started -= TestKill;
        playerAction.PlayerActionMap.FullInk.started -= FullInk;
#endif
    }

    //갈고리 잉크 소모량 표기
    private void UpdateInkFill(float currentInk)
    {
        if (_injectorRenderer == null) return;
        float ratio = (_inkHandler.maximumInk > 0f)
            ? Mathf.Clamp01(currentInk / _inkHandler.maximumInk)
            : 0f;

        // 실제 잉크 UI와 동기화하기위해 스케일링했습니다. 갈고리 잉크통은 3d 육면체고, UI 잉크표시는 2d 직사각형 면이므로 차오르는 정도의 표기가 달라요.
        float scaledRatio = ratio * 0.5f;
        // 그래프상 반전해야 제대로 잉크가 소모됌.
        scaledRatio = 1f - scaledRatio;

        _injectorRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_fillAmountPropID, scaledRatio);
        _mpb.SetColor(_teamColorPropID, _teamColor);
        _injectorRenderer.SetPropertyBlock(_mpb);
    }

    private void Dash(InputAction.CallbackContext context)
    {
        //if (playerPhotonView.Owner != PhotonNetwork.LocalPlayer) return;
        if ((Status.Ink >= dashInkCost) && !isTryGrapple)
        {
            DashMaxSpeedChanger();
            InkEvents.ConsumeInk(dashInkCost);
            isDash = true;
            ////Debug.Log("dash");
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Dash, 0.5f);
        }
    }
    
    public void DashMaxSpeedChanger()
    {
        // 1. 핵심: 만약 이전에 실행된 시퀀스가 있다면 즉시 중지하고 제거합니다.
        //    이것 덕분에 유지 시간이 초기화됩니다.
        if (mySequence != null && mySequence.IsActive())
        {
            mySequence.Kill();
        }

        // 2. 새로운 시퀀스를 생성합니다.
        mySequence = DOTween.Sequence();

        // 3. 시퀀스에 애니메이션을 순서대로 추가(Append)합니다.
        mySequence.Append(DOTween.To(() => Status.MaximumMoveSpeed, x => Status.MaximumMoveSpeed = x, boostMaxSpeed, raiseDuration)) // myFloat을 targetValue로
            .AppendInterval(holdDuration) // holdDuration 만큼 대기
            .Append(DOTween.To(() => Status.MaximumMoveSpeed, x => Status.MaximumMoveSpeed = x, originMaxSpeed, lowerDuration)); // myFloat을 originalValue로
    }

    private void AttackStart(InputAction.CallbackContext context)
    {
        // if (!isTryGrapple) >> 해당 판정 구문은 무기로 이동
            isAttack = true;
    }

    private void AttackCancel(InputAction.CallbackContext context)
    {
        isAttack = false;
    }

    private void GrappleStart(InputAction.CallbackContext context)
    {
        isTryGrapple = true;
        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Hook, 0.5f);
    }

    private void GrappleCancel(InputAction.CallbackContext context)
    {
        isTryGrapple = false;
    }

    //테스트용 체력 감소
    private void TestKill(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine) return;
        UpdateHealthByDelta(-30f);
        //Debug.Log("내 체력 : " + Status.Health);
    }

    //테스트용 물감 가득 차게
    private void FullInk(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine) return;
        InkEvents.FullInk();
    }

    public void InitializeStatus(PlayerStatus initialStatus, PlayerInfo playerInfo)
    {
        Status = initialStatus;
        Info = playerInfo;

        //characterRenderer.material.color = playerInfo.TeamColor;

        OnHealthChanged?.Invoke(Status.Health);

        //OnHpUIChanged?.Invoke(Status.Health);  // HP UI만 갱신 추가함
        //OnInkChanged?.Invoke(Status.Ink);      // 기존 Ink 이벤트는 그대로 추가함
        //OnInkChanged?.Invoke(Status.Ink);

        photonView.RPC(nameof(SetLayer), RpcTarget.AllBuffered, Info.TeamId);
        photonView.RPC(nameof(UpdateColor), RpcTarget.AllBuffered, Info.TeamId); // AllViaServer로 하면 로컬에서 안바꿔줘도 될 거 같은데 > 테스트 ㄱㄱ

        // 플레이어 팀에 따라 카메라 전방 주시하도록.
        playerCameraController.InitRotation(Info.TeamId);

        //Debug.Log($"[Initialize] {playerInfo.Nickname} " +$"정보: Seq={playerInfo.SequenceNumber}, " +$"정보: TeamID={playerInfo.TeamId}");
    }

    [PunRPC]
    void SetLayer(byte teamID)
    {
        // 팀ID에 따라 해당 팀 플레이어에 레이어 부여
        if (Info.TeamId == 0)
        {
            gameObject.layer = LayerMask.NameToLayer(LayerName_TeamA);
        }
        else if (Info.TeamId == 1)
        {
            gameObject.layer = LayerMask.NameToLayer(LayerName_TeamB);
        }
    }

    // 팀컬러 로컬에서만 변화
    private void PlayerColorSync()
    {
        if (Info != null)
            UpdateColor(Info.TeamId);

        // 잉크 게이지의 색상을 팀 색상으로 변경
        Color teamColor = Color.white;
        switch ((Team)Info.TeamId)
        {
            case Team.TeamA:
                teamColor = DataManager.Instance.colorTeamA;
                break;
            case Team.TeamB:
                teamColor = DataManager.Instance.colorTeamB;
                break;
            default:
                break;
        }
        if (TryGetComponent(out PlayerInkHandler inkHandler))
            inkHandler.SetInkBarColor(teamColor);

        //if (photonView.Owner.CustomProperties.TryGetValue(
        //        CustomPropKey.TeamColor, out var colObj)
        //    && colObj is Vector3 v)
        //{
        //    characterRenderer.material.color = new Color(v.x, v.y, v.z);
        //}

        // 3) injectorInk 파츠 컬러링
        _teamColor = teamColor;
        if (_injectorRenderer != null)
        {
            _injectorRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_teamColorPropID, _teamColor);
            _injectorRenderer.SetPropertyBlock(_mpb);
        }
    }
    public void SetHealth(float newHealth)//추가함
    {
        Status.Health = Mathf.Clamp(newHealth, 0f, Status.MaximumHealth);
        OnHealthChanged?.Invoke(Status.Health);
        //OnHpUIChanged?.Invoke(Status.Health); // HP UI 갱신 추가함
        if (Status.Health <= 0f)
        {
            if (deathEffectPrefab != null)
            {
                photonView.RPC(nameof(RPC_SpawnDeathEffect), RpcTarget.All, transform.position + Vector3.up * 1.5f);
            }

            OnDeath?.Invoke(this);
        }
    }

    [PunRPC]
    public void UpdateHealthByDelta(float delta)
    {
        if (!photonView.IsMine) return;
        //Debug.Log($"피격 : {Info.Nickname} {delta}");
        if (isDamaged)
        {
            Status.Health = Mathf.Clamp(Status.Health + delta, 0f, Status.MaximumHealth);
        }

        if (delta < 0f)
        {
            OnDamaged?.Invoke();
        }

        OnHealthChanged?.Invoke(Status.Health);
        //OnHpUIChanged?.Invoke(Status.Health); // HP UI 갱신 추가함
        if (Status.Health <= 0f)
        {
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Die,1f);

            if (deathEffectPrefab != null)
            {
                photonView.RPC(nameof(RPC_SpawnDeathEffect), RpcTarget.All, transform.position + Vector3.up * 1.5f);
               
            }
            ////Debug.Log("피가 없어");
            OnBeforeDeath?.Invoke();
            OnDeath?.Invoke(this);
        }
    }
    [PunRPC]
    public void RPC_SpawnDeathEffect(Vector3 position)
    {
        if (deathEffectPrefab == null) return;

        GameObject effect = Instantiate(deathEffectPrefab, position, Quaternion.identity);

        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = characterRenderer.material.color;
        }

        Destroy(effect, 2f);
    }

    [PunRPC]
    public void TakeDamage(float damage, int actorNum, PhotonMessageInfo info)
    {

        if (!photonView.IsMine) return; // 이것도 필요없을지도? 맞은 플레이어 해당 포톤뷰의 Owner에게만 보내는데? >> isMine 설정이 되어 있는데도 문제가 생긴다?
        //Debug.Log($"[DAMAGE] {photonView.Owner.NickName} took {damage} dmg  " + $"(before={Status.Health}, after={Status.Health - damage})");
        ////Debug.Log($"[TakeDamage] {PhotonNetwork.LocalPlayer.NickName} ({photonView.ViewID}) got hit! Called by {info.Sender.NickName}, isMine={photonView.IsMine} hitby {AttackerName} ");
        bool wasAlive = Status.Health > 0f;
        // ‘맞은 쪽’ 클라이언트가 자기 체력을 깎도록
        UpdateHealthByDelta(-damage);

        // 체력이 0 이하가 되고, 이전에는 살아있었다면 → 킬 확정
        if (Status.Health <= 0f && wasAlive)
        {
            // 이제 단 한 번의 RPC로 모든 처리를 위임
            RPCManager.Instance.photonView.RPC(
                nameof(RPCManager.RPC_RegisterKill),
                RpcTarget.AllViaServer,
                actorNum,
                Info.SequenceNumber);
        }
    }

    [PunRPC]
    void RPC_ShowKillFeed(int killerID, int victimID)
    {
        UIManager.Instance.GameHUDUI.AddKillFeedEntry(killerID, victimID);
    }

    [PunRPC]
    void UpdateColor(byte teamID)
    {
        // 혹시 아직 캐싱이 안 되었다면
        if (characterRenderer == null)
            characterRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        Team team = (Team)teamID;
        Color teamColor = Color.white;
        switch (team)
        {
            case Team.TeamA:
                teamColor = DataManager.Instance.colorTeamA;
                break;
            case Team.TeamB:
                teamColor = DataManager.Instance.colorTeamB;
                break;
            default:
                break;
        }

        if (characterRenderer == null) return;

        // 머티리얼이 여러 개일 수 있으니 배열로 돌면서
        foreach (var mat in characterRenderer.materials)
        {
            // URP Lit: Base Color 프로퍼티
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", teamColor);
            // Standard: _Color 프로퍼티
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", teamColor);
        }

        // 2) injectorInk 도 컬러 세팅
        _teamColor = teamColor;
        if (_injectorRenderer != null)
        {
            _injectorRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_teamColorPropID, _teamColor);
            _injectorRenderer.SetPropertyBlock(_mpb);
        }
    }

    // [PunRPC]
    // public void UpdateInkByDelta(float delta)
    // {
    //     Status.Ink = Mathf.Clamp(Status.Ink + delta, 0f, Status.MaximumInk);
    //     OnInkChanged?.Invoke(Status.Ink);
    // }

    //여기 필요한 부분인가?? 게임 메니저에서 플레이어 생성하고 초기화 하는데 여기서도 또 하넹??
    //네 아마도 이건 필요한부분입니다. 빼면 내 컴퓨터에서 다른플레이어들이 생성될때 기본값으로 초기화가 안될거에요.
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 플레이어가 생성될 때마다 호출되는 곳
        var props = photonView.Owner.CustomProperties;
        PlayerInfo playerInfo = PlayerInfo.FromHashtable(props);
        // Status는 초기값(로컬과 동일) 혹은 서버에서 받아온 값으로 설정
        var initialStatus = new PlayerStatus(100f, 100f, 100f, 100f, 30f, 30f, 30f, 45f);
        InitializeStatus(initialStatus, playerInfo);
    }

    [PunRPC]
    public void OffPlayer()
    {
        gameObject.SetActive(false);
    }
    [PunRPC]
    public void OnPlayer()
    {
        gameObject.SetActive(true);
    }

    [PunRPC]
    public void EquipWeapon(int weaponIndex)
    {
        // 생성할 무기 프리팹 가져오기
        GameObject weaponPrefab = DataManager.Instance.WeaponPrefabs[weaponIndex];
        if (weaponPrefab == null)
        {
            //Debug.LogError("해당 경로에 무기가 없습니다.");
            return;
        }

        // 대기방에서 해당 플레이어가 설정한 무기 생성
        GameObject instantiateWeapon = Instantiate(weaponPrefab, WeaponTransform.position, WeaponTransform.rotation);
        // 플레이어를 부모로
        instantiateWeapon.transform.SetParent(this.transform);
        // 팔 꺾인 거 잡기
        instantiateWeapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
        //무기 변수에 생성한 무기 넣어주기
        weapon = instantiateWeapon.GetComponent<Weapon_Base>();
        //Debug.Log(weapon);
    }

    [PunRPC]
    public void DestroyWeapon()
    {
        Destroy(weapon.gameObject);
    }

    //지금 내부 트리거들이 전부 반응 해서 2번 반응하는데 일단 돌아가긴해(창민)
    private void OnTriggerEnter(Collider other)
    {
        if ((Barriers & (1 << other.gameObject.layer)) != 0)
        {
            ////Debug.Log("데미지 금지");
            isDamaged = false;
            //공격도 다시 못 하게
            isAttack = false;
            //공격 금지
            playerAction.PlayerActionMap.Attack.started -= AttackStart;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((Barriers & (1 << other.gameObject.layer)) != 0)
        {
            ////Debug.Log("데미지 허용");
            //isDamaged = true;
            //공격 허가
            playerAction.PlayerActionMap.Attack.started += AttackStart;

            if (!photonView.IsMine) return;
            bool invincibleNow = (_effectHandler != null && _effectHandler.IsInvincible);

            if (!invincibleNow)
            {
                isDamaged = true;
            }
        }
    }
}