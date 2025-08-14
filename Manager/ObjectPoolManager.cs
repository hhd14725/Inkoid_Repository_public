using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

// 중요!
// 탄환, vfx 종류를 추가할 때 순서
// 1. 프리팹 생성
// 2. Resources/Bullet 혹은 Resources/Effect 알맞는 카테고리 폴더에 생성한 프리팹 넣기
// 3. 프리팹의 이름을 enums for ObjectPooling 내 해당 카테고리 enum에 추가
// 4. 탄환을 생성할 경우, BulletData 스크립터블 오브젝트를 이용해 생성한 탄환 데이터의 PrefabIndex_Bullet bulletIndex 설정해주기

#region enums for ObjectPooling
public enum Team
{
    TeamA,
    TeamB,
}

public enum PrefabIndex_Bullet
{
    Bullet_Gun,
    Bullet_Shotgun,
    Bullet_RPG,
}

public enum PrefabIndex_Effect
{
    MuzzleFX_Gun,
    MuzzleFX_Shotgun,
    MuzzleFX_Charge,
    MuzzleFX_RPG,
    TrailFX1,
    ImpactFX_Gun,
    ImpactFX_Shotgun,
    ImpactFX_Charge,
    ImpactFX_RPG,
}
#endregion

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;
    // Resources 폴더 내 각 프리팹을 정돈하기 위한 카테고리 폴더 경로
    readonly string
        bulletPath = "Bullet/",
        effectPath = "Effect/";

    // 총알, 이펙트 외 다른 오브젝트풀 카테고리가 필요하면 여기에 추가 + enum 추가
    Dictionary<PrefabIndex_Bullet, Queue<GameObject>>
        pools_Bullet_TeamA = new Dictionary<PrefabIndex_Bullet, Queue<GameObject>>(),
        pools_Bullet_TeamB = new Dictionary<PrefabIndex_Bullet, Queue<GameObject>>();

    Dictionary<PrefabIndex_Effect, Queue<GameObject>>
        pools_Effect_TeamA = new Dictionary<PrefabIndex_Effect, Queue<GameObject>>(),
        pools_Effect_TeamB = new Dictionary<PrefabIndex_Effect, Queue<GameObject>>();

    #region Initialize
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        // 풀 초기화
        InitPools();
    }

    void InitPools()
    {
        // 탄환, 이펙트 종류 수
        int bulletTypeNum = Enum.GetValues(typeof(PrefabIndex_Bullet)).Length,
            effectTypeNum = Enum.GetValues(typeof(PrefabIndex_Effect)).Length;

        // 탄환 종류 수에 맞춰 각 팀의 탄환을 담을 풀 초기화
        for (int i = 0; i < bulletTypeNum; i++)
        {
            PrefabIndex_Bullet indexTmp = (PrefabIndex_Bullet)i;
            pools_Bullet_TeamA.Add(indexTmp, new Queue<GameObject>());
            pools_Bullet_TeamB.Add(indexTmp, new Queue<GameObject>());
        }

        // 이펙트 종류 수에 맞춰 각 팀의 이펙트를 담을 풀 초기화
        for (int i = 0; i < effectTypeNum; i++)
        {
            PrefabIndex_Effect indexTmp = (PrefabIndex_Effect)i;
            pools_Effect_TeamA.Add(indexTmp, new Queue<GameObject>());
            pools_Effect_TeamB.Add(indexTmp, new Queue<GameObject>());
        }
    }
    #endregion

    #region Methods_BulletPool
    public GameObject GetBullet(Team teamID, PrefabIndex_Bullet bulletIndex, Vector3 position, Quaternion rotation, int ownerActorNumber)
    {
        GameObject obj = null;
        Dictionary<PrefabIndex_Bullet, Queue<GameObject>> dicitionaryTmp = null;

        // 어떤 팀의 풀을 쓸지
        switch (teamID)
        {
            case Team.TeamA:
                dicitionaryTmp = pools_Bullet_TeamA;
                break;
            case Team.TeamB:
                dicitionaryTmp = pools_Bullet_TeamB;
                break;
        }

        // bulletIndex에 해당하는 풀
        Queue<GameObject> queueTmp = dicitionaryTmp[bulletIndex];
        if (queueTmp.Count > 0)
        {
            // 로컬(자기 클라이언트)에서만
            // 큐에서 하나 뾱 빼주기
            obj = queueTmp.Dequeue();
            // 초기 위치, 회전 부여
            obj.transform.SetLocalPositionAndRotation(position, rotation);
            // 활성화
            obj.SetActive(true);

            if (obj.TryGetComponent(out Bullet_Base pooledBullet))
                pooledBullet.ownerActorNumber = ownerActorNumber;
        }
        // 해당 풀에 남은 게 없다면, 새로 만들어서 풀에 돌아가는 메서드를 넣어주기
        else
        {
            // 여기서 호출하려는 프리팹의 카테고리에 따라 Resources 폴더 내 경로 설정
            string path = bulletPath + bulletIndex.ToString();
            // 경로에 알맞는 프리팹
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                //Debug.LogError("Resources/" + path + " 경로에 해당 탄환 프리팹이 없습니다.");
                return null;
            }
            obj = Instantiate(prefab, position, rotation);

            if(obj.TryGetComponent(out Bullet_Base bullet))
            {
                // 해당 탄환의 종류, 팀 값 초기화
                bullet.InitBullet(bulletIndex, teamID, ownerActorNumber);
            }
            else
            {
                //Debug.LogError("탄환 팀 설정 초기화 실패");
            }
            // 탄환에 부착된 이펙트를 팀 색상으로 변경
            ChangeEffectColor(obj.transform, teamID);
        }

        return obj;
    }

    // 사용이 끝난 탄환을 오브젝트 풀로 복귀
    public void ReturnBulletPool(Team teamID, PrefabIndex_Bullet bulletIndex, GameObject usedBulletObj)
    {
        // 사용이 끝난 탄 비활성화
        usedBulletObj.SetActive(false);

        Dictionary<PrefabIndex_Bullet, Queue<GameObject>> dicitionaryTmp = null;
        // 어떤 팀의 풀을 쓸지
        switch (teamID)
        {
            case Team.TeamA:
                dicitionaryTmp = pools_Bullet_TeamA;
                break;
            case Team.TeamB:
                dicitionaryTmp = pools_Bullet_TeamB;
                break;
        }
        // bulletIndex에 해당하는 풀
        Queue<GameObject> queueTmp = dicitionaryTmp[bulletIndex];
        // 오브젝트 풀에 넣어주기
        queueTmp.Enqueue(usedBulletObj);
    }
    #endregion

    #region Methods_EffectPool
    public GameObject GetEffect(Team teamID, PrefabIndex_Effect effectIndex, float lifeTime,  Vector3 position, Quaternion rotation, float scale = 1)
    {
        GameObject obj = null;
        Dictionary<PrefabIndex_Effect, Queue<GameObject>> dicitionaryTmp = null;

        // 어떤 팀의 풀을 쓸지
        switch (teamID)
        {
            case Team.TeamA:
                dicitionaryTmp = pools_Effect_TeamA;
                break;
            case Team.TeamB:
                dicitionaryTmp = pools_Effect_TeamB;
                break;
        }

        // bulletIndex에 해당하는 풀
        Queue<GameObject> queueTmp = dicitionaryTmp[effectIndex];
        if (queueTmp.Count > 0)
        {
            // 로컬(자기 클라이언트)에서만
            // 큐에서 하나 뾱 빼주기
            obj = queueTmp.Dequeue();
            // 이펙트 스케일에 따른 크기 조정
            obj.transform.localScale = Vector3.one * scale;
            // 초기 위치, 회전 부여
            obj.transform.SetLocalPositionAndRotation(position, rotation);
            // 활성화
            obj.SetActive(true);
        }
        // 해당 풀에 남은 게 없다면, 새로 만들어서 풀에 돌아가는 메서드를 넣어주기
        else
        {
            // 여기서 호출하려는 프리팹의 카테고리에 따라 Resources 폴더 내 경로 설정
            string path = effectPath + effectIndex.ToString();
            // 경로에 알맞는 프리팹
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                //Debug.LogError("Resources/"+ path + " 경로에 해당 이펙트 프리팹이 없습니다.");
                return null;
            }
            obj = Instantiate(prefab, position, rotation);
            // 이펙트 스케일에 따른 크기 조정
            obj.transform.localScale = Vector3.one * scale;
            ChangeEffectColor(obj.transform, teamID);
        }
        // 라이프타임이 끝나면 오브젝트 풀로 복귀하도록 예약
        StartCoroutine(ReturnEffectPool(teamID, effectIndex, lifeTime, obj));

        return obj;
    }

    // 사용이 끝난 이펙트를 오브젝트 풀로 복귀
    IEnumerator ReturnEffectPool(Team teamID, PrefabIndex_Effect effectIndex, float lifeTime, GameObject usedEffectObj)
    {
        yield return new WaitForSeconds(lifeTime);

        // 사용이 끝난 이펙트 비활성화
        usedEffectObj.SetActive(false);

        Dictionary<PrefabIndex_Effect, Queue<GameObject>> dicitionaryTmp = null;
        // 어떤 팀의 풀을 쓸지
        switch (teamID)
        {
            case Team.TeamA:
                dicitionaryTmp = pools_Effect_TeamA;
                break;
            case Team.TeamB:
                dicitionaryTmp = pools_Effect_TeamB;
                break;
        }
        // effectIndex 에 해당하는 풀
        Queue<GameObject> queueTmp = dicitionaryTmp[effectIndex];
        // 오브젝트 풀에 넣어주기
        queueTmp.Enqueue(usedEffectObj);
    }
    #endregion

    // 이펙트를 팀 색상으로 바꿀 때 사용 (이펙트 초기화 때 각각 로컬에서 호출)
    public void ChangeEffectColor(Transform target, Team teamID)
    {
        Color teamColor = Color.white,
              teamColorDark = Color.white;

        // 어떤 팀 소속/적팀을 판정하기 위한 레이어 부여
        // > 적팀에만 맞도록 프로젝트 설정
        switch (teamID)
        {
            case Team.TeamA:
                teamColor = DataManager.Instance.colorTeamA;
                teamColorDark = DataManager.Instance.colorTeamA_Darker;
                break;
            case Team.TeamB:
                teamColor = DataManager.Instance.colorTeamB;
                teamColorDark = DataManager.Instance.colorTeamB_Darker;
                break;
            default:
                break;
        }

        // 이펙트 색상 부여
        ParticleSystem[] particleSystems = target.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            // 파티클 시스템의 StartColor가 흰색(기본값)이면, 머티리얼의 색상으로 제어
            if (particleSystems[i].main.startColor.color.Equals(Color.white))
            {
                // 머티리얼의 셰이더 프로퍼티 Emission 색상 변경
                particleSystems[i].GetComponent<ParticleSystemRenderer>().material.SetColor("_EmissionColor", teamColor);
            }
            // 흰색이 아니라면, 파티클 시스템의 StartColor로 제어
            else
            {
                float alpha = particleSystems[i].main.startColor.color.a;
                // ParticleSystem.MainModule이 구조체로 값 타입이나
                // 지역변수로 만들어서 변경값을 할당하면 내부적으로 적용되게끔 구조가 만들어져 있음
                ParticleSystem.MainModule tmpMain = particleSystems[i].main;
                tmpMain.startColor = new Color(teamColorDark.r, teamColorDark.g, teamColorDark.b, alpha);
            }
        }
        // 빛 색상도 팀 색상으로 변경
        Light[] light = target.GetComponentsInChildren<Light>();
        for (int i = 0; i < light.Length; i++)
        {
            light[i].color = teamColor;
        }
    }
}



