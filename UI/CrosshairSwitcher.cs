using System.Collections;
using UnityEngine;

public class CrosshairSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject defaultCrossHead;   // 기본 크로스헤어
    [SerializeField] private GameObject targetCrossHead;    // 적 조준시
    [SerializeField] private GameObject hitCrossHead;       // 적 명중시
    [SerializeField] private GameObject warningCrossHead;   // 탄을 사용할 수 없는 경고용 크로스헤드
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private float hitEffectTime = 0.2f;

    private Camera cam;
    private byte myTeamId;
    private Coroutine hitCoroutine;

    // 싱글톤 만들면.. 다른 클라이언트에서도 로컬로 싱글톤 생성(플레이어마다 CrosshairSwitcher 인스턴스 각각?) > 안쓰고 하려니 구조가 너무 더러워짐 + 추가로 다른 탄환/무기 생성 시 작업이 늘어남 > 싱글톤 유지하되 불필요한 다른 플레이어 크로스헤어 오브젝트는 파괴하자
    public static CrosshairSwitcher Instance { get; private set; }

    // 발사 가능 여부
    public bool IsShootable { get; set; } = true;

    void Start()
    {
        cam = Camera.main;
        PlayerHandler myPlayer = GetComponentInParent<PlayerHandler>();
        if (myPlayer != null && myPlayer.Info != null)
        {
            // 자기 자신의 크로스헤어만 남기고 나머지는 파괴(리소스 절약)
            if (myPlayer.photonView.IsMine)
            {
                Instance = this;
                myTeamId = myPlayer.Info.TeamId;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnEnable()
    {
        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);
        // 기본 크로스헤어만 보이도록
        ShowOnly(defaultCrossHead);
    }

    void Update()
    {
        if (hitCrossHead.activeSelf) return; // 히트 연출 중이면 조준 건너뜀

        // 잉크 잔량 부족 시 경고 크로스헤드 후 리턴
        if(!IsShootable)
        {
            ShowWarning();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            var handler = hit.collider.GetComponentInParent<PlayerHandler>();

            if (handler != null &&
                !handler.photonView.IsMine &&
                handler.Info != null &&
                handler.Info.TeamId != myTeamId)
            {
                ShowOnly(targetCrossHead); // 적 조준 중
                return;
            }
        }
        ShowOnly(defaultCrossHead); // 조준 안함
    }

    private void OnDisable()
    {
        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);
    }

    public void ShowHitEffect() // 공격 성공 시(상대 팀 피격) 외부에서 호출
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitEffectRoutine());
    }

    private IEnumerator HitEffectRoutine() // 히트 크로스헤어만 ON
    {
        ShowOnly(hitCrossHead);                     
        yield return new WaitForSeconds(hitEffectTime);
        ShowOnly(defaultCrossHead);                    // 다시 디폴트로 복귀
    }

    // 맞췄을 때 + 탄환이 다 떨어졌을 때 크로스헤어는 어떻게 표현해야 할까?
    // 맞췄다는 표시 후 잉크 잔량에 따라 보통 or 탄환 다 떨어짐 표시?
    // 그러면 잉크 잔량이 1발을 쏘는 데 필요한 양보다 많은지 판정하여 bool 값을 가져오는 게 필요 > 이건 주기적으로 읽어서 판정하기보다 이벤트로 할까
    private void ShowOnly(GameObject toEnable)
    {
        // 하나만 ON
        defaultCrossHead.SetActive(toEnable == defaultCrossHead);
        targetCrossHead.SetActive(toEnable == targetCrossHead);
        hitCrossHead.SetActive(toEnable == hitCrossHead);
        warningCrossHead.SetActive(toEnable == warningCrossHead);
    }

    void ShowWarning()
    {
        defaultCrossHead.SetActive(false);
        targetCrossHead.SetActive(false);
        hitCrossHead.SetActive(false);
        warningCrossHead.SetActive(true);
    }
}
