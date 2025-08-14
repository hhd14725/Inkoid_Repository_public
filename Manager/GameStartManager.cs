using Photon.Pun;
using System.Collections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameStartManager : MonoBehaviourPun
{
    public GameStartTimerUI gameStartUI;
    private int interval = 1;
    private bool started = false;
    private double? pendingStartTime = null; // RPC가 먼저 와도 값만 저장

    private void OnEnable()
    {
        NetworkManager.Instance.OnAllPlayersInGameReadyEvent += GameStart;
        NetworkManager.Instance.OnRoomPropertiesUpdatedEvent += OnRoomPropsChanged;
        TryStartFromRoomProps();
    }
    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnAllPlayersInGameReadyEvent -= GameStart;
            NetworkManager.Instance.OnRoomPropertiesUpdatedEvent -= OnRoomPropsChanged;
        }
    }
    private void OnRoomPropsChanged(Hashtable changed)
    {
        if (changed.ContainsKey(CustomPropKey.PreMatchStartTime) || changed.ContainsKey(CustomPropKey.MatchStartTime))
            TryStartFromRoomProps();
    }
    private void TryStartFromRoomProps()
    {
        if (started || !PhotonNetwork.InRoom) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        int total = gameStartUI.NumSprites.Count;

        double pre, t0;

        if (props.TryGetValue(CustomPropKey.PreMatchStartTime, out var preObj))
        {
            pre = (double)preObj;
            t0 = props.TryGetValue(CustomPropKey.MatchStartTime, out var t0Obj) ? (double)t0Obj : pre + total;
        }
        else if (props.TryGetValue(CustomPropKey.MatchStartTime, out var t0Obj2))
        {
            t0 = (double)t0Obj2;
            pre = t0 - total;
        }
        else
        {
            return; // 아직 시각이 안 박힘
        }

        // TimerInstance가 생성될 때까지 대기
        StartCoroutine(WaitTimerThenStart(pre, t0));
    }
    private IEnumerator WaitTimerThenStart(double pre, double t0)
    {
        while (GameManager.Instance == null || GameManager.Instance.TimerInstance == null)
            yield return null;

        BeginLocalStart(pre, t0);
    }
    private void BeginLocalStart(double pre, double t0)
    {
        if (started) return;

        int total = gameStartUI.NumSprites.Count;

        // 카운트다운 UI 세팅
        gameStartUI.SetNumImage(total - 1);
        gameStartUI.currentImage.enabled = true;

        // 게임 시간 고정 표시
        int matchDuration = (int)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.MatchTimeSeconds];
        GameManager.Instance.TimerInstance.ShowStaticTime(matchDuration);

        started = true;
        StartCoroutine(CountdownFrameDriven(pre, t0)); // 프레임 기반 카운트다운
    }

    // 프레임 기반: 로딩이 느려서 시간이 지나있어도 ‘현재 숫자’만 정확히 보여줌(순간이동 방지)
    private IEnumerator CountdownFrameDriven(double startTime, double t0)
    {
        int total = gameStartUI.NumSprites.Count;
        int prevSecond = -1; // 이전 초


        while (PhotonNetwork.Time < t0)
        {
            double elapsed = PhotonNetwork.Time - startTime;
            int tick = Mathf.Clamp((int)Mathf.Floor((float)elapsed), 0, total - 1);
            if (tick != prevSecond)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_CountDown, 1f);
                prevSecond = tick;
            }


            int idx = total - 1 - tick;
            gameStartUI.SetNumImage(idx);
            gameStartUI.currentImage.enabled = true;
            yield return null;
        }

        gameStartUI.currentImage.enabled = false;

        int duration = (int)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.MatchTimeSeconds];
        GameManager.Instance.TimerInstance.StartNetworkTimer(duration, t0);

        PlayerActionEnable();
        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Start, 1f);

        GameManager.Instance.isGamePlaying = true;

        yield return null;
        gameObject.SetActive(false);
    }

    private void GameStart()
    {
        if (this == null) return;
        if (!PhotonNetwork.IsMasterClient) return;
        //한번 발동 이 후에 구독 해제
        NetworkManager.Instance.OnAllPlayersInGameReadyEvent -= GameStart;
        double startTime = PhotonNetwork.Time;
        //한번만 카운트다운 시점 호스트가 지정해서 시작하도록 모두에게 buffered로 전파. 호스트가 직후 탈주하더라도 전파한 사실 기준으로 모두가 카운트다운.
        photonView.RPC(nameof(RPC_BroadcastStart), RpcTarget.All, startTime);

        //StartCoroutine(StartCountdown(interval));

    }

    [PunRPC]
    void RPC_BroadcastStart(double startTime)
    {
        // 오브젝트가 비활성일 수 있으니 코루틴 직접 시작하지 말고 값만 저장
        pendingStartTime = startTime;
        TryStartFromRoomProps();
    }

    // 호스트 퇴장해도 계속 돌 수 있는, 로컬 전용 코루틴
    private IEnumerator StartLocalCountdown(double startTime)
    {
        int total = gameStartUI.NumSprites.Count;
        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_CountDown, 1f);
        // 0초부터 total-1초까지, tick 단위로
        for (int tick = 0; tick < total; tick++)
        {
            double tickTime = startTime + tick * interval;
            // 다음 틱 타임까지 대기
            while (PhotonNetwork.Time < tickTime)
                yield return null;

            // 남은 숫자 = total - 1 - tick
            int idx = total - 1 - tick;
            gameStartUI.SetNumImage(idx);
            gameStartUI.currentImage.enabled = true;
          
        }
        
        // 마지막 “1” 을 보여주고, 1초 더 대기(=총 total*interval)
        double endTime = startTime + total * interval;
        while (PhotonNetwork.Time < endTime)
            yield return null;

        //  3) 숨기고 게임 타이머 시작
        gameStartUI.currentImage.enabled = false;

        int duration = (int)PhotonNetwork.CurrentRoom
            .CustomProperties[CustomPropKey.MatchTimeSeconds];
        GameManager.Instance.TimerInstance
            .StartNetworkTimer(duration, endTime);

        //  4) 플레이어 컨트롤 활성화
        PlayerActionEnable();

        //  5) 시작 효과음 (optional)
        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Start, 1f);

        
        yield return null;
        //  6) 자기 자신 비활성화 
        gameObject.SetActive(false);
    }

    //구버전 호스트가 직접 rpc를 이용해 카운트다운을 시작하던 코루틴
    //public IEnumerator StartCountdown(float interval = 1f)
    //{
    //    SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_CountDown, 1f); // 카운트다운 사운드 재생

    //    //카운트다운시작전 방만들때 설정한 게임시간보여줌.
    //    int matchDuration = (int)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.MatchTimeSeconds];
    //    photonView.RPC(nameof(RPC_ShowInitialTimer), RpcTarget.All, matchDuration);

    //    ////Debug.Log("Starting GameStart");
    //    // 5부터 1까지 카운트다운
    //    for (int i = gameStartUI.NumSprites.Count; i >= 1; i--)
    //    {
    //        //gameStartUI.SetNumImage(i-1);   // 숫자에 맞는 스프라이트 설정
    //        photonView.RPC(nameof(RPC_ShowCountdown), RpcTarget.All, i - 1);

    //        yield return new WaitForSeconds(interval);


    //    }

    //    photonView.RPC(nameof(RPC_HideCountdown), RpcTarget.All);
    //    double now = PhotonNetwork.Time;
    //    photonView.RPC(nameof(RPC_StartGameTimer), RpcTarget.All,
    //    (int)PhotonNetwork.CurrentRoom.CustomProperties[CustomPropKey.MatchTimeSeconds],
    //    now);

    //    photonView.RPC(nameof(RPC_EnablePlayers), RpcTarget.All);
    //    SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Start, 1f);

    //    //gameStartUI.currentImage.enabled = false;  // 다 끝나면 꺼버리기 (필요한 경우)

    //    // TODO: 게임 시작 처리
    //    ////Debug.Log("게임 시작!");
    //    //PlayerActionEnable();
    //    yield return null;

    //    // 사용이 끝난 카운트다운 오브젝트 비활성화 > 이벤트 구독 해제
    //    gameObject.SetActive(false);
    //}

    private void PlayerActionEnable()
    {
        if (GameManager.Instance.players.TryGetValue(
                PhotonNetwork.LocalPlayer.ActorNumber,
                out PlayerHandler localPlayer)
            )
        {
            localPlayer.playerAction.Enable();
        }
        else
        {
            //Debug.LogWarning("로컬 플레이어 못 찾음");
        }
    }


    [PunRPC]
    void RPC_ShowCountdown(int spriteIndex)
    {
        gameStartUI.currentImage.enabled = true;
        gameStartUI.SetNumImage(spriteIndex);
    }

    [PunRPC]
    void RPC_HideCountdown()
    {
        gameStartUI.currentImage.enabled = false;
    }

    [PunRPC]
    void RPC_StartGameTimer(int duration, double networkStartTime)
    {
        GameManager.Instance.TimerInstance.StartNetworkTimer(duration, networkStartTime);
    }

    [PunRPC]
    void RPC_EnablePlayers()
    {
        PlayerActionEnable();
    }

    [PunRPC]
    void RPC_ShowInitialTimer(int duration)
    {
        // 카운트다운 도중에만 시간 고정으로 보여줌
        GameManager.Instance.TimerInstance.ShowStaticTime(duration);
    }
}
