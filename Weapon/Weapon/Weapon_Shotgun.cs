using Photon.Pun;
using UnityEngine;

public class Weapon_Shotgun : Weapon_Base
{
    [SerializeField] WeaponData_HitScan weaponData_HitScan;

    [SerializeField, Tooltip("샷건의 레이캐스트 횟수")]
    int bulletCount = 32;

    [SerializeField, Tooltip("샷건의 방사형 각도 최대 범위")]
    float spreadAngle = 15f;

    // 발사 때 사용하는 값들(자주 쓰는 값)은 변수로 담아두고 쓰기
    // 물감 반지름, 대미지, 레이캐스트 사거리
    float stampRadius, damage, range;

    // 발사, 착탄 이펙트 데이터
    EffectData muzzleFlash, hitEffect;

    // 타겟 레이어 : 히트 스캔 방식일 경우 탄환을 쓰지 않기에 적/색칠 가능 오브젝트 레이어를 기억하게끔
    LayerMask targetLayer = 1 << (int)EnumLayer.LayerType.Map_Printable,
        enemyTeamLayer;

    protected override void Start()
    {
        base.Start();
        InitByData();
        if (playerHandler != null)
        {
            teamID = playerHandler.Info.TeamId;
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

            targetLayer |= enemyTeamLayer;
        }
        //else
        //{
        //    //Debug.LogError("PlayerHandler 못 찾았어요");
        //}
    }

    // 자주 쓰는 데이터는 담아두고 쉽게 참조할 수 있도록
    protected override void InitByData()
    {
        atkDelay_Max = weaponData_HitScan.atkDelay;
        attackInkCost = weaponData_HitScan.inkCost;
        stampRadius = weaponData_HitScan.stampData.radius;
        damage = weaponData_HitScan.damage;
        muzzleFlash = weaponData_HitScan.muzzleFlash;
        hitEffect = weaponData_HitScan.hitEffect;
        range = weaponData_HitScan.range;
    }

#if UNITY_EDITOR
    // 히트스캔 방식 사거리 확인용(디버그용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Quaternion rotation = Quaternion.LookRotation(firePos.forward);
        Gizmos.DrawLine(firePos.position, firePos.position + firePos.forward * range);
    }
#endif

    protected override void Attack()
    {
        //SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Shotgun);

        // 발사 위치와 각도
        Vector3 firePosition = firePos.position;
        Quaternion rotation = Quaternion.LookRotation(firePos.forward);
        // 발사 이펙트 호출
        CallEffect(muzzleFlash, firePosition, rotation, bulletScale);
        // sfx 호출 여부를 판정하기 위한 변수
        bool isHit = false;

        for (int i = 0; i < bulletCount * bulletScale; i++)
        {
            // 해당 탄환을 쏠 랜덤 각도 생성
            float x = Random.Range(-spreadAngle, spreadAngle);
            float y = Random.Range(-spreadAngle, spreadAngle);
            Vector3 shootDir = Quaternion.Euler(x, y, 0) * firePos.forward;

            if (Physics.Raycast(firePosition, shootDir, out RaycastHit hit, range * bulletScale, targetLayer))
            {
                // 포톤뷰를 가진 오브젝트 중 충돌 가능한 것: 플레이어, 색칠 가능한 맵 오브젝트
                PhotonView targetPv = hit.transform.GetComponentInParent<PhotonView>();
                if (targetPv != null)
                {
                    // 접점
                    Vector3 hitPoint = hit.point;
                    // 착탄 이펙트 호출 (샷건은 시각적 효과만 > sfx를 함께 호출하게 되면 한번에 여러번의 sfx를 중첩 호출하게 되어 아주 큰 소리가 남)
                    CallVFX(hitEffect, hitPoint, Quaternion.LookRotation(hit.normal), bulletScale);
                    // 공격이 끝난 후 한번만 착탄 sfx를 호출할 수 있도록 bool 값 변경
                    isHit = true;
                    // 색칠 가능한 오브젝트에 맞았다면, 색칠
                    if (targetPv.TryGetComponent(out PaintableObject paintable))
                    {
                        int viewID = paintable.GetComponent<PhotonView>().ViewID;

                        // 모든 클라이언트에 색칠 명령
                        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.CallPaint),
                            RpcTarget.AllViaServer, viewID, hitPoint, (byte)teamID, stampRadius * bulletScale);
                    }

                    // 해당 총알이 지금 어떤 팀인가에 따라 대미지를 줄 수 있는 타겟 판정
                    if ((1 << hit.transform.gameObject.layer) == enemyTeamLayer)
                    {
                        //Debug.Log($"[HIT] {PhotonNetwork.LocalPlayer.NickName} → {targetPv.Owner.NickName}  " +$"damage={damage}");
                        // 피격당한 플레이어의 클라이언트에만 피격 메시지 남김
                        //RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.ShowHitMessage),
                        //    targetPv.Owner, PhotonNetwork.LocalPlayer.NickName);
                        // 대미지 계산
                        PlayerHandler ph = targetPv.GetComponent<PlayerHandler>();
                        targetPv.RPC(nameof(ph.TakeDamage), targetPv.Owner, damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    }
                }
            }
        }

        // 맞은 타겟이 있다면 1번만 sfx 재생
        if (isHit)
            CallSFX(hitEffect, firePosition);


        // 발사 후처리
        base.Attack();
    }
}