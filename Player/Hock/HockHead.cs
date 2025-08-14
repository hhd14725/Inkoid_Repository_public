using UnityEngine;
using Photon.Pun;
using Random = UnityEngine.Random;

public class HockHead : MonoBehaviourPun
{
    // 훅 상태 정의
    private enum HockState
    {
        Idle, // 대기 상태
        Shot, // 발사됨
        Grabbed, // 벽에 고정됨
        Returning // 되돌아오는 중
    }

    [Header("Hook Settings")] [SerializeField]
    private float hockSpeed = 100f; // 발사 속도

    [SerializeField] private float hockBackSpeed = 10000f; // 되돌아올 때 속도
    [SerializeField] private float playerToHockSpeed = 250f; // 플레이어가 훅으로 날아가는 속도
    private float boostMaxSpeed; // 플레이어 속도 제한 잠깐 풀 수치
    private float originMaxSpeed;
    [SerializeField] private float hookPullMaxRange = 50f;
    [SerializeField] private float hookPullLimit = 2f;

    [SerializeField, Tooltip("훅을 꽂을 때 레이캐스트 횟수")]
    int rayCount = 20;

    [SerializeField, Tooltip("레이캐스트 방사형 각도 범위")]
    float spreadAngle = 30f;

    // 붙을 수 있는 레이어
    int grappleableLayers = (1 << (int)EnumLayer.LayerType.Map_Printable)
                            | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                            | (1 << 0); // 디폴트 레이어 추가

    //조준할 레이어
    int TargetLayer = (1 << (int)EnumLayer.LayerType.Map_Printable)
                      | (1 << (int)EnumLayer.LayerType.Map_NotPrint); 

    //레이 쏘는 거리
    float maxDistance = 1000f;

    //레이맞은 위치와 내 위치간 거리
    private float hitPointToHockDistance;

    //조준점으로 쏘는 최소 사거리
    private float minShootDistance = 3f;

    [Header("Object References")] [SerializeField]
    private string ropeRendererText = "RopeRenderer"; // 로프 라인 렌더러 오브젝트 이름

    [SerializeField] private string hockFirePositionText = "HockFirePosition"; // 훅 시작 위치 오브젝트 이름

    // 내부 컴포넌트 참조
    private Transform player;
    private Rigidbody playerRigidbody;
    private PlayerHandler playerHandler;
    private PlayerCameraController playerCameraController;

    private Rigidbody hockRigidbody;
    private LineRenderer ropeLineRenderer;
    public Transform firePosition;
    private Vector3 shootDir;
    private TriggerHock triggerHock;
    private Collider hockHaedCollision;
    private float hcokHeadToFirePositionDistance;
    [SerializeField] private float ResetHockDistance = 3f;

    private HockState currentState = HockState.Idle; // 현재 상태
    private Vector3 _baseScale;
    byte myTeam;

    private void Awake()
    {
        _baseScale = transform.localScale;
        player = transform.parent;

        firePosition = player.Find(hockFirePositionText);
        //if (firePosition == null)
            //Debug.LogError("HockFirePosition을 찾을 수 없습니다.");

        playerHandler = player.GetComponent<PlayerHandler>();
        playerRigidbody = player.GetComponent<Rigidbody>();
        playerCameraController = player.GetComponent<PlayerCameraController>();

        hockRigidbody = GetComponent<Rigidbody>();
        hockRigidbody.isKinematic = true;
        hockRigidbody.useGravity = false;

        // LineRenderer 설정
        ropeLineRenderer = player.Find(ropeRendererText)?.GetComponent<LineRenderer>();
        //if (ropeLineRenderer == null)
            //Debug.LogError("RopeRenderer를 찾을 수 없습니다.");

        ropeLineRenderer.positionCount = 2;
        ropeLineRenderer.widthMultiplier = 0.3f;

        // 트리거 훅 이벤트 연결
        triggerHock = GetComponentInChildren<TriggerHock>();
        if (triggerHock != null)
            triggerHock.OnHookTriggered += HandleReturnToPlayer;
        //else
            //Debug.LogWarning("TriggerHock이 자식으로 존재하지 않습니다.");


        //훅해드 콜라이더 가져오기
        hockHaedCollision = GetComponent<Collider>();
    }

    private void Start()
    {
        myTeam = playerHandler.Info.TeamId;
        originMaxSpeed = playerHandler.Status.MaximumMoveSpeed;
        boostMaxSpeed = playerToHockSpeed;
    }

