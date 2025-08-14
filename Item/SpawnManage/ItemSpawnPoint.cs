using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]  // 편집 모드에서만 Awake/OnEnable 등 호출하는 어트리뷰트. OnDrawGizmos는 콜백이라서 이렇게 해놓으면 에디터에서만 그려진다.
#endif
public class ItemSpawnPoint : MonoBehaviour
{
    [Header("리스폰 대기시간(초)")]
    public float RespawnCooldown = 5f;

    [HideInInspector]
    public bool IsAvailable = true;

    private void OnDrawGizmos()
    {
        // 에디터 편집 모드에서만 기즈모 그리기
        if (Application.isPlaying)
            return;

        Gizmos.color = IsAvailable ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}