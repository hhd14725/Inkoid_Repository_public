using UnityEngine;

public class RespawnBarrier : MonoBehaviour
{
    // 인스펙터에서 어느 팀 배리어인지 값 할당(0 or 1)
    [SerializeField] int teamID;
    // 기존의 투명도 값
    readonly float alpha = 0.4196078f;

    private void Start()
    {
        // 베이스 배리어 색상을 팀 색상으로
        Material material = GetComponent<MeshRenderer>().material;
        Color teamColor = Color.white;
        if (teamID == 0)
            teamColor = DataManager.Instance.colorTeamA;
        else if (teamID == 1)
            teamColor = DataManager.Instance.colorTeamB;
        material.color = new Color(teamColor.r, teamColor.g, teamColor.b, alpha);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 총알(Bullet_Base)인지 확인
        if (other.gameObject.TryGetComponent(out Bullet_Base bullet))
        {
            // 총알을 즉시 오브젝트 풀로 반환(사라지게)
            ObjectPoolManager.Instance.ReturnBulletPool(bullet.teamID, bullet.bulletIndex, bullet.gameObject);
        }
    }
}
