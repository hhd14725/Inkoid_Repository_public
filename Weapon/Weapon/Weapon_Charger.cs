using Photon.Pun;
using UnityEngine;

public class Weapon_Charger : Weapon_Base
{
    // 충전률을 보여주는 UI
    ChargeUI chargeUI;

    // 레이저 조준선을 표현할 라인 랜더러
    LineRenderer lineRenderer;

    // 모든 클라이언트에서 같은 라인랜더러를 지칭하게 할 인덱스
    int id_lineRenderer, id_charger;
    // 0: 라인 시작점, 1: 라인 끝점
    // 현재의 라인랜더러 두 지점
    Vector3[] currentLaserPos = new Vector3[2],
              // 통신으로 수신한 다음 라인랜더러 두 지점
              receivedLaserPos = new Vector3[2];
    public void ReceiveLaserPos(Vector3 startPos, Vector3 endPos)
    {
        receivedLaserPos[0] = startPos;
        receivedLaserPos[1] = endPos;
    }

    // 무기 데이터 (차저는 2가지 타입을 함께 사용)
    [SerializeField] WeaponData_Bullet weaponData_Bullet;
    [SerializeField] WeaponData_Charger weaponData_HitScan;
    float lerpSpeed = 10f;
    readonly float sendInterval = 0.05f; // 송신 주기
    float sendTimer = 0; // 송신 타이머

    // 발사 때 사용하는 값들(자주 쓰는 값)은 변수로 담아두고 쓰기

    // 탄환 타입
    // 탄환 인덱스
    int bulletIndex;

    // 히트 스캔 타입
    // 물감: 반지름, 공격: 대미지, 레이캐스트 사거리, 빔 반지름
    float stampRadius, damage, range, radius;

    // 발사, 착탄 이펙트 데이터
    EffectData muzzleFlash, hitEffect;

    // 타겟 레이어 : 히트 스캔 방식일 경우 탄환을 쓰지 않기에 적/색칠 가능 오브젝트 레이어를 기억하게끔
    LayerMask paintableLayer = 1 << (int)EnumLayer.LayerType.Map_Printable,
        enemyTeamLayer;

    // 차지형
    // 잉크 소모량 최대치, 공격 딜레이
    float attackInkCost_ChargeMax, atkDelay_Charge;

    // 차지 시간 관련 (인스펙터에서 할당 필요)
    // 충전 필요 시간(최대 충전 시간)
    [SerializeField] float chargeTime_Max,
        // 탄환형으로 발사할 때의 입력 시간 기준
        chargeTime_bulletShot = 0.3f;

    // 충전 지속 시간(공격 키를 누르고 있는 시간)
    float chargeTime = 0,
        // (현재 충전 시간 / 최대 충전 시간) 비율
        chargeRate = 0,
        // 충전량에 따른 현재 사거리
        rangeTmp = 0;

    bool isMine = false;