    private void OnEnable()
    {
        //playerHandler.OnDeath += HanderResetHook;
        playerHandler.OnBeforeDeath += ResetHook;
    }
    
    private void OnDisable()
    {
        //playerHandler.OnDeath -= HanderResetHook;
        playerHandler.OnBeforeDeath -= ResetHook;
    }

    private void Update()
    {
        UpdateRopeLine();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        switch (currentState)
        {
            case HockState.Idle:
                TryShoot(); // 발사 시도
                break;

            case HockState.Shot:
                // 너무 멀어지면 자동 리턴
                if (Vector3.Distance(player.position, transform.position) >= hookPullMaxRange)
                    SetState(HockState.Returning);
                break;

            case HockState.Grabbed:
                if (Vector3.Distance(player.position, transform.position) >= hookPullLimit)
                {
                    MovePlayerToHook(); // 플레이어 당기기
                }

                break;

            case HockState.Returning:
                UpdateHockPositionToHockHead();
                if (hcokHeadToFirePositionDistance <= ResetHockDistance)
                {
                    ResetHook();
                }
                ReturnHook(); // 훅 되돌리기
                break;
        }

        // 버튼에서 손 뗐을 때 리턴 시작
        if (!playerHandler.isTryGrapple && currentState != HockState.Idle)
            SetState(HockState.Returning);
    }

    private void TryShoot()
    {
        if (!playerHandler.isTryGrapple) return;

        SetState(HockState.Shot);

        transform.position = firePosition.position;
        transform.rotation = playerCameraController.playerVirtualCamera.transform.rotation;
        transform.parent = null;
        hockRigidbody.isKinematic = false;
        hockRigidbody.velocity = Vector3.zero;
        hockRigidbody.useGravity = true;

        Vector3 playerCameraDirection = playerCameraController.playerVirtualCamera.transform.forward;
        Ray flontCameraRay = new Ray(
            playerHandler.transform.position + playerCameraController.cameraHeightOffset
            , playerCameraDirection);
        // //Debug.DrawRay(
        //     playerCameraController.playerVirtualCamera.transform.position
        //     , playerCameraDirection * 100f
        //     , Color.red
        //     , 5f);
        // //Debug.DrawRay(
        //     playerCameraController.playerVirtualCamera.transform.position
        //     , playerCameraDirection * 3f
        //     , Color.green
        //     , 10f);


        //레이캐스트 발사 (사거리 변수 사용)
        if (Physics.Raycast(flontCameraRay, out RaycastHit hit, maxDistance, TargetLayer))
        {
            // 레이가 타켓레이어에 맞았을 경우 (기존과 거의 동일)
            Vector3 hitPoint = hit.point;

            hitPointToHockDistance = Vector3.Distance(hitPoint, transform.position);

            //레이 맞은 위치랑 내 위치랑 계산해서
            //if (hitPointToHockDistance > minShootDistance)
            //{
                //거리가 괜찮으면 레이 맞은 위치로 쏘기
                shootDir = hitPoint - firePosition.position;
            //}
            // else
            // {
            //     //거리가 너무 가깝다면 그냥 대각으로 쏘기
            //     shootDir = transform.forward*3f+(transform.right);
            // }
        }
        else
        {
            // 레이가 아무것에도 맞지 않았을 경우
            // flontCameraRay의 시작점으로부터 maxDistance만큼 떨어진 지점을 목표로 설정
            Vector3 endPoint = flontCameraRay.GetPoint(maxDistance);
            shootDir = endPoint - firePosition.position;
        }

        //계산된 shootDir 방향으로 힘을 가함 (공통 로직)
        transform.rotation = Quaternion.LookRotation(shootDir.normalized);
        hockRigidbody.AddForce((shootDir.normalized * hockSpeed) + playerRigidbody.velocity, ForceMode.Impulse);
    }

    private void MovePlayerToHook()
    {
        Vector3 pullDir = transform.position - player.position;
        playerRigidbody.AddForce(pullDir.normalized * playerToHockSpeed, ForceMode.Force);
    }

    private void ReturnHook()
    {
        if(!hockRigidbody.isKinematic)
            hockRigidbody.velocity = Vector3.zero;
        Vector3 returnDir = firePosition.position - transform.position;
        hockRigidbody.AddForce(returnDir.normalized * hockBackSpeed, ForceMode.Force);
    }

    private void UpdateRopeLine()
    {
        ropeLineRenderer.SetPosition(0, transform.position);
        ropeLineRenderer.SetPosition(1, firePosition.position);
    }

