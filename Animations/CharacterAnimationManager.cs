// CharacterAnimationManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class CharacterAnimationManager : MonoBehaviour
{
    Animator _anim;
    Rigidbody _rb;
    PlayerHandler _handler;

    // 애니메이터 파라미터 해시
    static readonly int IS_MOVING = Animator.StringToHash("IsMoving");
    static readonly int VERTICAL_SPEED = Animator.StringToHash("VerticalSpeed");
    static readonly int IS_SHOOTING = Animator.StringToHash("IsShooting");
    static readonly int MOVE_DIRECTION = Animator.StringToHash("MoveDirection");
    static readonly int TRIG_GRAPPLE = Animator.StringToHash("Grapple");
    static readonly int TRIG_HIT = Animator.StringToHash("Hit");
    static readonly int TRIG_DASH = Animator.StringToHash("Dash");

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponentInParent<Rigidbody>();
        _handler = GetComponentInParent<PlayerHandler>();
    }

    void OnEnable()
    {
        // 갈고리 입력 이벤트 구독
        _handler.playerAction.PlayerActionMap.Grapple.started += OnGrapple;
        // 대쉬 이벤트 구독
        _handler.playerAction.PlayerActionMap.Dash.started += OnDash;
        // 피격 이벤트 구독
        //_handler.OnHealthChanged += OnHealthChanged;
        _handler.OnDamageTaken += OnDamageTaken;
    }

    void OnDisable()
    {
        _handler.playerAction.PlayerActionMap.Grapple.started -= OnGrapple;
        _handler.playerAction.PlayerActionMap.Dash.started -= OnDash;
        //_handler.OnHealthChanged -= OnHealthChanged;
        _handler.OnDamageTaken -= OnDamageTaken;
    }
    void OnDamageTaken(float currentHp)
    {
        _anim.SetTrigger(TRIG_HIT);
    }
    void Update()
    {
        // 1. 이동 상태 파라미터
        Vector3 horizVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        bool isMoving = horizVel.magnitude > 0.1f;
        _anim.SetBool(IS_MOVING, isMoving);
        _anim.SetFloat(VERTICAL_SPEED, _rb.velocity.y);

        // 2. 사격 중 플래그
        _anim.SetBool(IS_SHOOTING, _handler.isAttack);

        // 3. 좌우 이동 입력 (-1: 왼쪽, 0: 정면, +1: 오른쪽)
        float dir = _handler.playerAction.PlayerActionMap
                          .HorizontalMove.ReadValue<Vector2>().x;
        _anim.SetFloat(MOVE_DIRECTION, dir);
    }

    // 갈고리 발사 트리거
    void OnGrapple(InputAction.CallbackContext ctx)
    {
        _anim.SetTrigger(TRIG_GRAPPLE);
    }

    // 대시 트리거
    void OnDash(InputAction.CallbackContext ctx)
    {
        _anim.SetTrigger(TRIG_DASH);
    }

    // 피격 시 트리거
    void OnHealthChanged(float newHealth)
    {
        _anim.SetTrigger(TRIG_HIT);
    }
}
