using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PlayerHandler))]
public class PlayerController : MonoBehaviourPun
{
    private PlayerInfo playerInfo;
    private PlayerStatus playerStatus;
    private PlayerHandler playerHandler;
    private PlayerAction playerAction;

    private PlayerCameraController playerCameraController;

    public Rigidbody playerRigidbody;
    private bool dashPressed = false;
    private Vector3 HorizontalityMoveDirection;
    private Vector3 activeMoveForce;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Quaternion targetRotation;
    private Vector3 cameraForward;
    private Vector3 cameraRight;
    private Vector3 cameraUp;

    private Vector2 horizontalityMoveInput;
    private Vector2 VerticalMoveInput;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerHandler = GetComponent<PlayerHandler>();
    }

    private void Start()
    {
        playerAction = playerHandler.playerAction;
        playerCameraController = playerHandler.playerCameraController;
    }

    private void FixedUpdate()
    {
        if (!IsPhotonViewIsMine()) return;

        Move();

        if (playerHandler.isDash)
        {
            Dash();
            playerHandler.isDash = false;
        }

        // if (playerHandler.isAttack)
        // {
        //     Attack();
        //     playerHandler.isAttack = false;
        // }
    }

    private void Move()
    {
        // 1.이동방향 입력 받기
        horizontalityMoveInput = playerAction.PlayerActionMap.HorizontalMove.ReadValue<Vector2>();
        VerticalMoveInput = playerAction.PlayerActionMap.VerticalMove.ReadValue<Vector2>();

        // 2. 카메라 기준 방향 설정
        cameraForward = playerCameraController.playerVirtualCamera.transform.forward;
        cameraRight = playerCameraController.playerVirtualCamera.transform.right;
        //cameraUp = transform.position;

        // y축 방향 제거 (지면 기준 방향으로)
        cameraForward.y = 0;
        cameraForward.Normalize();
        cameraRight.y = 0;
        cameraRight.Normalize();

        // 3. 카메라 기준 이동 방향 계산
        HorizontalityMoveDirection = cameraForward * horizontalityMoveInput.y
                                     + cameraRight * horizontalityMoveInput.x;

        // 4. 최종 이동 벡터
        activeMoveForce = HorizontalityMoveDirection.normalized
                          * playerHandler.Status.HorizontalityMoveSpeed
                          + Vector3.up * (VerticalMoveInput.y * playerHandler.Status.VerticalMoveSpeed);

        // 6. 이동 적용
        playerRigidbody.AddForce(activeMoveForce, ForceMode.Force);

        // 7. 최대 속도 제한
        if (playerRigidbody.velocity.magnitude > playerHandler.Status.MaximumMoveSpeed)
        {
            playerRigidbody.velocity = playerRigidbody.velocity.normalized
                                       * playerHandler.Status.MaximumMoveSpeed;
        }

        // 8. 마지막 방향 저장
        if (activeMoveForce != Vector3.zero)
        {
            lastMoveDirection = activeMoveForce;
        }
        
        //9.공격할 때 카메라 방향으로 회전
        if (playerHandler.isAttack)
        {
            targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,2f * Time.fixedDeltaTime);
        }
        //이동 방향이 있고 공격 안할 때 이동 방향으로 회전
        else if (activeMoveForce != Vector3.zero && !playerHandler.isAttack)
        {
            targetRotation = Quaternion.LookRotation(activeMoveForce,playerCameraController.playerVirtualCamera.transform.up);
            transform.rotation = Quaternion.Slerp
                (transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
            //transform.rotation = targetRotation;
        }
        //대기할 때 자세 초기화
        else if (!playerHandler.isAttack)
        {
            Vector3 currentEuler = transform.rotation.eulerAngles;
            Quaternion uprightRotation = Quaternion.Euler(0f, currentEuler.y, 0f); // 기울기 제거
            transform.rotation = Quaternion.Slerp(transform.rotation, uprightRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void Dash()
    {
       playerHandler.Status.MaximumMoveSpeed += playerHandler.Status.DashForce;
        
        playerRigidbody.AddForce(lastMoveDirection.normalized
                                 * playerHandler.Status.DashForce, ForceMode.Impulse);
        
        playerHandler.Status.MaximumMoveSpeed -= playerHandler.Status.DashForce;
    }

    // private void Attack()
    // {
    //     
    // }

    private bool IsPhotonViewIsMine()
    {
        return photonView.IsMine;
        //return true;
    }
}