    private void UpdateHockPositionToHockHead()
    {
        hcokHeadToFirePositionDistance = Vector3.Distance(transform.position, firePosition.position);
    }

    private void SetState(HockState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case HockState.Idle:
                ResetHook();
                break;

            case HockState.Shot:

                break;

            case HockState.Grabbed:
                hockRigidbody.velocity = Vector3.zero;
                hockRigidbody.isKinematic = true;
                hockRigidbody.useGravity = false;
                playerHandler.Status.MaximumMoveSpeed = boostMaxSpeed;

                break;

            case HockState.Returning:
                hockRigidbody.isKinematic = false;
                hockRigidbody.useGravity = true;
                playerHandler.Status.MaximumMoveSpeed = originMaxSpeed;

                hockHaedCollision.isTrigger = true;

                InkEvents.ResetInkRecovery();
                SoundManager.Instance.StopLoopSfx();
                break;
        }
    }

    public void ResetHook()
    {
        SoundManager.Instance.StopLoopSfx();
        float ps = player.localScale.x; // 아이템 사용 대비, (TianizeSelf/Smallify 모두 uniform scale 이라 가정)
        transform.localScale = _baseScale * ps;
        hockRigidbody.isKinematic = false;
        hockRigidbody.velocity = Vector3.zero;
        hockRigidbody.isKinematic = true;
        hockRigidbody.useGravity = false;

        hockHaedCollision.isTrigger = false;

        transform.position = firePosition.position;
        transform.rotation = firePosition.rotation;
        transform.parent = player;

        currentState = HockState.Idle;
        ////Debug.Log("훅 리셋");
    }

    // public void HanderResetHook(PlayerHandler playerHandler)
    // {
    //     ResetHook();
    // }

    // 플레이어 훅 포지션 트리거에 들어왔을 때 훅 리셋
    private void HandleReturnToPlayer()
    {
        if (currentState == HockState.Returning)
            ResetHook();
    }

    // 벽에 닿았을 때 훅 고정
    private void OnCollisionEnter(Collision collision)
    {
        if (!playerHandler.isTryGrapple) return;

        if (((1 << collision.gameObject.layer) & grappleableLayers) != 0)
        {
            var paintableObject = collision.collider.GetComponentInParent<PaintableObject>();
            if (paintableObject != null)
            {
                for (int i = 0; i < rayCount; i++)
                {
                    // 랜덤 각도 생성
                    float AngleX = Random.Range(-spreadAngle, spreadAngle);
                    float AngleY = Random.Range(-spreadAngle, spreadAngle);
                    Vector3 shootDir = Quaternion.Euler(AngleX, AngleY, 0) * transform.forward;

                    Ray ray = new Ray(transform.position, shootDir);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 1f, (1 << (int)EnumLayer.LayerType.Map_Printable)))
                    {
                        //Debug.DrawRay(ray.origin, ray.direction * 1f, Color.green, 10f);
                        ////Debug.Log($"Hit Point: {hit.point}, UV: {hit.textureCoord}");

                        Vector2 uv = hit.textureCoord;

                        RenderTexture rt = paintableObject.GetMaskTmp();
                        Texture2D tmpTex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                        RenderTexture prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        tmpTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        tmpTex.Apply();
                        RenderTexture.active = prev;

                        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * rt.width), 0, rt.width - 1);
                        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * rt.height), 0, rt.height - 1);
                        Color teamPosession = tmpTex.GetPixel(x, y);
                        // //Debug.Log("팀 점유 정보 : " + teamPosession.g + ", " + teamPosession.r);
                        Destroy(tmpTex);

                        int painted = Mathf.RoundToInt(teamPosession.g);
                        int teamID = Mathf.RoundToInt(teamPosession.r);

                        // 해당 UV에 색칠이 되어 있고, 자신의 팀이 색칠한 부분일 때
                        if (painted == 1 && (int)myTeam == teamID)
                        {
                            SoundManager.Instance.PlayLoopSfx(ClipIndex_SFX.mwfx_Reload, 2f);
                            // //Debug.Log("잉크 부스트");
                            InkEvents.BoostInkRecovery();
                            break;
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("Raycast 실패: 유효한 UV를 얻지 못함");
                    }
                }
            }

            SetState(HockState.Grabbed);
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_HookHit);
        }
        else
        {
            //붙을수 없는 놈일 때 바로 돌아가게
            SetState(HockState.Returning);
            // 튕기는 사운드 재생은 이 자리에
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_HookMiss);
        }
    }
}