using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
public class RPCManager : MonoBehaviourPun
{
    // 포톤뷰
    // RPCManager
    public PhotonView photonView_RPCManager { get; private set; }
    // 해당 클라이언트의 플레이어
    public PhotonView photonView_Player { get; private set; }
    public void SetPlayerPhotonView(PhotonView photonView_Player)
    {
        this.photonView_Player = photonView_Player;
    }

    #region SingleTon
    public static RPCManager Instance;
    // 중복으로 접근하지 못하게 락을 거는 용도
    static readonly object _lock = new object();
    void Awake()
    {
        if (Instance == null)
        {
            lock (_lock)
            {

                Instance = this;
            }
        }
        else
            Destroy(gameObject);
    }
    #endregion

    private void Start()
    {
        photonView_RPCManager = GetComponent<PhotonView>();
    }

    // 브로드캐스트가 가능한 매서드들 앞에는 Attribute : PunRPC 필요

    #region ObjectPooling
    // 총알 발사!
    [PunRPC]
    public void SpawnBullet(byte teamID, PrefabIndex_Bullet bulletIndex, Vector3 position, Quaternion rotation, float scaleMultiplier, int ownerActorNumber)
    {
        // 오브젝트 풀에서 해당 총알 하나를 꺼내서 위치, 회전을 부여하여 활성화
        var bullet = ObjectPoolManager.Instance.GetBullet((Team)teamID, bulletIndex, position, rotation, ownerActorNumber);
        //대형화, 소형화 아이템 전용
        if (bullet != null)
        {
            bullet.GetComponent<Bullet_Base>().ApplyItemScale(scaleMultiplier); // 아이템용 스케일 곱해주기
        }
    }
    // 착탄 이펙트 출력
    // teamID는 byte라서 enum을 그대로 쓰진 못했지만 effectIndex, ClipIndex_SFX는 int형이라 그대로 전송 가능
    // 통신 패킷을 여러번 만들어서 전송하는 건 효율이 좋지 않으므로 VFX, SFX를 함께 호출하도록 전송하는 것으로 변경
    // 둘 중 하나 혹은 따로 전송할 필요가 있다면 VFX, SFX Only 메서드로 필요한 것만 호출
    [PunRPC]
    public void SpawnEffect(byte teamID, PrefabIndex_Effect effectIndex, float lifeTime, ClipIndex_SFX clipIndex, float sfxVolumeScale, Vector3 position, Quaternion rotation, float scale = 1)
    {
        // VFX
        ObjectPoolManager.Instance.GetEffect((Team)teamID, effectIndex, lifeTime, position, rotation, scale);
        // SFX
        SpawnEffect_SFXOnly(clipIndex, sfxVolumeScale, position);
    }
    // 시각적 효과만 부르고 싶을 때
    [PunRPC]
    public void SpawnEffect_VFXOnly(byte teamID, PrefabIndex_Effect effectIndex, float lifeTime, Vector3 position, Quaternion rotation, float scale = 1)
    {
        // VFX
        ObjectPoolManager.Instance.GetEffect((Team)teamID, effectIndex, lifeTime, position, rotation, scale);
    }
    // 효과음만 재생하고 싶을 때
    [PunRPC]
    public void SpawnEffect_SFXOnly(ClipIndex_SFX clipIndex, float sfxVolumeScale, Vector3 position)
    {
        // SFX + 진원지로부터 플레이어 위치가 멀리 떨어질수록 소리 크기 감쇄
        // 해당 클라이언트의 플레이어 위치
        Vector3 playerPos = photonView_Player.transform.position;
        // 소리의 진원지 > 플레이어까지의 거리 (SqrMagnitude: 제곱값) > 너무 가까우면 소리가 너무 커진다 하니
        float distance = Mathf.Max(Vector3.Distance(playerPos, position), 1);
        // 멀어질수록 점점 작아지도록
        SoundManager.Instance.PlaySfx(clipIndex, sfxVolumeScale/distance);
    }
    [PunRPC]
    public void SpawnEffect_SFX3D(ClipIndex_SFX clipIndex, float sfxVolumeScale, Vector3 position)
    {
        SoundManager.Instance.PlaySfx3D(clipIndex, position, sfxVolumeScale);
    }
    #endregion

    #region Bullet
    //[PunRPC]
    //public void TakeDamage(float dmg)
    //{
    //    // 대미지는 모두에게 전달할 필요는 없고 맞은 대상에게만
    //    // > 맞은 대상은 피격 모션 재생/사망 이 정도만 모두에게 알리면 될 듯
    //    // 여기에 체력 감소 코드 삽입
    //    //Debug.Log($"{photonView.Owner.NickName} takes {dmg} damage.");
    //}

    // 이건 플레이어로 옮겨서 총알이 맞췄을 때 가져와서 호출
    //[PunRPC]
    //public void ShowHitMessage(string attackerName)
    //{
    //    //Debug.Log($"피격됨! {attackerName}에게 맞았습니다.");
    //}

    // 착탄 지점에 페인트 색칠하기
    [PunRPC]
    public void CallPaint(int paintableViewID, Vector3 pos, byte teamIndex, float radius)
    {
        // ViewID → PaintableObject 찾기
        PhotonView pv = PhotonView.Find(paintableViewID);
        if (pv != null && pv.TryGetComponent(out PaintableObject paintable))
        {
            // 실제 페인트 호출
            PaintManager.Instance.paint(paintable, pos, teamIndex, radius);
        }
    }

