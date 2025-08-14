using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet_Base : MonoBehaviour
{
    //쏜사람 찾아주는 용도
    public int ownerActorNumber;

    protected Rigidbody rigid;

    protected SphereCollider sphereCollider;
    protected float initColsize = 0.01f, changeRateColsize, colsizeMax;

    // 총알 공통 데이터들
    public BulletData bulletData;
    protected float remainLife;
    public float trailTime = 0.1f;
    // 총알 종류 > 풀에 돌아갈 때 필요
    public PrefabIndex_Bullet bulletIndex { get; private set; }
    // 팀 id
    public Team teamID { get; private set; }
    //아이템용 물감스탬프크기조절 변수
    protected float currentStampRadius = 1f;
    //아이템용 총알원본스케일 저장
    private Vector3 originalLocalScale;
    protected float changedLocalScale = 1;

    // 타겟 레이어 : 히트 스캔 방식일 경우 탄환을 쓰지 않기에 적/색칠 가능 오브젝트 레이어를 기억하게끔
    LayerMask paintableLayer = 1 << (int)EnumLayer.LayerType.Map_Printable,
              enemyTeamLayer,
              targetLayers;

    private void Awake()
    {
        // 충돌을 위한 리지드바디 설정
        // > 이 설정이 없으면 뚫고 지나갈 수 있음
        // > 이래도 뚫으면 더 연산이 많은 설정으로 테스트 해보기
        if (TryGetComponent(out rigid))
        {
            rigid.interpolation = RigidbodyInterpolation.Interpolate;
            rigid.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        originalLocalScale = transform.localScale;

        if(TryGetComponent(out sphereCollider))
        {
            // 콜라이더 크기 최대치를 받아옴
            colsizeMax = bulletData.colliderSizeMax;
            // FixedUpdate() 주기당 콜라이더 사이즈 변화량
            changeRateColsize = (colsizeMax - initColsize) / bulletData.lifeTime * Time.fixedDeltaTime;
        }
    }

    protected virtual void OnEnable()
    {
        // 총알 생명주기 초기화
        remainLife = bulletData.lifeTime;
        rigid.rotation = Quaternion.LookRotation(transform.forward);
        // 총알의 진행 방향 속도 초기값
        rigid.velocity = bulletData.speed * transform.forward;
        // 아이템용 물감 스탬프 크기 초기화
        currentStampRadius = bulletData.stampData.radius * transform.localScale.x; //구체라서 로컬스케일 x,y.z 중 취사선택
        // 총알 콜라이더 크기 작게 초기화
        sphereCollider.radius = initColsize;

        // 발사 이펙트 호출
        CallEffect(bulletData.muzzleFlash, transform.position, Quaternion.Euler(transform.forward));
    }

    private void OnDisable()
    {
        //풀에 반납할때 아이템을 쓰면 커졌다가 다시 원래크기로 돌아가야하니까, 풀에서 꺼낼때 원본크기로 원복시키자..
        transform.localScale = originalLocalScale;
        changedLocalScale = 1;

        rigid.velocity = Vector3.zero; // 속도 초기화
        //rigid.position = Vector3.zero; // 위치 초기화
        //rigid.rotation = Quaternion.identity; // 회전 초기화
        rigid.angularVelocity = Vector3.zero; // 회전각 초기화
                                              //rigid.
                                              //transform.position = Vector3.zero; // 위치 초기화
                                              //transform.rotation = Quaternion.identity; // 회전 초기화
    }

    protected virtual void FixedUpdate()
    {
        if(sphereCollider.radius < colsizeMax)
            sphereCollider.radius = Mathf.Min(sphereCollider.radius + changeRateColsize, colsizeMax);

        // 총알 생명주기
        remainLife -= Time.fixedDeltaTime;
        // 생명주기가 끝났다면 오브젝트 풀로 돌아가기
        if (remainLife <= 0)
            ObjectPoolManager.Instance.ReturnBulletPool(teamID, bulletIndex, gameObject);

        // 중력의 영향을 받아 y축 방향 속도 변화
        rigid.AddForce(Vector3.up * bulletData.gravityScale, ForceMode.Acceleration);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
     
        // 충돌 접점 위치
        Vector3 hitPoint = collision.contacts[0].point;
        GameObject hitObj = collision.gameObject;

        //Debug.Log($"[Bullet] 충돌한 오브젝트 이름: {collision.gameObject.name}");

        // 착탄 이펙트 호출
        if(ownerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            CallEffect(bulletData.hitEffect, hitPoint, Quaternion.LookRotation(collision.contacts[0].normal), changedLocalScale);

        // GetComponentInParent 호출 전에 레이어로 판정
        // 아마도 색칠 가능한 오브젝트에 레이어가 제대로 들어가지 않은 듯.. 원복 ㄱ
        //int layerCol = (1 << collision.gameObject.layer);
        //if ((layerCol & targetLayers) != 0)
        //{
        // 포톤뷰를 가진 오브젝트 중 충돌 가능한 것: 플레이어, 색칠 가능한 맵 오브젝트
        PhotonView targetPv = collision.collider.GetComponentInParent<PhotonView>();
        if (targetPv != null)
        {
            // 해당 클라이언트에서 발사한 총알이 아니라면 이하 색칠/대미지 주기는 작동하지 않도록 >> !targetPv.isMine이랑 기능적으로 동일함
            // 단 Owner가 null일 수 있음 >> isMine을 쓸 경우 Null이면 에러가 떠서 좀 더 빨리 알 수 있음 >> isMine 쓰죠

            // 아래 주석 친 구문 때문에 총알 바운스 문제가 생겼을지도 > 총알은 한번 부딪히면 풀로 돌아가야 하는데 리턴이 되면서 풀로 돌아가지 않고 충돌 후 튕겨나가는 것처럼 보일지도
            // 1p가 생성한 구조물이라 그런가

            // if (targetPv.Owner != PhotonNetwork.LocalPlayer) return;
            if (targetPv.IsMine)
            {
                // 색칠 가능한 오브젝트에 맞았다면, 색칠
                if (hitObj.TryGetComponent(out PaintableObject paintable))
                {
                    int viewID = paintable.GetComponent<PhotonView>().ViewID;

                    // 모든 클라이언트에 색칠 명령
                    RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.CallPaint), RpcTarget.AllViaServer, viewID, hitPoint, (byte)teamID, currentStampRadius);
                }
            }
            if (ownerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // 해당 총알이 지금 어떤 팀인가에 따라 대미지를 줄 수 있는 타겟 판정
                if (((int)teamID == 0 && collision.gameObject.layer == (int)EnumLayer.LayerType.TeamB) ||
                ((int)teamID == 1 && collision.gameObject.layer == (int)EnumLayer.LayerType.TeamA))
                {
                    //Debug.Log($"[HIT] {PhotonNetwork.LocalPlayer.NickName} → {targetPv.Owner.NickName}  " + $"damage={bulletData.damage}");

                    // 그런데 왜 RPC 통신을 2번 하지?
                    // 피격당한 플레이어의 클라이언트에만 피격 메시지 남김
                    //RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.ShowHitMessage), targetPv.Owner, PhotonNetwork.LocalPlayer.NickName);
                    // 대미지 계산
                    PlayerHandler ph = targetPv.GetComponent<PlayerHandler>();
                    targetPv.RPC(nameof(ph.TakeDamage), targetPv.Owner, bulletData.damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    //targetPv.RPC(nameof(ph.UpdateHealthByDelta), targetPv.Owner, bulletData.damage);
                    if (CrosshairSwitcher.Instance != null && CrosshairSwitcher.Instance.gameObject.activeSelf)
                    {
                        CrosshairSwitcher.Instance.ShowHitEffect();
                    }
                }
            }

        }
        // 충돌 처리가 끝난 후 오브젝트 풀로 복귀
        ObjectPoolManager.Instance.ReturnBulletPool(teamID, bulletIndex, gameObject);
    }

    // 해당 탄의 생성 때 넣어줄 데이터(해당 게임 도중에만 지속될 데이터)
    public void InitBullet(PrefabIndex_Bullet bulletIndex, Team teamID, int actorNumber)
    {
        // 생성 시 초기화
        // 탄 종류, 팀ID
        // 어떤 오브젝트 풀로 복귀해야 하는지 지정할 때 필요
        this.bulletIndex = bulletIndex;
        this.teamID = teamID;
        // 탄 충돌 판정을 위해 어느 팀의 총알인지 레이어 및 타겟 레이어 지정
        switch (teamID)
        {
            case Team.TeamA:
                gameObject.layer = (int)EnumLayer.LayerType.BulletA;
                break;
            case Team.TeamB:
                gameObject.layer = (int)EnumLayer.LayerType.BulletB;
                break;
        }
        ownerActorNumber = actorNumber;

        // 발사 이펙트 호출
        CallEffect(bulletData.muzzleFlash, transform.position, Quaternion.LookRotation(transform.forward), changedLocalScale);
    }

    protected void CallEffect(EffectData effectData, Vector3 positon, Quaternion rotation, float scale = 1)
    {
        if (ownerActorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;

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

    //아이템 대형화 소형화때 적용할 메서드
    public void ApplyItemScale(float scaleMultiplier)
    {
        // 1) 스케일 적용
        transform.localScale = originalLocalScale * scaleMultiplier;
        // 2) stamp 반경 재계산
        currentStampRadius = bulletData.stampData.radius * transform.localScale.x;
        changedLocalScale = scaleMultiplier;
    }
}
