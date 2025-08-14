using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Weapon/Charger")]
public class WeaponData_Charger : WeaponData_HitScan
{
    [field: SerializeField] public EffectData chargeEffect { get; private set; }
}