    //// 모든 클라이언트에서 킬/뎃 집계 + 킬로그 띄우기 + UI 회색 처리(죽은 쪽)까지 한 번에.
    ///
    [PunRPC]
    public void RPC_RegisterKill(int killerID, int victimID)
    {
        // 1) 통계 집계
        StatsManager.Instance.RegisterKill(killerID, victimID);

        // 2) 킬로그 띄우기
        UIManager.Instance.GameHUDUI.AddKillFeedEntry(killerID, victimID);

        // 3) UI 회색 처리(죽은 쪽)
        UIManager.Instance.GameHUDUI.SetPlayerAlive(victimID, false);
    }

    // 모든 클라이언트에서 리스폰 시 UI 회색 해제
    [PunRPC]
    public void RPC_SetAlive(int playerID, bool alive)
    {
        UIManager.Instance.GameHUDUI.SetPlayerAlive(playerID, alive);
    }
    //리스폰시 플레이어상단 statusUI에 무기 이름 갱신
    [PunRPC]
    public void RPC_UpdatePlayerWeapon(int playerID, int weaponIndex)
    {
        UIManager.Instance.GameHUDUI.UpdatePlayerWeapon(playerID, weaponIndex);
    }

    #endregion

    #region Unity API Related
    // 컴포넌트, 오브젝트 활성화/비활성화 동기화
    // 컴포넌트 enabled 변경을 동기화하려면 ChangeComponent_enabled 메서드 호출하여 매개변수만 넣어주면 돼요
    Dictionary<int, object> components = new Dictionary<int, object>();
    // 사용이 끝난 키를 반환
    Queue<int> availableKey = new Queue<int>();
    // 다음 인덱스
    int nextIndex = 0;

    public int RegisterComponent(object _component)
    {
        int key;
        // 사용이 끝난 키가 있다면
        if (availableKey.Count > 0)
        {
            // 해당 키를 꺼내서 재활용
            key = availableKey.Dequeue(); 
        }
        // 큐에 남은 키가 없다면
        else
        {
            // 다음 인덱스 새 키
            key = nextIndex++; 
        }

        // 딕셔너리에 새 항목 등록
        components[key] = _component;
        // 해당 항목에 접근하기 위한 키를 반환
        return key;
    }

    // 사용이 끝난 컴포넌트의 등록 해제 시 호출
    public void UnregisterComponent(int key)
    {
        // 해당 키를 가진 짝을 딕셔너리에서 제거하는 데 성공하였다면
        if (components.Remove(key))
        {
            // 사용이 끝난 키에 해당 키 값을 넣어줌(재사용 준비)
            availableKey.Enqueue(key);
        }
    }

    public void SetActive_AllClients(int key, bool isEnabled)
    {
        photonView.RPC(nameof(SetActive_RPC), RpcTarget.AllBufferedViaServer, key, isEnabled);
    }
    [PunRPC]
    void SetActive_RPC(int key, bool isEnabled)
    {
        object component = components[key];

        //if (component == null)
            //Debug.LogError("component가 null");

        if (component is Behaviour b)
        {
            b.enabled = isEnabled;
        }
        else if (component is Renderer r)
        {
            r.enabled = isEnabled;
        }
        else if (component is Collider c)
        {
            c.enabled = isEnabled;
        }
        else if (component is GameObject gameObj)
        {
            gameObj.SetActive(isEnabled);
        }
        else
        {
            //Debug.LogError("지원하지 않는 타입입니다: " + component.GetType());
        }
    }

    public void SetLaserPos(int key_WeaponCharger, Vector3 start, Vector3 end)
    {
        photonView.RPC(nameof(SetLaserPos_RPC), RpcTarget.OthersBuffered, key_WeaponCharger, start, end);
    }

    [PunRPC]
    void SetLaserPos_RPC(int key_WeaponCharger, Vector3 start, Vector3 end)
    {
        if (components.TryGetValue(key_WeaponCharger, out object component))
        {
            if (component is Weapon_Charger charger)
            {
                charger.ReceiveLaserPos(start, end);
            }
            //else
            //{
            //    //Debug.LogError(key_WeaponCharger + " 에 해당하는 값은 Weapon_Charger 타입이 아닙니다.");
            //}
        }
        //else
        //{
        //    //Debug.LogError(key_WeaponCharger + " 에 해당하는 딕셔너리 항목이 없습니다.");
        //}
    }

    // 라인랜더러 관련
    public void DrawLine(int key_lineRenderer, Vector3 start, Vector3 end)
    {
        photonView.RPC(nameof(DrawLine_RPC), RpcTarget.OthersBuffered, key_lineRenderer, start, end);
    }
    [PunRPC]
    void DrawLine_RPC(int key_lineRenderer, Vector3 start, Vector3 end)
    {
        if(components.TryGetValue(key_lineRenderer, out object component))
        {
            if(component is LineRenderer lineRenderer)
            {
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }
            //else
            //{
            //    //Debug.LogError(key_lineRenderer + " 에 해당하는 값은 LineRenderer 타입이 아닙니다.");
            //}
        }
        //else
        //{
        //    //Debug.LogError(key_lineRenderer + " 에 해당하는 딕셔너리 항목이 없습니다.");
        //}
    }

    //결과창 호스트가 띄우는 메서드
    [PunRPC]
    public void RPC_ShowEndPopupAll()
    {
        UIManager.Instance.GameHUDUI.ShowEndPopup();
    }

    #endregion
}
// 7/9 유빈
// [PunRPC] 를 이용하여 메서드를 래핑하려면 해당 메서드가 있는 스크립트를 포함(컴포넌트)한 오브젝트에 PhotonView 컴포넌트가 포함되어 있어야 합니다.
// 하지만, PhotonView가 늘어나면 통신 부하가 늘어나기에
// 한 클라이언트 내 플레이어 오브젝트 한 곳에서만 PhotonView 컴포넌트를 사용하며
// 통신 관련 래핑 메서드들을 한 곳에서 관리하려 합니다.