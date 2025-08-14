using Photon.Pun;
using UnityEngine;

public class Weapon_Base : MonoBehaviour
{
    // 팀ID
    protected byte teamID;

    // 남은 딜레이 시간
    protected float atkDelay_Left = 0f, atkDelay_Max;

    // 발사 가능한 상태인지 여부
    public bool isAttackable { get; set; } = true;

    [SerializeField] private float fireOffSet = -5f;

    // 탄이 나가는 트랜스폼(위치, 회전 정보)
    public Transform firePos;

    // 무기 모델링이 들어 있는 트랜스폼(총 외형)
    public Transform gunModel;
    protected int myTeamLayer;

    // 무기 발사 조작, 피아식별 값을 가져올 컴포넌트
    protected PlayerHandler playerHandler;

    //플레이어 카메라 콘트롤러
    private PlayerCameraController playerCameraController;

    // 총알 스케일
    public float bulletScale = 1f;

    // 공격에 드는 잉크 코스트
    protected float attackInkCost = 5f;

    //무기랑 레이쏜 위치 사이 거리
    private float hitPointToHockDistance;

    //조준점으로 조준할 최소 거리
    private float minShootDistance = 3f;

    // 쏠 방향
    private Vector3 shootDir;

    // 발사 불가 경고 sfx 관련
    // 해당 sfx 재생 가능 상태까지 남은 딜레이
    protected float warningSoundPlayDelay = 0;
    // 발사 후 발사 불가 경고 sfx 재생 불가 딜레이
    protected readonly float warningSoundPlayDelay_Shoot = 0.5f,
                   // 해당 sfx 플레이 시 딜레이 시간, 
                   warningSoundPlayDelay_Played = 1f,
                   // 재생 볼륨 스케일
                   volumeScale = 2;

    protected virtual void Start()
    {
        playerHandler = GetComponentInParent<PlayerHandler>(true);
        if (playerHandler != null)
        {
            playerHandler.OnBulletSizeChanged += OnBulletScaleChanged;
            playerCameraController = playerHandler.playerCameraController;
        }
    }

    protected virtual void OnEnable()
    {
        isAttackable = true;
        atkDelay_Left = warningSoundPlayDelay = 0;
    }


    private void OnBulletScaleChanged(float scale)
    {
        bulletScale = scale;
    }

