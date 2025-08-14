using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class PlayerCameraController : MonoBehaviourPun
{
    private PlayerHandler playerHandler;
    private PlayerAction playerAction;

    private PlayerController playerController;

    [SerializeField] public CinemachineBrain cinemachineBrainCamera;
    [SerializeField] public CinemachineVirtualCamera playerVirtualCamera;

    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 15f;

    public float minPitch = -80f;
    public float maxPitch = 80f;

    [SerializeField] private Vector2 mouseInput;
    [SerializeField] private Quaternion rotation;
    [SerializeField] private Vector3 desiredPosition;
    [SerializeField] private Vector3 cameraLookTarget;
    [SerializeField] private Vector3 currentCameraPosition;
    [SerializeField] private float followSmoothSpeed = 5f;
    public Vector3 cameraHeightOffset = new Vector3(0f, 1.5f, 0f);
    public Vector3 viewOffset = new Vector3(0f, 3f, -6f);
    public float rotationSpeed = 30f;

    private Vector3 currentVelocity;

    // private int targetLayer = (1 << (int)EnumLayer.LayerType.TeamA)
    //                            | (1 << (int)EnumLayer.LayerType.TeamB)
    //                            | (1 << (int)EnumLayer.LayerType.BulletA)
    //                            | (1 << (int)EnumLayer.LayerType.BulletB)
    //                            | (1 << (int)EnumLayer.LayerType.HockBackTriggered)
    //                            | (1 << (int)EnumLayer.LayerType.Item)
    //                            | (1 << (int)EnumLayer.LayerType.TriggerHock);

    private int targetLayer = (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                              | (1 << (int)EnumLayer.LayerType.Map_Printable)
                              | (1 << (int)EnumLayer.LayerType.Default);
    private void Start()
    {
        if (!photonView.IsMine) return;

        playerHandler = GetComponent<PlayerHandler>();
        playerController = GetComponent<PlayerController>();
        playerAction = playerHandler.playerAction;

        cinemachineBrainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineBrain>();
        playerVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
        playerVirtualCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 팀에 따라 초기 보는 방향이 전방을 향하도록 y축 회전에 영향을 줄 yaw 파라미터를 변경
    public void InitRotation(byte TeamID)
    {
        yaw = (int)TeamID == 0 ? 0 : 180;
    }

    void FixedUpdate()
    {
        //if (target == null) return;
        if (!IsPhotonViewIsMine()) return;

        CameraRotateMove();
    }

    public void CameraRotateMove()
    {
        // Input System 기반 마우스 입력 받기
        mouseInput = playerAction.PlayerActionMap.MouseAim.ReadValue<Vector2>();

        yaw += mouseInput.x * MouseSensitivitySettings.YawSensitivity * Time.fixedDeltaTime;
        pitch -= mouseInput.y * MouseSensitivitySettings.PitchSensitivity * Time.fixedDeltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        rotation = Quaternion.Euler(pitch, yaw, 0f);
        cameraLookTarget = playerController.transform.position + cameraHeightOffset;
        desiredPosition = cameraLookTarget + rotation * viewOffset;
        
        // 기존 Raycast는 매우 얇은 선을 사용하기 때문에 벽의 모서리나 틈새를 통과하기 쉽습니다.
        // 더 안정적인 충돌 감지를 위해 SphereCast를 사용하는 것이 좋습니다.
        // SphereCast는 가상의 구체를 날려 충돌을 확인하므로 카메라가 벽을 뚫는 현상을 훨씬 효과적으로 방지할 수 있습니다.
        RaycastHit hit;
        Vector3 direction = (desiredPosition - cameraLookTarget).normalized;
        float maxDistance = viewOffset.magnitude;
        // 카메라의 크기를 대변하는 가상의 구체 반경입니다. 씬에 맞게 조절할 수 있습니다.
        float cameraRadius = 0.5f; 

        Vector3 finalCameraPosition;

        if (Physics.SphereCast(cameraLookTarget, cameraRadius, direction, out hit, maxDistance, targetLayer))
        {
            // SphereCast가 어딘가에 부딪혔다면, 카메라를 충돌 지점까지만 이동시킵니다.
            // hit.distance는 광선의 시작점부터 구체가 처음 충돌한 지점까지의 거리입니다.
            finalCameraPosition = cameraLookTarget + direction * hit.distance;
        }
        else
        {
            // 충돌이 없다면, 원래 원했던 위치로 카메라를 이동시킵니다.
            finalCameraPosition = desiredPosition;
        }

        playerVirtualCamera.transform.position = finalCameraPosition;
        playerVirtualCamera.transform.LookAt(cameraLookTarget);
    }
    
    private bool IsPhotonViewIsMine()
    {
        return photonView.IsMine;
        //return true;
    }
}