    private void Awake()
    {
        lineRenderer = GetComponentInChildren<LineRenderer>();
        // 모든 클라이언트에서 등록해줘야 문제가 없음(브로드캐스트하면 각 클라이언트에서 해당 컴포넌트를 지칭해야 하기에 모든 클라에서 동일하게 등록할 필요가 있음)
        if (lineRenderer != null)
        {
            // 라인랜더러를 로컬 클라이언트에 등록하고 해당 키 값을 받아옴 > 그런데 이거 각 클라별로 등록 순서 문제 없나? (RPC 통신은 버퍼로 쌓인 순서대로 들어오기에 순서가 꼬이지는 않음) > 잘 되는구만
            id_lineRenderer = RPCManager.Instance.RegisterComponent(lineRenderer);
            id_charger = RPCManager.Instance.RegisterComponent(this);

            // 무기 장착 시에는 기본적으로 레이저가 보이지 않음
            RPCManager.Instance.SetActive_AllClients(id_lineRenderer, false);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (isMine && chargeUI != null)
        {
            chargeUI.gameObject.SetActive(true);
        }
    }

    protected override void Start()
    {
        base.Start();
        InitByData();

        isMine = playerHandler.playerPhotonView.IsMine;

        if (playerHandler != null)
        {
            teamID = playerHandler.Info.TeamId;

            // 모든 클라이언트에서 실행할 부분
            Color teamColor = Color.white, teamColorDark = Color.white;
            if (teamID == 0)
            {
                teamColor = DataManager.Instance.colorTeamA;
                teamColorDark = DataManager.Instance.colorTeamA_Darker;
            }
            else if (teamID == 1)
            {
                teamColor = DataManager.Instance.colorTeamB;
                teamColorDark = DataManager.Instance.colorTeamB_Darker;
            }
            // 라인랜더러 색상을 팀 색상으로 변경
            GradientColorKey teamGradient0 = new GradientColorKey(),
                             teamGradient1 = new GradientColorKey();
            teamGradient0.color = teamColorDark;
            teamGradient1.color = teamColor;
            GradientAlphaKey gradientAlphaKey = new GradientAlphaKey();
            gradientAlphaKey.alpha = 1;
            Gradient gradient = new Gradient();
            gradient.SetKeys(new GradientColorKey[2] { teamGradient0, teamGradient1 }, new GradientAlphaKey[2] { gradientAlphaKey, gradientAlphaKey });
            lineRenderer.colorGradient = gradient;

            // 내 플레이어일 때만(해당 차저를 든 유저에 한해서 실행할 구문들)
            if (playerHandler.playerPhotonView.IsMine)
            {
                // 차지 UI 로컬 클라이언트에서 활성화
                chargeUI = playerHandler.gameObject.GetComponentInChildren<ChargeUI>(true);
                chargeUI.gameObject.SetActive(true);

                if (teamID == 0)
                {
                    gameObject.layer = (int)EnumLayer.LayerType.TeamA;
                    myTeamLayer = 1 << (int)EnumLayer.LayerType.TeamB
                                  | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                                  | (1 << (int)EnumLayer.LayerType.Map_Printable);
                    enemyTeamLayer = 1 << (int)EnumLayer.LayerType.TeamB;
                }
                else if (teamID == 1)
                {
                    gameObject.layer = (int)EnumLayer.LayerType.TeamB;
                    myTeamLayer = 1 << (int)EnumLayer.LayerType.TeamA
                                  | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                                  | (1 << (int)EnumLayer.LayerType.Map_Printable);
                    enemyTeamLayer = 1 << (int)EnumLayer.LayerType.TeamA;
                }
                // 차지 UI의 컬러를 팀 색상에 맞게 설정
                chargeUI.SetColor(teamID);
            }
        }
        //else
        //{
        //    //Debug.LogError("PlayerHandler 못 찾았어요");
        //}
    }

    // 자주 쓰는 데이터는 담아두고 쉽게 참조할 수 있도록 초기화
    protected override void InitByData()
    {
        // 탄환형
        attackInkCost = weaponData_Bullet.inkCost;
        atkDelay_Max = weaponData_Bullet.atkDelay;
        bulletIndex = (int)weaponData_Bullet.bullet;

        // 히트스캔 (차지)
        atkDelay_Charge = weaponData_HitScan.atkDelay;
        attackInkCost_ChargeMax = weaponData_HitScan.inkCost;
        stampRadius = weaponData_HitScan.stampData.radius;
        damage = weaponData_HitScan.damage;
        muzzleFlash = weaponData_HitScan.muzzleFlash;
        hitEffect = weaponData_HitScan.hitEffect;
        range = weaponData_HitScan.range;
        radius = weaponData_HitScan.radius;
    }

    private void Update()
    {
        // Update에서는 조준 레이저 그려주는 구문만
        if (lineRenderer.enabled)
        {
            // 해당 무기의 주인 클라이언트라면
            if (isMine)
            {
                // 레이저 시작점: 총구 위치
                currentLaserPos[0] = firePos.position;
                // 레이저 끝점
                currentLaserPos[1] = currentLaserPos[0] + firePos.forward * rangeTmp;
                // 라인랜더러 그려주기
                lineRenderer.SetPositions(currentLaserPos);

                // 주기마다 레이저 동기화
                sendTimer += Time.deltaTime;
                if (sendTimer >= sendInterval)
                {
                    sendTimer = 0f;
                    // 전송
                    //RPCManager.Instance.DrawLine(id_lineRenderer, currentLaserPos[0], currentLaserPos[1]);
                    RPCManager.Instance.SetLaserPos(id_charger, currentLaserPos[0], currentLaserPos[1]);
                }
            }
            // 무기의 주인이 아닌 클라이언트라면
            else
            {
                // 차저 보유자로부터 받은 receivedLaserPos를 통해 currentLaserPos를 보간하여 자연스럽게 보이게
                for (int i = 0; i < currentLaserPos.Length; i++)
                {
                    currentLaserPos[i] = Vector3.Lerp(currentLaserPos[i], receivedLaserPos[i], Time.deltaTime * lerpSpeed);
                }
                lineRenderer.SetPositions(currentLaserPos);
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (!playerHandler.photonView.IsMine) return;
        // 공격 가능한 상태로 전환 및 크로스헤어 표시 전환
        // 조건: (공격 딜레이가 끝남 and 한발 이상 쏠 만큼 잉크가 남아있음) or 그래플 훅이 작동하지 않을 때 >> 이때만 공격 가능
        isAttackable = CrosshairSwitcher.Instance.IsShootable = (atkDelay_Left < 0 && attackInkCost <= playerHandler.Status.Ink) && !playerHandler.isTryGrapple;
        if (!isAttackable)
        {
            // 무기 발사 쿨타임
            atkDelay_Left -= Time.fixedDeltaTime;
            // 발사 불가 sfx 딜레이 세어주기
            warningSoundPlayDelay -= Time.fixedDeltaTime;
            // 재생 가능한 상태에 공격키를 눌렀다면 발사 불가 경고 sfx 재생 및 다시 딜레이
            if (playerHandler.isAttack && warningSoundPlayDelay < 0)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Warning, volumeScale);
                warningSoundPlayDelay = warningSoundPlayDelay_Played;
            }

            // 충전중이던 경우 충전 표시 해제
            EndCharge();
        }
        else
        {
            // 공격 키를 누르는 동안
            if (playerHandler.isAttack)
            {
                // 공격 시작시 사운드 재생
                if (chargeTime == 0)
                {
                    //SoundManager.Instance.PlayLoopSfx(ClipIndex_SFX.mwfx_Charging);
                    SoundManager.Instance.PlaySfxAndThen(
                        this.gameObject,                  // 1. "내가" 이 사운드를 요청했다는 정보
                        ClipIndex_SFX.mwfx_Charging,      // 2. 시작할 사운드
                        chargeTime_Max,                   // 3. 재생할 시간
                        ClipIndex_SFX.mwfx_ChargingFinish    // 4. 끝나고 재생할 사운드
                    );
                }

                // 무기가 조준선 방향을 보도록
                UpdateCameraWeaponSync();

                //  충전 지속 시간 누적
                chargeTime = Mathf.Min(chargeTime + Time.fixedDeltaTime, chargeTime_Max);
                // 충전 최대량에 대한 비율
                chargeRate = chargeTime / chargeTime_Max;

                // 레이저 표시
                // lineRenderer.enabled = true;
                if (!lineRenderer.enabled)
                    RPCManager.Instance.SetActive_AllClients(id_lineRenderer, true);

                // 현재 사거리
                rangeTmp = range * chargeRate;

                // 차저 충전률 표시
                chargeUI.SetChargeRatio(chargeRate);
            }
            // 공격키를 누르다가 손을 떼었을 때, 충전 시간이 0초보다 길면, 공격 키를 눌렀다는 뜻
            else if (chargeTime > 0)
            {
                //SoundManager.Instance.StopLoopSfx();
                SoundManager.Instance.StopManagedSfx(ClipIndex_SFX.mwfx_Charging);

                // 무기가 조준선 방향을 보도록
                UpdateCameraWeaponSync();

                // 0 < chargeTime < chargeTime_bulletShot : 차지로 넘어가기 전
                if (chargeTime < chargeTime_bulletShot)
                {
                    // 탄환 발사
                    InkEvents.ConsumeInk(attackInkCost);
                    Attack();
                }
                // 차지한 만큼 공격 발사
                else
                {
                    // 차지한 양에 따라 물감 소모
                    InkEvents.ConsumeInk(attackInkCost_ChargeMax * chargeRate);
                    // 차지한 양에 따라 대미지를 주는 공격
                    ChargeAttack(chargeRate);
                }

                // 충전 초기화
                EndCharge();
            }
        }
    }

    void EndCharge()
    {
        // 충전 시간 초기화
        chargeTime = 0;
        // 충전률 UI 표시 초기화
        chargeUI.SetChargeRatio(0);
        // 조준선 레이저 모든 클라이언트에서 비활성화
        RPCManager.Instance.SetActive_AllClients(id_lineRenderer, false);
    }

    private void OnDisable()
    {
        if (isMine)
        {
            //SoundManager.Instance.StopLoopSfx();
            //SoundManager.Instance.StopAllManagedSfx();
            SoundManager.Instance.StopManagedSfx(ClipIndex_SFX.mwfx_Charging);
            chargeUI.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        RPCManager.Instance.UnregisterComponent(id_lineRenderer);            
    }

    // 기본 공격은 탄환과 같음
    protected override void Attack()
    {
        // 해당 무기의 총알 1발 발사
        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.SpawnBullet), RpcTarget.AllViaServer,
            teamID,
            bulletIndex,
            firePos.position,
            Quaternion.LookRotation(firePos.forward),
            bulletScale, PhotonNetwork.LocalPlayer.ActorNumber);
        // 발사 후처리
        base.Attack();
    }

