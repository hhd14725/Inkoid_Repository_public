using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PingUI : MonoBehaviourPun
{
    public GameObject Ping;
    public Camera MainCamera;
    public float ScaleFactor;
    public float MinDist;
    public Transform Tgt;
    public float Childscale = 1.5f;
    //float dist;

    private GameObject currentPing;

    private static Dictionary<int, GameObject> teamPingMap = new Dictionary<int, GameObject>();

    //감지할 레이어들
    private int targetLayer = (1 << (int)EnumLayer.LayerType.Map_Printable)
                              | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                              | (1 << (int)EnumLayer.LayerType.Default);

    void Start()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame)
        {
            CreatingPing();
        }


        //if (Tgt == null || MinDist <= 0f) return;


        //float scale = dist > MinDist ? (dist / MinDist) * ScaleFactor : ScaleFactor;
        //if (!float.IsNaN(scale) && !float.IsInfinity(scale))
        //{
        //    transform.localScale = Vector3.one * scale;
        //}
        //if (dist > MinDist)
        //{
        //    transform.localScale = Vector3.one * (dist / MinDist) * ScaleFactor;
        //}
        //else
        //{
        //    transform.localScale = Vector3.one * ScaleFactor;
        //}
        if (Tgt == null) return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CustomPropKey.TeamId, out var teamIdObj))
        {
            int myTeamId = (byte)teamIdObj;

            if (teamPingMap.TryGetValue(myTeamId, out var myPing) && myPing != null)
            {
                float dist = Vector3.Distance(MainCamera.transform.position, myPing.transform.position);
                // 안전한 최소 거리 보장
                float safeMinDist = Mathf.Max(MinDist, 0.001f);
                float maxDist = 30f; // 최대 거리 제한
                float clampedDist = Mathf.Min(dist, maxDist);
                // √ 거리 기반 감쇠
                float rawScale = Mathf.Sqrt(clampedDist / safeMinDist) * ScaleFactor;
                // 최소/최대 스케일 제한
                float minScale = 0.25f; // 최소 스케일
                float maxScale = 2f; // 최대 스케일
                float targetScale = Mathf.Clamp(rawScale, minScale, maxScale);
                //Debug.Log("로우" + rawScale);
                //Debug.Log("타겟" + targetScale);
                // 디버그용 출력 (선택사항)
                // //Debug.Log($"Distance: {dist}, RawScale: {rawScale}, FinalScale: {targetScale}");
                // 부드럽게 본체 크기 변화
                myPing.transform.localScale = Vector3.Lerp(
                    myPing.transform.localScale,
                    Vector3.one * targetScale,
                    Time.deltaTime * Childscale
                );
                // 자식 오브젝트도 부드럽게 크기 변화
                foreach (Transform child in myPing.transform)
                {
                    Vector3 childTargetScale = Vector3.one * targetScale * Childscale;
                    child.localScale = Vector3.Lerp(
                        child.localScale,
                        childTargetScale,
                        Time.deltaTime * Childscale
                    );
                }
            }
        }
    }

    void CreatingPing()
    {
        var handler = GetComponent<PlayerHandler>();
        if (handler == null) return;

        int myTeamId = handler.Info.TeamId;

        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, targetLayer))
        {
            Vector3 point = hitInfo.point;

            GetComponent<PhotonView>().RPC(nameof(ShowPing), RpcTarget.All, point, myTeamId, hitInfo.normal);
        }
    }

    [PunRPC]
    void ShowPing(Vector3 pos, int pingTeamId, Vector3 hitInfoNormal)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties
                .TryGetValue(CustomPropKey.TeamId, out var teamIdObj)
            && (byte)teamIdObj == pingTeamId)
        {
            if (teamPingMap.ContainsKey(pingTeamId))
            {
                Destroy(teamPingMap[pingTeamId]);
            }

            // Z-파이팅 방지를 위해 법선 벡터 방향으로 약간의 오프셋을 줍니다.
            float offset = 0.05f; // 이 값은 필요에 따라 조절하세요.
            Vector3 spawnPosition = pos + hitInfoNormal * offset;

            // 오브젝트의 위쪽(Vector3.up)을 표면의 법선(hitInfo.normal) 방향으로 일치시키는 회전
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitInfoNormal);
            GameObject newPing = Instantiate(Ping, spawnPosition, rotation);

            foreach (Transform child in newPing.transform)
            {
                child.localScale *= Childscale;
            }

            var particleSystems = newPing.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.duration = 1f;
                main.startLifetime = Mathf.Min(main.startLifetime.constant, 1f);
                main.simulationSpeed = 1f;
                main.loop = true;
                ps.Play();
            }

            teamPingMap[pingTeamId] = newPing;
            Destroy(newPing, 10f);

            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_ping);
        }
    }
}