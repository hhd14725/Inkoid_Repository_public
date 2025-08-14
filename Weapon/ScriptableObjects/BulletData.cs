using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Bullet_", menuName = "Bullet/Shooter")]
public class BulletData : ScriptableObject
{
    // 총알 속도
    [field: SerializeField] public float speed { get; private set; } = 10f;
    // 총알에 작용하는 중력가속도(마이너스 값을 주면 아래로 떨어져요)
    [field: SerializeField] public float gravityScale { get; private set; } = 0f;
    // 총알 생명주기
    [field: SerializeField] public float lifeTime { get; private set; } = 1.5f;
    // 대미지
    [field: SerializeField] public float damage { get; private set; } = 10f;
    // 콜라이더 크기 최대
    [field: SerializeField] public float colliderSizeMax { get; private set; } = 1.5f;
    // 스탬프(물감 찍기) 데이터
    [field: SerializeField, Header("StampData")] public StampData stampData { get; private set; }
    // 발사 이펙트
    [field: SerializeField, Header("Effects")] public EffectData muzzleFlash  { get; private set; }
    // 착탄 이펙트 (총알이 무언가와 충돌했을 때 호출)
    [field: SerializeField] public EffectData hitEffect { get; private set; }
}

[Serializable]
public class StampData
{
    // 나중에 다른 데이터를 추가할 수 있기에 StampData 유지
    // 크기
    [field: SerializeField] public float radius { get; private set; }
}

[Serializable]
public class EffectData
{
    // 이펙트 종류
    [field: SerializeField] public PrefabIndex_Effect effect { get; private set; }
    // 생존 주기
    [field: SerializeField] public float lifeTime { get; private set; }
    // sfx 오디오 클립
    [field: SerializeField] public ClipIndex_SFX sfxClip { get; private set; }
    // sfx 볼륨 스케일
    [field: SerializeField, Range(0f, 30f)] public float sfxVolumeScale { get; private set; }
}