using Photon.Pun;
using UnityEngine;

public class RigidBodyNetworkSync1 : MonoBehaviourPun, IPunObservable
{
    private Rigidbody rb;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;

    private float lastPacketTime;
    private float syncTime;
    private float syncDelay;

    private bool firstTake = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 오브젝트 → 위치, 회전, 속도 전송
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // 다른 클라이언트의 위치 정보 수신
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();

            // 최초 1회만 → 강제로 위치 맞추기 (순간이동 방지)
            if (firstTake)
            {
                rb.position = networkPosition;
                rb.rotation = networkRotation;
                firstTake = false;
            }

            // 다음 프레임부터는 예측 시작
            syncTime = 0f;
            syncDelay = (float)(PhotonNetwork.Time - lastPacketTime);
            lastPacketTime = (float)PhotonNetwork.Time;
        }
    }

    void FixedUpdate()
    {
        // 내 오브젝트는 직접 조종하니까 동기화 안 함
        if (photonView.IsMine || firstTake) return;

        syncTime += Time.fixedDeltaTime;

        // 예측 위치 계산: 받은 위치 + 속도 × 시간
        Vector3 predictedPosition = networkPosition + networkVelocity * syncTime;

        // 보간하여 자연스럽게 따라가기 (위치 + 회전)
        rb.position = Vector3.Lerp(rb.position, predictedPosition, 1f);

        Quaternion safeRotation = Quaternion.Normalize(networkRotation);
        rb.rotation = Quaternion.Slerp(rb.rotation, safeRotation, 1f);
    }
}