    void ChargeAttack(float chargeRate)
    {
        // 발사 위치와 각도
        Vector3 firePosition = firePos.position;
        Vector3 shootDir = firePos.forward;

        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_ChargeShot);

        // 발사 이펙트 호출
        CallEffect(muzzleFlash, firePosition, Quaternion.LookRotation(shootDir), bulletScale * chargeRate);

        // 충전량에 따른 사거리에 맞게
        // 기존: 빛을 한줄기 쏘아 맞춘 판정... 이었는데 너무 안맞아요 ㅠ
        // if (Physics.Raycast(firePosition, shootDir, out RaycastHit hit, rangeTmp))
        // 변경: 캡슐 모양으로 캐스트하여 맞춘 판정(기존보다 공격 범위도 살짝 넓게하여 빔 판정)
        // + 건물 뚫고 뒤를 때릴 수 있게끔
        //RaycastHit[] hitTargets = Physics.CapsuleCastAll(laserStartPos, laserEndPos, radius, shootDir, rangeTmp); >> 건물을 인식 못하여 폐기

        // 캡슐 모양으로 발사하여 맞은 모든 타겟들(색칠 가능한 조형 + 상대 팀 플레이어)
        // 건물이 맞았는지 확인
        RaycastHit[] hitBuildings = Physics.RaycastAll(firePosition, shootDir, rangeTmp, paintableLayer);
        for (int i = 0; i < hitBuildings.Length; i++)
        {
            //Debug.Log(hitBuildings[i].transform.name);

            // 접점
            Vector3 hitPoint = hitBuildings[i].point;
            // 착탄 이펙트 호출 //- hits[i].normal * 0.01f
            CallEffect(hitEffect, hitPoint, Quaternion.LookRotation(hitBuildings[i].normal), bulletScale * chargeRate);

            // 색칠 가능한 맵 오브젝트는 PhotonView를 가짐
            PhotonView targetPv = hitBuildings[i].transform.GetComponentInParent<PhotonView>();
            if (targetPv != null)
            {
                // 색칠 가능한 오브젝트에 맞았다면, 색칠
                if (targetPv.TryGetComponent(out PaintableObject paintable))
                {
                    int viewID = paintable.GetComponent<PhotonView>().ViewID;

                    // 모든 클라이언트에 색칠 명령
                    RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.CallPaint),
                        RpcTarget.AllViaServer, viewID, hitPoint, (byte)teamID, stampRadius * chargeRate * bulletScale);
                }
            }
        }

        // 상대 팀 플레이어가 맞았는지 확인
        Collider[] hitColliders = Physics.OverlapCapsule(currentLaserPos[0], currentLaserPos[1], radius * chargeRate * bulletScale,
            enemyTeamLayer);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            // 접점
            Vector3 hitPoint = hitColliders[i].ClosestPoint(firePosition);
            // 법선 벡터를 통한 충돌 방향에 수직 방향의 회전각
            Quaternion hitNormal = Quaternion.Euler((hitPoint - firePosition).normalized);

            // 착탄 이펙트 호출
            CallEffect(hitEffect, hitPoint, hitNormal, chargeRate * bulletScale);

            // 맞은 플레이어의 포톤뷰
            PhotonView targetPv = hitColliders[i].transform.GetComponentInParent<PhotonView>();
            if (targetPv != null)
            {
                //Debug.Log($"[HIT] {PhotonNetwork.LocalPlayer.NickName} → {targetPv.Owner.NickName}  " +$"damage={damage}");
                // 피격당한 플레이어의 클라이언트에만 피격 메시지 남김
                //RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.ShowHitMessage),
                //    targetPv.Owner, PhotonNetwork.LocalPlayer.NickName);
                PlayerHandler ph = targetPv.GetComponent<PlayerHandler>();
                // 충전량에 대해 대미지 계산
                float damageTmp = chargeRate * damage;
                targetPv.RPC(nameof(ph.TakeDamage), targetPv.Owner, damageTmp, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        // 발사 후처리
        SetDelay(atkDelay_Charge);
    }

    protected override void SetDelay(float atkDelay)
    {
        base.SetDelay(atkDelay);
        // 발사 불가 경고 사운드 재생 딜레이 부여
        warningSoundPlayDelay = warningSoundPlayDelay_Shoot;
        // + 딜레이를 표시하기 위한 발사 불가 크로스헤어 표시
        CrosshairSwitcher.Instance.IsShootable = false;
    }


#if UNITY_EDITOR
    // 히트스캔 방식 사거리 확인용(디버그용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Quaternion rotation = Quaternion.LookRotation(firePos.forward);

        // 캡슐의 시작, 끝점
        Vector3 p1 = currentLaserPos[0];
        Vector3 p2 = currentLaserPos[1];

        // 구체 양끝
        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);

        // 측면 연결 (원기둥)
        Gizmos.DrawLine(p1 + firePos.forward * radius, p2 + firePos.forward * radius);
        Gizmos.DrawLine(p1 - firePos.forward * radius, p2 - firePos.forward * radius);
        Gizmos.DrawLine(p1 - firePos.right * radius, p2 - firePos.right * radius);
        Gizmos.DrawLine(p1 + firePos.right * radius, p2 + firePos.right * radius);
    }
#endif
}