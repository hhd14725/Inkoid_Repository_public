using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Weapon/HitScan")]
public class WeaponData_HitScan : ScriptableObject
{
    // 공격 딜레이
    [field: SerializeField] public float atkDelay { get; private set; }
    // 발사에 필요한 잉크 소모량
    [field: SerializeField] public float inkCost { get; private set; }
    // 사거리
    [field: SerializeField] public float range { get; set; }
    // 대미지
    [field: SerializeField] public float damage { get; set; }
    // 공격 반경 (Raycast 한 줄을 쏘는 타입에는 사용하지 않음)
    [field: SerializeField] public float radius { get; set; }
    // 물감 데이터
    [field: SerializeField] public StampData stampData { get; set; }

    // 발사 이펙트
    [field: SerializeField, Header("Effects")] public EffectData muzzleFlash { get; private set; }
    // 착탄 이펙트 (총알이 무언가와 충돌했을 때 호출)
    [field: SerializeField] public EffectData hitEffect { get; private set; }
}