    protected virtual void FixedUpdate()
    {
        if (!playerHandler.photonView.IsMine) return;
        // 무기 쿨타임
        atkDelay_Left -= Time.fixedDeltaTime;
        // 공격 가능한 상태로 전환 및 크로스헤어 표시 전환
        // 조건: (공격 딜레이가 끝남 and 한발 이상 쏠 만큼 잉크가 남아있음) , 그래플 훅이 작동하지 않을 때 >> 이때만 공격 가능
        bool isInkEnough = attackInkCost <= playerHandler.Status.Ink;
        CrosshairSwitcher.Instance.IsShootable = isInkEnough && !playerHandler.isTryGrapple;
        isAttackable = (atkDelay_Left < 0 && isInkEnough) && !playerHandler.isTryGrapple;

        // 무기 발사 조작 + 발사 가능 상태 >> 발사
        if (playerHandler.isAttack)
        {
            if (isAttackable)
            {
                // 카메라, 발사방향 동기화
                UpdateCameraWeaponSync();
                InkEvents.ConsumeInk(attackInkCost);
                Attack();
            }
            else if (!isInkEnough && warningSoundPlayDelay < 0)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Warning, volumeScale);
                warningSoundPlayDelay = warningSoundPlayDelay_Played;
            }
        }
        else
        {
            // 발사 불가 sfx 딜레이 세어주기
            warningSoundPlayDelay -= Time.fixedDeltaTime;
            //일단 임시 방편
            if (!playerHandler.isAttack)
            {
                gunModel.rotation = Quaternion.LookRotation(playerHandler.transform.forward);
            }
        }
    }

    protected virtual void Attack()
    {
        SetDelay(atkDelay_Max);
        // 발사 불가 경고 사운드 재생 딜레이 부여
        warningSoundPlayDelay = warningSoundPlayDelay_Shoot;
    }

    protected virtual void SetDelay(float atkDelay)
    {
        // 발사 딜레이 부여
        isAttackable = false;
        atkDelay_Left = atkDelay;
    }

    protected void UpdateCameraWeaponSync()
    {
        // 카메라의 위치와 정면 방향을 가져옵니다.
        Transform cameraTransform = playerHandler.playerCameraController.playerVirtualCamera.transform;
        Vector3 cameraPosition = cameraTransform.position;
        Vector3 cameraDirection = cameraTransform.forward;

        // Ray를 생성합니다.
        Ray flontCameraRay = new Ray(playerHandler.transform.position
                                     + playerCameraController.cameraHeightOffset, cameraDirection);
        float maxDistance = 1000f; // Ray의 최대 길이를 적절히 설정하는 것이 좋습니다. (10000f는 너무 길 수 있습니다)

        // Raycast를 실행합니다.
        if (Physics.Raycast(flontCameraRay, out RaycastHit hit, maxDistance, myTeamLayer))
        {
            // Raycast가 myTeamLayer붙은 오브젝트가 맞았을 경우:
            // 카메라 위치에서 충돌 지점까지 파란색 선을 그립니다.
            ////Debug.DrawLine(cameraPosition, hit.point, Color.blue,10f);

            hitPointToHockDistance = Vector3.Distance(hit.point, transform.position);
            // // 레이 맞은 위치와 내 위치(또는 총구 위치)를 계산해서
            // if (hitPointToHockDistance > minShootDistance)
            // {
            //     // 거리가 충분하면: 총구가 레이 맞은 위치를 바라보게 함
            // 1. 총구에서 목표 지점(hit.point)까지의 '방향' 벡터를 계산합니다.
            Vector3 directionToHit = hit.point - gunModel.position;
            
            // 2. 총구가 계산된 방향을 바라보도록 회전시킵니다.
            gunModel.rotation = Quaternion.LookRotation(directionToHit);
            // }
            // else
            // {
            //     // 거리가 너무 가까우면: 총구가 왼쪽 앞 대각선을 바라보게 함
            //     // 1. 왼쪽 앞 대각선 '방향' 벡터를 계산합니다.
            //     Vector3 diagonalDirection = transform.forward*3f + (-transform.right);
            //
            //     // 2. 총구가 그 대각선 방향을 바라보도록 회전시킵니다.
            //     gunModel.rotation = Quaternion.LookRotation(diagonalDirection);
            // }
        }
        else
        {
            // Raycast가 아무것도 맞추지 못했을 경우:
            // 카메라 위치에서 최대 거리까지 빨간색 선(Ray)을 그립니다.
            ////Debug.DrawRay(cameraPosition, cameraDirection * maxDistance, Color.red,10f);

            // 레이가 아무것에도 맞지 않았을 경우
            // flontCameraRay의 시작점으로부터 maxDistance만큼 떨어진 지점을 목표로 설정
            Vector3 endPoint = flontCameraRay.GetPoint(maxDistance);
            gunModel.rotation = Quaternion.LookRotation(endPoint-transform.position);
        }
        // 보너스: 총구가 현재 어느 방향을 향하고 있는지 초록색 선으로 표시합니다.
        ////Debug.DrawRay(gunModel.position, gunModel.forward * 5f, Color.green,10f);
    }


    // 무기 데이터에서 자주 쓰는 값들을 변수로 받아두기
    protected virtual void InitByData() { }

    private void OnDestroy()
    {
        if (playerHandler != null)
        {
            // ★ 반드시 해제
            playerHandler.OnBulletSizeChanged -= OnBulletScaleChanged;
        }
    }

    // 이펙트 호출(히트스캔 타입이 사용)
    protected void CallEffect(EffectData effectData, Vector3 positon, Quaternion rotation, float scale = 1)
    {
        // 각 클라이언트에서 해당하는 VFX, SFX를 호출하도록 브로드캐스트
        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.SpawnEffect), RpcTarget.AllViaServer,
            (byte)teamID,
            (int)effectData.effect,
            effectData.lifeTime,
            (int)effectData.sfxClip,
            effectData.sfxVolumeScale,
            positon,
            rotation,
            scale);
    }

    protected void CallVFX(EffectData effectData, Vector3 positon, Quaternion rotation, float scale = 1)
    {
        // 각 클라이언트에서 해당하는 VFX, SFX를 호출하도록 브로드캐스트
        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.SpawnEffect_VFXOnly),
            RpcTarget.AllViaServer,
            (byte)teamID,
            (int)effectData.effect,
            effectData.lifeTime,
            positon,
            rotation,
            scale);
    }

    protected void CallSFX(EffectData effectData, Vector3 positon, float scale = 1)
    {
        // 각 클라이언트에서 해당하는 VFX, SFX를 호출하도록 브로드캐스트
        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.SpawnEffect_SFXOnly),
            RpcTarget.AllViaServer,
            (int)effectData.sfxClip,
            effectData.sfxVolumeScale,
            positon
        );
    }
}


// 기존 위치 FixedUpdate() 안
// 제외 사유
// 1. 초기화가 안되면 Start()에서 에러가 뜨기에 지속적으로 체크할 필요가 없음 (최적화)
// 2. 플레이어 생성 후 무기가 생성되기에 순서 상으로도 문제가 없을 것

// 창민님 작업 부분
// playerHandler 초기화 체크
//if (playerHandler == null) return;
//// 카메라 컨트롤러 체크
//var camCtrl = playerHandler.playerCameraController;
//if (camCtrl == null) return;
//// 실제 VirtualCamera 할당 체크
//var vcam = camCtrl.playerVirtualCamera;
//if (vcam == null) return;