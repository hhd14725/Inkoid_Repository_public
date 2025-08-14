using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnManager : MonoBehaviourPun
{
    public static ItemSpawnManager Instance { get; private set; }

    [Header("카테고리별 가중치 (Common → SuperRare 순서)")]
    [Tooltip("Common, Uncommon, Rare, SuperRare 순서로 가중치를 입력합니다.")]
    [SerializeField] private float[] categoryWeights = new float[4] { 4f, 3f, 2f, 1f };

    private List<ItemSpawnPoint> spawnPoints = new List<ItemSpawnPoint>();
    private float totalCategoryWeight;

    // 뽑힌 아이템 인스턴스별로 SpawnableItem 정보를 저장해두는 사전
    private Dictionary<int, SpawnableItem> spawnableItemLookup = new Dictionary<int, SpawnableItem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 씬의 스폰 포인트 캐시
        ItemSpawnPoint[] points = FindObjectsOfType<ItemSpawnPoint>();
        spawnPoints.AddRange(points);

        // 가중치 총합 계산
        totalCategoryWeight = 0f;
        foreach (float weight in categoryWeights)
            totalCategoryWeight += weight;

        // 초기 스폰
        for (int i = 0; i < spawnPoints.Count; i++)
            TrySpawnAt(spawnPoints[i], i);
    }

    private void TrySpawnAt(ItemSpawnPoint spawnPoint, int spawnPointIndex)
    {
        if (!spawnPoint.IsAvailable) return;

        // 1) 카테고리 확률에 따른 아이템 인덱스 선택
        int itemIndex = PickRandomItemIndex();
        SpawnableItem spawnableItem = DataManager.Instance.GetSpawnableItemAtIndex(itemIndex);

        // 2) 네트워크 스폰
        GameObject instance = PhotonNetwork.Instantiate(
            $"Item/{spawnableItem.ItemPrefab.name}",
            spawnPoint.transform.position,
            Quaternion.identity
        );

        // 3) 생성된 아이템의 ViewID로 효과 정보 저장
        PhotonView spawnedView = instance.GetComponent<PhotonView>();
        spawnableItemLookup[spawnedView.ViewID] = spawnableItem;

        // 4) Respawner 초기화
        ItemRespawner respawner = instance.AddComponent<ItemRespawner>();
        respawner.Initialize(spawnPointIndex, spawnableItem.ItemCategory);

        // 해당 포인트 비활성화
        spawnPoint.IsAvailable = false;
    }

    private int PickRandomItemIndex()
    {
        float randomValue = Random.value * totalCategoryWeight;
        float cumulativeWeight = 0f;
        ItemCategory chosenCategory = ItemCategory.Common;

        // 카테고리 선택
        for (int i = 0; i < categoryWeights.Length; i++)
        {
            cumulativeWeight += categoryWeights[i];
            if (randomValue <= cumulativeWeight)
            {
                chosenCategory = (ItemCategory)i;
                break;
            }
        }

        // 해당 카테고리 아이템 인덱스 수집
        List<int> matchingIndices = new List<int>();
        int totalCount = DataManager.Instance.SpawnableItemCount;
        for (int i = 0; i < totalCount; i++)
        {
            if (DataManager.Instance.GetSpawnableItemAtIndex(i).ItemCategory == chosenCategory)
                matchingIndices.Add(i);
        }

        // 수집된 것 중 균등 랜덤, 비어있으면 전체에서 랜덤
        if (matchingIndices.Count > 0)
            return matchingIndices[Random.Range(0, matchingIndices.Count)];
        return Random.Range(0, totalCount);
    }

    // MasterClient 에서만 호출
    // 아이템 파괴, 리스폰 예약, 클라이언트 전체 효과 RPC 순으로 처리.
    [PunRPC]
    public void HandlePickupOnMaster(
        int spawnPointIndex,
        int itemViewId,
        int playerViewId,
        int _ ) // 사용하지는 않는중, 구버전.
    {
        // 1) 아이템 오브젝트 파괴
        PhotonView itemView = PhotonView.Find(itemViewId);
        if (itemView != null)
            PhotonNetwork.Destroy(itemView);

        // 2) 쿨다운 후 재스폰
        StartCoroutine(RespawnCoroutine(spawnPointIndex));

        // 3) 실제 뽑혔던 SpawnableItem 정보 lookup
        if (!spawnableItemLookup.TryGetValue(itemViewId, out SpawnableItem spawnableItem))
        {
            //Debug.LogWarning($"SpawnableItem 정보 미발견(viewID={itemViewId})");
            return;
        }

        // 4) 모든 클라이언트에 이펙트 타입·지속시간 전달
        photonView.RPC(
            nameof(ApplyEffectOnClients),
            RpcTarget.All,
            playerViewId,
            (int)spawnableItem.EffectType,
            spawnableItem.EffectDuration,
            spawnableItem.TitanScaleMultiplier,
            spawnableItem.TitanHealthMultiplier,
            spawnableItem.DebuffScaleMultiplier,
            spawnableItem.DebuffHealthMultiplier,
            spawnableItem.BulletScaleMultiplier
        );

        // 5) 사용한 키 삭제 (메모리 누수 방지)
        spawnableItemLookup.Remove(itemViewId);
    }

    private IEnumerator RespawnCoroutine(int spawnPointIndex)
    {
        yield return new WaitForSeconds(spawnPoints[spawnPointIndex].RespawnCooldown);
        spawnPoints[spawnPointIndex].IsAvailable = true;
        TrySpawnAt(spawnPoints[spawnPointIndex], spawnPointIndex);
    }

    // 플레이어 프리팹에 붙은 PlayerEffectHandler 로 효과를 전달.
    [PunRPC]
    private void ApplyEffectOnClients(int playerViewId, int effectTypeValue, 
        float effectDuration, 
        float titanScale,
        float titanHealthMultiplier,
        float smallScale,
        float smallHealth,
        float bulletScale)
    {
        //Debug.Log($"[Client] ApplyEffectOnClients 호출: playerView={playerViewId}, type={effectTypeValue}, duration={effectDuration}");
        var pickerPv = PhotonView.Find(playerViewId);
        if (pickerPv == null || !pickerPv.gameObject.activeInHierarchy) return;
        byte pickerTeam = pickerPv.GetComponent<PlayerHandler>().Info.TeamId;

        var effectHandler = pickerPv.GetComponent<PlayerEffectHandler>();
        //25.07.19 김건휘 이펙트 불러오기 추가
        effectHandler.AttachPickupEffect(effectDuration);


        var type = (ItemEffectType)effectTypeValue;
        switch (type)
        {
            case ItemEffectType.TitanizeSelf:
                // 획득자만 대형화
                pickerPv.GetComponent<PlayerEffectHandler>()
                        .ApplyEffect(type, effectDuration, titanScale, titanHealthMultiplier, bulletScale);

                pickerPv.GetComponent<PlayerEffectHandler>()
                        .PlayTitanEffectVFX(effectDuration);
                break;

            case ItemEffectType.EnemySmall:
                // 적팀 전체에 소형화 적용
                foreach (var enemyHandler in FindObjectsOfType<PlayerEffectHandler>())
                {
                    if (enemyHandler.GetComponent<PlayerHandler>().Info.TeamId != pickerTeam)
                    {
                        enemyHandler.ApplyEffect(type, effectDuration, smallScale, smallHealth, bulletScale);

                        enemyHandler.PlayShrinkEffectVFX();
                    }
                }
                break;

            default:
                // 그 외
                pickerPv.GetComponent<PlayerEffectHandler>()
                        .ApplyEffect(type, effectDuration);
                break;
        }
    }

}
