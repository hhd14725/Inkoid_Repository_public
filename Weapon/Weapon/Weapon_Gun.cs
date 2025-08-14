using Photon.Pun;
using UnityEngine;

public class Weapon_Gun : Weapon_Base
{
    // 무기 데이터
    [SerializeField] WeaponData_Bullet weaponData;

    // 탄환 인덱스
    int bulletIndex;

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
                myTeamLayer = 1 << 1 << (int)EnumLayer.LayerType.TeamB
                              | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                              | (1 << (int)EnumLayer.LayerType.Map_Printable);
            }
            else if (teamID == 1)
            {
                gameObject.layer = (int)EnumLayer.LayerType.TeamB;
                myTeamLayer = 1 << (int)EnumLayer.LayerType.TeamA
                              | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                              | (1 << (int)EnumLayer.LayerType.Map_Printable);
                ;
            }
        }
        //else
        //{
        //    //Debug.LogError("PlayerHandler 못 찾았어요");
        //}
    }

    protected override void InitByData()
    {
        attackInkCost = weaponData.inkCost;
        atkDelay_Max = weaponData.atkDelay;
        bulletIndex = (int)weaponData.bullet;
    }

    // 공격 로직 외에는 모두 베이스와 동일할 것으로 예상
    // 필요하면 더 세분화
    protected override void Attack()
    {
        // 해당 무기의 총알 1발 발사
        RPCManager.Instance.photonView_RPCManager.RPC(nameof(RPCManager.Instance.SpawnBullet), RpcTarget.AllViaServer,
            teamID,
            bulletIndex,
            firePos.position,
            Quaternion.LookRotation(firePos.forward), bulletScale, PhotonNetwork.LocalPlayer.ActorNumber);
        // 발사 후처리
        base.Attack();
    }
}