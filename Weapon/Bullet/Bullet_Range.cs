using Photon.Pun;
using UnityEngine;

public class Bullet_Range : Bullet_Base
{
    // 범위 공격의 구형 범위의 반지름
    [Header("Range Parameter"), SerializeField] float range;

    // 탄이 닿은 위치로부터 구형 레이캐스트 중심점과의 이격(맞은 지점에 제대로 색칠되게 하기 위한 값) 
    float interval = 0.3f;

    protected override void FixedUpdate()
    {
        // 초기에는 콜라이더 크기가 작았다가 날아가는 도중 점점 커지게(발사 때 인근 벽에 탄환이 맞는 문제에 대한 해결책)
        if (sphereCollider.radius < colsizeMax)
            sphereCollider.radius = Mathf.Min(sphereCollider.radius + changeRateColsize, colsizeMax);

        // 총알 생명주기
        remainLife -= Time.fixedDeltaTime;
        // 생명주기가 끝났다면 오브젝트 풀로 돌아가기
        if (remainLife <= 0)
        {
            // 충돌 접점 위치
            Vector3 hitPoint = transform.position;
            Vector3 hitNormal = transform.forward;
            // 범위 공격 후 탄환 복귀
            RangeHit(hitPoint, hitNormal);
        }

        // 중력의 영향을 받아 y축 방향 속도 변화
        rigid.AddForce(Vector3.up * bulletData.gravityScale, ForceMode.Acceleration);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        // 충돌 접점 위치
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = -collision.contacts[0].normal;
        // 범위 공격 후 탄환 복귀
        RangeHit(hitPoint, hitNormal);
    }

    void RangeHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 공격/색칠
        // 충돌 위치로부터 구형으로 볌위 내 퍼지게
        // 맞은 방향에서 살짝 위로 띄워서 해야 제대로 맞음
        RaycastHit[] hits = Physics.SphereCastAll(hitPoint, range, hitNormal, interval);

        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 point = hits[i].point;
            GameObject hitObj = hits[i].collider.gameObject;
            int hitLayer = hitObj.layer;

            // 포톤뷰를 가진 오브젝트 중 충돌 가능한 것: 플레이어, 색칠 가능한 맵 오브젝트
            PhotonView targetPv = hitObj.GetComponentInParent<PhotonView>();
            if (targetPv != null)
            {
                // 색칠 가능한 오브젝트에 맞았다면, 색칠
                if (targetPv.IsMine && hitObj.TryGetComponent(out PaintableObject paintable))
                {
                    int viewID = paintable.GetComponent<PhotonView>().ViewID;

                    // 모든 클라이언트에 색칠 명령
                    RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.CallPaint), RpcTarget.AllViaServer, viewID, hitPoint, (byte)teamID, currentStampRadius);
                }
                if (ownerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                    // 해당 총알이 지금 어떤 팀인가에 따라 대미지를 줄 수 있는 타겟 판정
                    if ((teamID == Team.TeamA && hitLayer == (int)EnumLayer.LayerType.TeamB) ||
                       (teamID == Team.TeamB && hitLayer == (int)EnumLayer.LayerType.TeamA))
                    {
                        //Debug.Log($"[HIT] {PhotonNetwork.LocalPlayer.NickName} → {targetPv.Owner.NickName}  " + $"damage={bulletData.damage}");

                        // 그런데 왜 RPC 통신을 2번 하지?
                        // 피격당한 플레이어의 클라이언트에만 피격 메시지 남김
                        //RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.ShowHitMessage), targetPv.Owner, PhotonNetwork.LocalPlayer.NickName);
                        // 대미지 계산
                        PlayerHandler ph = targetPv.GetComponent<PlayerHandler>();
                        targetPv.RPC(nameof(ph.TakeDamage), targetPv.Owner, bulletData.damage, PhotonNetwork.LocalPlayer.ActorNumber);
                        //targetPv.RPC(nameof(ph.UpdateHealthByDelta), targetPv.Owner, bulletData.damage);

                    }

            }
        }

        // 착탄 이펙트 호출
        // VFX
        CallEffect(bulletData.hitEffect, hitPoint, Quaternion.LookRotation(hitNormal), changedLocalScale);
        // SFX
        if (ownerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            RPCManager.Instance.photonView_RPCManager.RPC(
        nameof(RPCManager.Instance.SpawnEffect_SFX3D),
        RpcTarget.All,
        bulletData.hitEffect.sfxClip,         // 이펙트 데이터에 지정된 폭발 사운드 클립
        bulletData.hitEffect.sfxVolumeScale,  // 볼륨 스케일
        hitPoint                              // 재생 위치
        );

        // 충돌 처리가 끝난 후 오브젝트 풀로 복귀
        ObjectPoolManager.Instance.ReturnBulletPool(teamID, bulletIndex, gameObject);
    }
}
