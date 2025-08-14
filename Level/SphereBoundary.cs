using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SphereBoundary : MonoBehaviour
{
    SphereCollider sphereTrigger;
    LayerMask layersToBlock;

    void Awake()
    {
        sphereTrigger = GetComponent<SphereCollider>();
        sphereTrigger.isTrigger = true;
        layersToBlock = LayerMask.GetMask(
            EnumLayer.LayerType.TeamA.ToString(), 
            EnumLayer.LayerType.TeamB.ToString()
            );
    }

    private void OnTriggerExit(Collider other)
    {
        // 트리거나 막을 레이어가 아니라면 해당 구문 동작하지 않도록
        if (other.isTrigger || ((1 << other.gameObject.layer) & layersToBlock) == 0)
            return;
        // 레이어를 통해 막는 오브젝트를 판정함으로써 아래 구문은 쓸 필요가 없어짐
        //if (other.GetComponentInParent<HockHead>() != null)
        //    return;
        // ① kinematic 바디나 Rigidbody가 없는 애들은 절대 처리하지 않는다
        var rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic)
            return;

        // ② 이 아래 코드는 dynamic 바디만 진입
        Vector3 exitPos = other.transform.position;
        Vector3 center = transform.position;
        float radius = sphereTrigger.radius * transform.lossyScale.x;

        // 구 표면으로 위치 보정
        Vector3 dir = (exitPos - center).normalized;
        Vector3 clampedPos = center + dir * radius;
        other.transform.position = clampedPos;

        // 관성 제거
        rb.velocity = Vector3.zero;

        // (원한다면) 반사 속도 적용
        // rb.velocity = Vector3.Reflect(rb.velocity, -dir);
    }
}