// 레이즈 이벤트 사용 예시 (이후에 필요하면 쓸 것)
//// 포톤뷰를 안쓰는 이상 타겟 지정이 불가능하기에 레이즈 이벤트 사용 불가
//// 레이즈 이벤트의 전송 옵션들
//// 모든 클라이언트에 전송
//RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All, };
//// 이벤트의 신뢰도 증가, 성능 소모 증가(그래도 RPC보단 가볍다 함). 중요한 이벤트면 Reliability = true
//SendOptions sendOptions = new SendOptions { Reliability = true };
//// 모든 클라이언트에서 매개변수 게임 오브젝트 활성화 > 이벤트 수신기가 있어야 함 
//void EnableSync(GameObject targetToEnable, Vector3 position, Quaternion rotation)
//{
//    // RaiseEvent에서는 유니티 API 타입들을 넘겨줄 수 없음
//    // 따라서 통신에 각 PhotonView가 지닌 ViewID 사용
//    // 마침, PhotonNetwork.Instantiate()로 생성하는 오브젝트 풀링이라 생성한 객체들의 프리펩에는 PhotonView 존재
//    int viewID = targetToEnable.GetComponent<PhotonView>().ViewID;

//    object[] content = new object[]
//    {
//    viewID,
//    position,
//    rotation
//    };
//    // eventContent : 활성화할 오브젝트의 ViewID만 포함
//    PhotonNetwork.RaiseEvent((byte)EventCode.enable, content, options, sendOptions);
//}