using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//맵 선택, 씬이름에 동일하게 설정할것
public enum Map
{
    Stadium,    // 제일 처음 만든맵.
    DayCity,
    NightCity,
    PipeWorld,
    TutorialScene, // 튜토리얼 맵

}
[Serializable]
public class MapPreview
{
    public Map map;      // 위 Map enum 값
    public Sprite preview;  // Map 이미지입니다. 에디터에서 드래그 앤 드롭
    public TeamColorSort[] banColors; // 해당 맵에서 사용하지 않는 색상들(건물 색상과 잉크 색상이 비슷하여 구분이 힘든 경우 추가)
}



// 아이템 추가 시,
// 1. ItemPrefabs에 무기 프리팹 넣기
// 2. 프리팹 배열 순서에 맞게 PrefabIndex_Item enum에 항목 추가
//public enum PrefabIndex_Item
//{

//}

// 아이템 레어도, 해당 아이템 프리팹을 담고있을 구조체, 아이템 프리팹, 이넘, 레어도 배열끼리 매핑해서
// 병렬로 처리하는것보다 구조체에 담아서 처리하는게 매핑을 줄일수있다고 판단. 추후 여기서 쿨타임같은것들도 관리가능.
[System.Serializable]
public struct SpawnableItem
{
    [Tooltip("스폰할 아이템 Prefab (Resources/Items/ 에 위치)")]
    public GameObject ItemPrefab;

    [Tooltip("가중치 계산용 카테고리")]
    public ItemCategory ItemCategory;

    [Tooltip("픽업 시 적용할 이펙트 타입")]
    public ItemEffectType EffectType;

    [Tooltip("이펙트 지속 시간(초)")]
    public float EffectDuration;

    // EnemySmall 전용 파라미터
    [Tooltip("EnemySmall: 체력 감소량")]
    public float DebuffHealthMultiplier;
    [Tooltip("EnemySmall: 축소 배율")]
    public float DebuffScaleMultiplier;

    // TitanizeSelf 전용 파라미터
    [Tooltip("TitanizeSelf: 배율 (1 = 원래 크기)")]
    public float TitanScaleMultiplier;
    [Tooltip("TitanizeSelf: 최대 체력 배율")]
    public float TitanHealthMultiplier;

    [Tooltip("대형화/소형화 시 총알 크기 배율")]
    public float BulletScaleMultiplier;
}


// 아이템 레어도 분류, 가중치를 위한 enum값
public enum ItemCategory
{
    Common,
    Uncommon,
    Rare,
    SuperRare
}
//아이템 효과타입
public enum ItemEffectType
{
    NoInkConsumption,
    Invincibility,
    TitanizeSelf,
    EnemySmall
}


// 플레이어도 프리팹 여러개 생기면 무기 생성 방식 참조
// 무기 추가 시,
// 기존
// 1. 프리팹: Resources/Weapon 폴더에 넣기
// 2. 프리팹 이름 복사
// 3. 복사한 이름을 PrefabIndex_Weapon 항목으로 넣기 (무기 고를 때 순서와 동일하게끔 하기)

// 변경
// 1. WeaponPrefabs에 무기 프리팹 넣기
// 2. 프리팹 배열 순서에 맞게 PrefabIndex_Weapon enum에 항목 추가
public enum PrefabIndex_Weapon
{
    Shooter, // Shooter == Gun : 둘을 혼용해서 쓰고 있기에 이 둘의 명칭이 같다 보시면 됩니다.
    Shotgun,
    Charger,
    RPG,
}

// 팀 색상 종류(인스펙터에서 각 맵의 금지 색상을 할당할 때 작업 편의를 위해 추가)
public enum TeamColorSort
{
    Red,
    Orange,
    Yellow,
    Green,
    Skyblue,
    Blue,
    Violet,
    Pink,
}

public enum FaceSort
{
    Normal,
    Smile,
    Sad,
}

public class DataManager: MonoBehaviour
{
    #region SingleTon
    public static DataManager Instance;
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

        //맵 프리뷰 딕셔너리에 enum과 이미지 키값으로 초기화 할당
        MapPreviews = mapPreviewsList.ToDictionary(x => x.map, x => x.preview);
    }
    #endregion

    #region Data

    // 맵 프리뷰용 이미지
    [Header("맵 프리뷰")]
    public List<MapPreview> mapPreviewsList; // 에디터에서 할당

    public Dictionary<Map, Sprite> MapPreviews { get; private set; }




    // 아이템 프리펩
    //[field: SerializeField] public GameObject[] ItemPrefabs { get; private set; }

    [Header("스폰 가능한 아이템 설정")]
    [Tooltip("프리팹, 카테고리, 이펙트, 지속시간을 SpawnableItem에 등록")]
    [SerializeField] private SpawnableItem[] spawnableItems;
    public int SpawnableItemCount => spawnableItems.Length;
    //인덱스에 해당하는 SpawnableItem을 반환하는 메서드입니다.
    public SpawnableItem GetSpawnableItemAtIndex(int index)
    {
        if (index < 0 || index >= spawnableItems.Length)
        {
            //Debug.LogError($"GetSpawnableItemAtIndex: 잘못된 인덱스 {index}");
            return default;
        }

        return spawnableItems[index];
    }
    

    // 무기 프리팹
    [field: SerializeField, Header("무기")] public GameObject[] WeaponPrefabs { get; private set; }
    // 무기 이미지
    [field: SerializeField] public Sprite[] WeaponImages { get; private set; }
    // 무기 이미지 for GameScene
    [field: SerializeField] public Sprite[] WeaponImages_GameScene { get; private set; }

    // 팀 색상들의 데이터 (인스펙터에서 할당 가능) > 이후에서 이 색상 중에서 선택 가능하게끔 하면 됩니다
    [field:SerializeField, Header("색상")] public Color[] TeamColorsData { get; private set; }
    [field: SerializeField] public Color[] TeamColorsData_Darker { get; private set; }

    // 게임 시작할 때 팀 A, B에서 색상 인덱스를 받아둠
    public Color colorTeamA { get; private set; }
    public Color colorTeamB { get; private set; }
    public Color colorTeamA_Darker { get; private set; }
    public Color colorTeamB_Darker { get; private set; }

    public void InitTeamColors(byte _colorTeamA, byte _colorTeamB)
    {
        colorTeamA = TeamColorsData[_colorTeamA];
        colorTeamB = TeamColorsData[_colorTeamB];
        colorTeamA_Darker = TeamColorsData_Darker[_colorTeamA];
        colorTeamB_Darker = TeamColorsData_Darker[_colorTeamB];
    }
     
    [field: SerializeField, Header("잉코이드 표정")] public Sprite[] inkoidFaces { get; private set; }
    #endregion
}
