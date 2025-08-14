using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEffectHandler : MonoBehaviourPun
{
    public ItemEffectType? currentEffect { get; private set; } = null;

    public bool IsInvincible => ActiveEffects.Contains(ItemEffectType.Invincibility);
    public List<ItemEffectType> ActiveEffects { get; private set; } = new();

    [Header("버스트 이펙트 (최초 1~2초)")]
    [SerializeField] private ParticleSystem[] burstEffects;
    [SerializeField] private float burstRate = 200f;       // Burst 시 Emission rate
    [SerializeField] private float burstDuration = 1.5f;   // Burst 지속시간

    [Header("트레일 이펙트 (이후 계속)")]
    [SerializeField] private TrailRenderer[] boosterTrails;
    [SerializeField] private float maxTrailTime = 0.2f;    // 최고 속도일 때 트레일 길이
    [SerializeField] private float trailFadeSpeed = 5f;

    [Header("파티클 방출 세팅")]
    [SerializeField] private float emissionSmooth = 25f;   // 보간 속도

    // 카메라 원복용 초기 offset 저장
    private Vector3 _initialViewOffset;
    private Vector3 _initialCameraHeightOffset;

    private Rigidbody _rb;
    private InputAction _moveH, _moveV, _dash;
    private float _maxMoveSpeed;
    private bool _bursting = false;
    private Coroutine _burstCoroutine;
    private bool _wasMovingLastFrame = false;
    private ParticleSystem.EmissionModule[] _burstEmissions;


    private PlayerHandler _handler;
    private PlayerInkHandler _inkHandler;

    private bool _baseInitialized = false;
    private Vector3 _baseScale;
    private float _baseMaxHealth;

    private Coroutine _scaleEffectCoroutine; // 거대화 소형화 중첩적용 방지용 변수
    private Coroutine _invincibilityCoroutine;
    private Coroutine _infiniteInkCoroutine;

    private Action<float> _healthSuppressor;

    [SerializeField] private GameObject pickupEffectPrefab;// 25.07.19 김건휘 이펙트 추가
    [SerializeField] private GameObject DebuffEffectPrefab;// 25.07.21 김건휘 이펙트 추가
    [SerializeField] private GameObject TitanEffectPrefab; // 25.07.21 김건휘 이펙트 추가
    [SerializeField] private GameObject GodEffectPrefab; // 25.07.21 김건휘 이펙트 추가
    [SerializeField] private GameObject InfiniteInkPrefab; // 25.07.21 김건휘 이펙트 추가
    [SerializeField] private GameObject RespawnPrefab; // 25.07.24 김건휘 리스폰 이펙트 추가
    private void Awake()
    {
        _handler = GetComponent<PlayerHandler>();
        _inkHandler = GetComponent<PlayerInkHandler>();
        _rb = GetComponent<Rigidbody>();

        //카메라 컨트롤러의 원래 offset을 저장
        _initialViewOffset = _handler.playerCameraController.viewOffset;
        _initialCameraHeightOffset = _handler.playerCameraController.cameraHeightOffset;

        // PS EmissionModule 캐싱
        _burstEmissions = new ParticleSystem.EmissionModule[burstEffects.Length];
        for (int i = 0; i < burstEffects.Length; i++)
        {
            var ps = burstEffects[i];
            _burstEmissions[i] = ps.emission;
            _burstEmissions[i].rateOverTime = 0f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 입력 액션 캐싱
        var map = _handler.playerAction.PlayerActionMap;
        _moveH = map.HorizontalMove;
        _moveV = map.VerticalMove;
        _dash = map.Dash;

        // 이동 가능해지면 Enable 호출
    }


    private void OnEnable()
    {
        _handler.OnDeath += OnPlayerDeath;

        //_moveAction.Enable();
        //_dashAction.Enable();
        StartCoroutine(CacheMaxSpeedNextFrame());
    }

    private void OnDisable()
    {
        // 비활성화 시 반드시 해제
        _handler.OnDeath -= OnPlayerDeath;


        //_moveAction.Disable();
        //_dashAction.Disable();
    }
    private IEnumerator CacheMaxSpeedNextFrame()
    {
        yield return null;
        _maxMoveSpeed = _handler.Status.MaximumMoveSpeed;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // 1) 이동 입력 (수평 + 수직)
        Vector2 h = _moveH.ReadValue<Vector2>();
        Vector2 v = _moveV.ReadValue<Vector2>();
        bool moving = h.sqrMagnitude + v.sqrMagnitude > 0.01f;

        // 2) 최초 이동 또는 대시 시 Burst 시작
        if ((moving && !_wasMovingLastFrame) || _dash.triggered)
            StartBurst();

        _wasMovingLastFrame = moving;
        // 3) 트레일 지속시간 타겟 계산
        float speed = _rb.velocity.magnitude;
        float t = _maxMoveSpeed > 0f ? Mathf.Clamp01(speed / _maxMoveSpeed) : 0f;
        float targetTrailTime = moving ? maxTrailTime * t : 0f;

        // 4) Lerp하여 자연스럽게 줄어들게
        for (int i = 0; i < boosterTrails.Length; i++)
        {
            float curr = boosterTrails[i].time;
            boosterTrails[i].time = Mathf.Lerp(curr, targetTrailTime, Time.deltaTime * trailFadeSpeed);
        }
    }

    private void StartBurst()
    {
        if (_bursting) return;

        _bursting = true;
        if (_burstCoroutine != null) StopCoroutine(_burstCoroutine);
        _burstCoroutine = StartCoroutine(StopBurstAfterDelay());

        foreach (var ps in burstEffects)
        {
            var em = ps.emission;
            em.rateOverTime = burstRate;
            ps.Play();
        }
    }

    private IEnumerator StopBurstAfterDelay()
    {
        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        // 페이드 아웃
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * emissionSmooth;
            float rate = Mathf.Lerp(burstRate, 0f, t);
            for (int i = 0; i < _burstEmissions.Length; i++)
            {
                _burstEmissions[i].rateOverTime = rate;
            }
            yield return null;
        }
        foreach (var ps in burstEffects)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _bursting = false;
    }
    private void OnPlayerDeath(PlayerHandler who)
    {
        CancelAllEffects();
    }
    public void AttachPickupEffect(float duration = 2f)//25.07.19 김건휘 이펙트 추가
    {
        if (pickupEffectPrefab == null)
        {
            //Debug.LogWarning("[PlayerEffectHandler] pickupEffectPrefab 미할당");
            return;
        }

        // 위치 조절은 여기서 하심됨다
        Vector3 spawnPos = transform.position + Vector3.down * 0.7f;

        GameObject fx = Instantiate(pickupEffectPrefab, spawnPos, Quaternion.identity);
        fx.transform.SetParent(transform); // 플레이어 따라다니기
        fx.transform.localScale *= 2f; // 크기는 여기서 조정
        Destroy(fx, duration); // 시간지나면 지움

        if (photonView.IsMine) // 내가 먹으면 바로 들리고
        {
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_ItemPickUp);
        }
        else // 다른사람이 먹으면 멀리서 들리게 
        {
            SoundManager.Instance.PlaySfx3D(ClipIndex_SFX.mwfx_ItemPickUp, transform.position);
        }
    }

    public void ApplyEffect(ItemEffectType effectType, float duration, float scaleMultiplier = 1f, float healthMultiplier = 1f, float bulletScale = 1f)
    {
        if (!gameObject.activeInHierarchy || !enabled)
            return;

        if (!ActiveEffects.Contains(effectType))
            ActiveEffects.Add(effectType);

        currentEffect = effectType; // 효과 시작 시 기록

        //대형화, 소형화 시점 기본값 가져오기 초기화.
        if ((effectType == ItemEffectType.TitanizeSelf || effectType == ItemEffectType.EnemySmall)
           && !_baseInitialized)
        {
            _baseScale = transform.localScale;
            _baseMaxHealth = _handler.Status.MaximumHealth;
            _baseInitialized = true;
        }

        //효과중첩 방지
        if ((effectType == ItemEffectType.TitanizeSelf || effectType == ItemEffectType.EnemySmall)
               && _scaleEffectCoroutine != null)
        {
            StopCoroutine(_scaleEffectCoroutine);
            _scaleEffectCoroutine = null;
            //중첩들어온다면 즉시 스케일, 체력, 총알 크기 복원하고 다시 효과적용
            transform.localScale = _baseScale;
            _handler.Status.MaximumHealth = _baseMaxHealth;
            _handler.OnBulletSizeChanged?.Invoke(1f);
        }


        switch (effectType)
        {
            case ItemEffectType.NoInkConsumption:

                if (_infiniteInkCoroutine != null)
                {
                    // 1) 코루틴 정지
                    StopCoroutine(_infiniteInkCoroutine);
                    _infiniteInkCoroutine = null;
                    // 2) 이벤트 원상복귀
                    InkEvents.OnInkConsumed += _inkHandler.ConsumeInk;
                }
                // 3) 새 코루틴 시작
                _infiniteInkCoroutine = StartCoroutine(InfiniteInkRoutine(duration));
                break;

            case ItemEffectType.Invincibility:
                if(_invincibilityCoroutine != null)
                {
                    StopCoroutine(_invincibilityCoroutine);
                    _invincibilityCoroutine = null;
                    _handler.isDamaged = true;
                }
                _invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
                break;
            case ItemEffectType.TitanizeSelf:
                _scaleEffectCoroutine = StartCoroutine(
                   TitanizeRoutine(scaleMultiplier, healthMultiplier, duration, bulletScale)
               );
                break;
            case ItemEffectType.EnemySmall:
                _scaleEffectCoroutine = StartCoroutine(
                     SmallifyRoutine(scaleMultiplier, healthMultiplier, duration, bulletScale)
                 );
                break;
        }
    }

    //무적
    private IEnumerator InvincibilityRoutine(float duration)
    {
        //데미지 안 받게
        _handler.isDamaged = false;
        //풀피로 만들어주기
        _handler.Status.Health = _handler.Status.MaximumHealth;


        GodEffect(duration);

        // float baseline = _handler.Status.Health;

        // // 재귀 방지용 suppress 콜백
        // Action<float> suppress = null;
        // suppress = newHp =>
        // {
        //     if (newHp < baseline)
        //     {
        //         // 1) 잠시 해제
        //         _handler.OnHealthChanged -= suppress;
        //         // 2) 델타 보정 (이 호출은 다시 suppress 를 트리거하지 않음)
        //         _handler.UpdateHealthByDelta(baseline - newHp);
        //         // 3) 재등록
        //         _handler.OnHealthChanged += suppress;
        //     }
        //     else
        //     {
        //         baseline = newHp;
        //     }
        // };
        //
        // _healthSuppressor = suppress;
        // _handler.OnHealthChanged += _healthSuppressor;

        yield return new WaitForSeconds(duration);

        // // 해제
        // _handler.OnHealthChanged -= _healthSuppressor;
        // _healthSuppressor = null;

        //데미지 다시 받게
        _handler.isDamaged = true;
        _invincibilityCoroutine = null;  // 참조 해제
        currentEffect = null;

        ActiveEffects.Remove(ItemEffectType.Invincibility);
    }


    //잉크무한
    private IEnumerator InfiniteInkRoutine(float duration)
    {
        InfiniteInkEffect(duration);
        _inkHandler.Ink = _inkHandler.maximumInk;
        InkEvents.OnInkConsumed -= _inkHandler.ConsumeInk;
        yield return new WaitForSeconds(duration);
        InkEvents.OnInkConsumed += _inkHandler.ConsumeInk;
        _infiniteInkCoroutine = null;

        currentEffect = null;

        ActiveEffects.Remove(ItemEffectType.NoInkConsumption);
    }

    //대형화
    private IEnumerator TitanizeRoutine(
        float scaleMultiplier,
        float healthMultiplier,
        float duration, float bulletScale)
    {
        // 적용
        transform.localScale = _baseScale * scaleMultiplier;
        float newMax = _baseMaxHealth * healthMultiplier;
        _handler.Status.MaximumHealth = newMax;
        _handler.SetHealth(_handler.Status.Health + (newMax - _baseMaxHealth));

        _handler.OnBulletSizeChanged?.Invoke(bulletScale);

        var camController = _handler.playerCameraController;
        camController.viewOffset = _initialViewOffset + new Vector3(0, 0, -2f);
        camController.cameraHeightOffset = _initialCameraHeightOffset + new Vector3(0f, 1.5f, 0f); // 카메라 높이 조정

        yield return new WaitForSeconds(duration);

        // 복원
        transform.localScale = _baseScale;
        _handler.Status.MaximumHealth = _baseMaxHealth;
        if (_handler.Status.Health > _baseMaxHealth)
            _handler.SetHealth(_baseMaxHealth);

        _handler.OnBulletSizeChanged?.Invoke(1f);

        camController.viewOffset = _initialViewOffset;
        camController.cameraHeightOffset = _initialCameraHeightOffset; // 카메라 높이 복원

        _scaleEffectCoroutine = null; //코루틴 참조 해제
        currentEffect = null;

        ActiveEffects.Remove(ItemEffectType.TitanizeSelf);
    }


    //상대팀 소형화
    private IEnumerator SmallifyRoutine(
       float scaleMultiplier,
       float healthMultiplier,
       float duration, float bulletScale)
    {
        // 적용
        transform.localScale = _baseScale * scaleMultiplier;
        float newMax = _baseMaxHealth * healthMultiplier;
        _handler.Status.MaximumHealth = newMax;
        _handler.SetHealth(_handler.Status.Health * healthMultiplier);

        _handler.OnBulletSizeChanged?.Invoke(bulletScale);


        yield return new WaitForSeconds(duration);

        // 복원
        transform.localScale = _baseScale;
        _handler.Status.MaximumHealth = _baseMaxHealth;
        if (_handler.Status.Health > _baseMaxHealth)
            _handler.SetHealth(_baseMaxHealth);

        _handler.OnBulletSizeChanged?.Invoke(1f);

        _scaleEffectCoroutine = null; // 코루틴 참조 해제
        currentEffect = null; // 효과 종료라고 알려준다

        ActiveEffects.Remove(ItemEffectType.EnemySmall);
    }
    public void PlayShrinkEffectVFX(float duration = 10f)// 07.21 김건휘 디버프 이펙트 추가
    {
        if (DebuffEffectPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.down * 0.5f;

        GameObject vfx = Instantiate(DebuffEffectPrefab, spawnPos, Quaternion.identity);
        vfx.transform.SetParent(transform); // 플레이어 따라다니기
        vfx.transform.localScale = transform.localScale * 2f; // 크기는 여기서 조정
        vfx.transform.localRotation = Quaternion.Euler(Vector3.zero);
        var particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1.8f;
            main.startLifetime = Mathf.Min(main.startLifetime.constant, 1.5f);
            main.simulationSpeed = 0.8f;
            main.loop = true;
            ps.Play();
        }

        Destroy(vfx, duration);
    }
    public void PlayTitanEffectVFX(float duration)// 07.21 김건휘 거인화 이펙트 추가
    {
        if (TitanEffectPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.down * 0.7f;

        GameObject vfx = Instantiate(TitanEffectPrefab, spawnPos, Quaternion.identity);
        vfx.transform.SetParent(transform);
        vfx.transform.localScale = transform.localScale;
        vfx.transform.localRotation = Quaternion.Euler(Vector3.zero); // 플레이어가 바라보는 방향으로 회전
        var particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1.8f;
            main.startLifetime = Mathf.Min(main.startLifetime.constant, 1.5f);
            main.simulationSpeed = 1.5f;
            main.loop = true;
            ps.Play();
        }

        Destroy(vfx, duration);
    }
    public void GodEffect(float duration)// 07.21 김건휘 무적 이펙트 추가
    {
        if (GodEffectPrefab == null) return;
        Vector3 spawnPos = transform.position;
        GameObject vfx = Instantiate(GodEffectPrefab, spawnPos, Quaternion.identity);
        vfx.transform.SetParent(transform); // 플레이어 따라다니기
        vfx.transform.localScale *= 2f; // 크기는 여기서 조정
        vfx.transform.localRotation = Quaternion.Euler(Vector3.zero);
        Destroy(vfx, duration);
    }
    public void InfiniteInkEffect(float duration)// 07.21 김건휘 무한잉크 이펙트 추가
    {
        if (InfiniteInkPrefab == null) return;
        Vector3 spawnPos = transform.position + Vector3.down * 0.7f;
        GameObject vfx = Instantiate(InfiniteInkPrefab, spawnPos, Quaternion.identity);
        vfx.transform.SetParent(transform); // 플레이어 따라다니기
        vfx.transform.localScale *= 2f; // 크기는 여기서 조정
        vfx.transform.localRotation = Quaternion.Euler(Vector3.zero);
        Destroy(vfx, duration);
    }



    //리스폰때 모든 효과 취소용
    private void CancelAllEffects()
    {
        // --- 스케일 효과 취소 ---
        if (_scaleEffectCoroutine != null)
        {
            StopCoroutine(_scaleEffectCoroutine);
            _scaleEffectCoroutine = null;

            if (_baseInitialized)
            {
                // 원래 크기·체력 복원
                transform.localScale = _baseScale;
                _handler.Status.MaximumHealth = _baseMaxHealth;
                if (_handler.Status.Health > _baseMaxHealth)
                    _handler.SetHealth(_baseMaxHealth);

                // 총알 크기 복원
                _handler.OnBulletSizeChanged?.Invoke(1f);

                _baseInitialized = false;
            }
        }

        // --- 인비닌 취소 ---
        if (_invincibilityCoroutine != null)
        {
            StopCoroutine(_invincibilityCoroutine);
            _invincibilityCoroutine = null;
        }
        if (_healthSuppressor != null)
        {
            _handler.OnHealthChanged -= _healthSuppressor;
            _healthSuppressor = null;
        }

        // --- 무한잉크 취소 ---
        if (_infiniteInkCoroutine != null)
        {
            StopCoroutine(_infiniteInkCoroutine);
            _infiniteInkCoroutine = null;
        }
        // 잉크 소비 이벤트 복원
        InkEvents.OnInkConsumed -= _inkHandler.ConsumeInk; //살아있을때랑 안겹치게방어코드
        InkEvents.OnInkConsumed += _inkHandler.ConsumeInk;

        ActiveEffects.Clear();
    }
    public void PlayRespawnAura(float duration = 2f)
    {
        if (RespawnPrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.down * 0.7f;
        GameObject vfx = Instantiate(RespawnPrefab, spawnPos, Quaternion.identity);
        vfx.transform.SetParent(transform); // 플레이어 따라다니게
        vfx.transform.localScale *= 2f;
        vfx.transform.localRotation = Quaternion.identity;
        Destroy(vfx, duration);
    }

    [PunRPC]
    public void RPC_ApplyBarrierInvincibility(float duration)
    {
        ApplyEffect(ItemEffectType.Invincibility, duration);
    }
}
