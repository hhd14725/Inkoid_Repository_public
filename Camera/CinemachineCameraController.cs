using UnityEngine;
using Cinemachine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachineCameraController : MonoBehaviourPun
{
    CinemachineVirtualCamera vCam;
    CinemachineCollider colliderExt;

    [Header("Assign this in the prefab")]
    [SerializeField] Transform cameraPivot;

    void Awake()
    {
        vCam = GetComponent<CinemachineVirtualCamera>();
        colliderExt = vCam.GetComponent<CinemachineCollider>();

        if (!photonView.IsMine)
        {
            // Remote 플레이어 카메라는 아예 꺼 버립니다..
            vCam.gameObject.SetActive(false);
            return;
        }

        // Local 플레이어는 Priority 세팅 + Follow/LookAt
        //Transform pivot = transform.Find("CameraPivot");
        //if (cameraPivot == null)
            //Debug.LogError("CameraPivot을 찾을 수 없습니다!", this);
        else
        {
            vCam.Follow = cameraPivot;
            vCam.LookAt = cameraPivot;
        }
        vCam.Priority = 100;

        // 콜라이더는 나중에(한 프레임 뒤) 켜기 위해 일단 꺼 둡니다.
        if (colliderExt != null)
            colliderExt.enabled = false;
    }

    // IEnumerator Start를 쓰면 코루틴 없이도 한 프레임 대기를 쓸 수 있습니다.
    IEnumerator Start()
    {
        // Local 플레이어에만 적용
        if (!photonView.IsMine)
            yield break;

        // 한 프레임 대기 → pivot, rigidbody, 네트워크 보간 등 모든 초기화 끝난 다음
        yield return null;

        if (colliderExt != null)
            colliderExt.enabled = true;
    }
}
