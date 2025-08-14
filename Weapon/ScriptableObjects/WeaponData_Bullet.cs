using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Weapon/Bullet")]
public class WeaponData_Bullet : ScriptableObject
{
    // 공격 딜레이
    [field:SerializeField] public float atkDelay { get; private set; }
    // 발사에 필요한 잉크 소모량
    [field: SerializeField] public float inkCost { get; private set; }
    // 총알 종류
    [field: SerializeField] public PrefabIndex_Bullet bullet { get; private set; }
}


