using UnityEngine;

[ExecuteAlways]
public class UIFrontCam : MonoBehaviour
{
    Camera targetCamera;

    void Awake()
    {
        // 메인 카메라 캐시
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        // 1) 완전히 카메라를 향하도록 회전
        Vector3 dir = transform.position - targetCamera.transform.position;
        transform.rotation = Quaternion.LookRotation(dir);

        // → 만약 수직 축(Y축)만 회전시키고 싶으면 아래처럼 바꿔 주세요:
        // Vector3 flatDir = new Vector3(dir.x, 0, dir.z);
        // transform.rotation = Quaternion.LookRotation(flatDir);
    }
}