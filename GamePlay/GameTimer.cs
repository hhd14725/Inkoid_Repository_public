using UnityEngine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using TMPro; 
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameTimer : MonoBehaviour
{
    public event Action<int> OnTimeChanged;
    public event Action OnTimerEnded;

    int matchDuration;

    double startTime;

    //float startLocalTime;
    bool running = false;

    // 등록된 이벤트 목록
    private List<TimedEvent> scheduledEvents = new List<TimedEvent>();
    private int lastSecond; // 초 단위 변경을 감지하기 위한 변수

    private float originBgmSourcePitch = 1f;
    
    //띄워줄 텍스트를 위한 변수
    private TextMeshProUGUI announcementText;
    private Coroutine announcementCoroutine;

    // 특정 시간에 실행될 이벤트를 저장하는 내부 클래스
    private class TimedEvent
    {
        public int TriggerTime; // 실행될 시간 (남은 시간 기준)
        public Action Callback; // 실행될 함수
        public bool HasFired; // 이미 실행되었는지 확인하는 플래그
    }

    private void Start()
    {
        // SoundManager가 존재할 때만 pitch 값을 가져옵니다.
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            originBgmSourcePitch = SoundManager.Instance.bgmSource.pitch;
        }
        ////Debug.Log(GameManager.Instance.hudUI);
        //if (GameManager.Instance == null)
            //Debug.LogError("GameManager.Instance is null in GameTimer.Start()");
        //else
             //Debug.Log($"GameManager.Instance in GameTimer.Start(): {GameManager.Instance}");
        announcementText = GameManager.Instance.hudUI.AnnouncementText;
        announcementText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        RestoreBgmPitch();
        
        OnTimerEnded += RestoreBgmPitch;
    }

    private void OnDisable()
    {
        // SoundManager와 bgmSource가 모두 유효할 때만 pitch를 복원합니다.
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            SoundManager.Instance.bgmSource.pitch = originBgmSourcePitch;
        }
        OnTimerEnded -= RestoreBgmPitch;
    }

    /// 특정 남은 시간에 함수를 한 번만 실행하도록 예약합니다.
    /// <param name="triggerTime">실행할 시간 (남은 시간 기준, 예: 30초 남았을 때 실행하려면 30)</param>
    /// <param name="callback">실행할 함수</param>
    public void ScheduleEvent(int triggerTime, Action callback)
    {
        scheduledEvents.Add(new TimedEvent { TriggerTime = triggerTime, Callback = callback, HasFired = false });
    }

    //public void InitializeTimer()
    //{
    //    if (!PhotonNetwork.InRoom)
    //    {
    //        //Debug.LogWarning("[Timer] Not in room, skip init");
    //        return;
    //    }

    //    var props = PhotonNetwork.CurrentRoom.CustomProperties;
    //    if (!props.ContainsKey(CustomPropKey.MatchTimeSeconds) || !props.ContainsKey(CustomPropKey.StartTime))
    //    {
    //        //Debug.LogError("[Timer] Missing properties: MatchTimeSeconds or StartTime");
    //        return;
    //    }

    //    matchDuration = (int)props[CustomPropKey.MatchTimeSeconds];
    //    startTime = (double)props[CustomPropKey.StartTime];
    //    running = true;

    //    // Immediately notify the full duration for UI
    //    OnTimeChanged?.Invoke(matchDuration);
    //    //Debug.Log($"[Timer] Initialized: duration={matchDuration}, startTime={startTime}, now={PhotonNetwork.Time}");
    //}

    //설정한 게임시간 먼저 노출시키는 함수. 보여주기용
    public void ShowStaticTime(int duration)
    {
        running = false;
        // UI 업데이트
        OnTimeChanged?.Invoke(duration);
    }

    public void StartNetworkTimer(int duration, double networkStartTime)
    {
        matchDuration = duration;
        startTime = networkStartTime;
        running = true;
        lastSecond = duration; // lastSecond 초기화

        SettingTimerEvents(duration);

        OnTimeChanged?.Invoke(matchDuration); // 타이머 시작 시 전체 시간을 즉시 표시
    }

    //public void StartLocalTimer(int duration)
    //{
    //    matchDuration = duration;
    //    startLocalTime = Time.time;
    //    running = true;
    //    OnTimeChanged?.Invoke(matchDuration);
    //}
    void Update()
    {
        if (!running) return;

        int remain = matchDuration - (int)(PhotonNetwork.Time - startTime);
        int toShow = Mathf.Max(remain, 0);

        // lastSecond는 이전 프레임의 남은 시간을 저장합니다.
        // 현재 남은 시간(toShow)이 lastSecond보다 작아졌다면 시간이 흐른 것입니다.
        // 프레임 드랍으로 여러 초가 한 번에 경과하는 경우도 이 루프가 처리해줍니다.
        while (lastSecond > toShow)
        {
            lastSecond--; // 시간을 1초씩 감소시킵니다.
            OnTimeChanged?.Invoke(lastSecond); // UI 및 기타 리스너에 변경된 시간을 알립니다.
            CheckScheduledEvents(lastSecond); // 해당 시간에 예약된 이벤트가 있는지 확인하고 실행합니다.
        }

        // remain <= 0 조건이 충족되면 타이머를 정지하고 종료 이벤트를 호출합니다.
        // running 플래그를 확인하여 OnTimerEnded가 한 번만 호출되도록 보장합니다.
        if (remain <= 0 && running)
        {
            running = false;
            OnTimerEnded?.Invoke();
        }
    }

    /// 현재 남은 시간을 기준으로 예약된 이벤트를 확인하고 실행합니다.
    /// <param name="currentTime">현재 남은 시간</param>
    private void CheckScheduledEvents(int currentTime)
    {
        // Linq를 사용하거나 foreach를 사용해도 됩니다.
        foreach (var evt in scheduledEvents)
        {
            // 아직 실행되지 않았고, 실행할 시간이 되었다면
            if (!evt.HasFired && evt.TriggerTime == currentTime)
            {
                evt.Callback?.Invoke(); // 등록된 함수 실행
                evt.HasFired = true; // 한 번만 실행되도록 플래그 설정
            }
        }
    }

    public void ResetTimer()
    {
        running = false;
        OnTimeChanged = null;
        OnTimerEnded = null;
        Destroy(gameObject);
    }

    private void SettingTimerEvents(int gameTime)
    {
        //게임 시간의 1/3 지점, 2/3 지점에서 안내 방송
        int firstAnnounceTime = gameTime * 2 / 3;
        int secondAnnounceTime = gameTime / 3;
        
        ScheduleEvent(firstAnnounceTime, AnnounceTeamStatus);
        ScheduleEvent(firstAnnounceTime,  () => {ShowAnnouncement(firstAnnounceTime, 2f);});
        
        ScheduleEvent(secondAnnounceTime, AnnounceTeamStatus);
        ScheduleEvent(secondAnnounceTime,  () => {ShowAnnouncement(secondAnnounceTime, 2f);});
        

        //30초 알림!
        ScheduleEvent(30, () =>
        {
            // SoundManager가 유효할 때만 실행합니다.
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Last_Spurt);
                
                if (SoundManager.Instance.bgmSource != null)
                {
                    SoundManager.Instance.bgmSource.pitch *= 1.5f;
                }
            }
        });

        //0초 알림 (타이머 종료 시점)
        ScheduleEvent(0, () => 
        { 
            // SoundManager와 bgmSource가 모두 유효할 때만 pitch를 복원합니다.
            if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
            {
                SoundManager.Instance.bgmSource.pitch = originBgmSourcePitch; 
            }
        });
    }

    private void AnnounceTeamStatus()
    {
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        // 안전하게 값을 가져오기 위해 변수 선언
        int blueScore = 0;
        int redScore = 0;

        // 키가 있는지 확인하고 값을 가져옵니다.
        if (roomProps.ContainsKey("BluePoints"))
        {
            blueScore = (int)roomProps["BluePoints"];
        }

        if (roomProps.ContainsKey("RedPoints"))
        {
            redScore = (int)roomProps["RedPoints"];
        }
        
        if (blueScore > redScore)
        {
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_A_Winning);
        }
        else if (blueScore == redScore)
        {
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Same);
        }
        else
        {
            SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_B_Winning);
        }
    }

    private void RestoreBgmPitch()
    {
        // SoundManager와 bgmSource가 모두 유효할 때만 pitch를 복원합니다.
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            SoundManager.Instance.bgmSource.pitch = originBgmSourcePitch;
        }
    }
    
    public void ShowAnnouncement(int RestTime, float duration)
    {
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
        }
        announcementCoroutine = StartCoroutine(AnnounceTextCoroutine(RestTime, duration));
    }
    
    private IEnumerator AnnounceTextCoroutine(int Time, float duration)
    {
        if (announcementText == null)
        {
            yield break; 
        }

        announcementText.text = $"게임 시간이 {Time}초 남았습니다!!!";
        announcementText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (announcementText != null)
        {
            announcementText.gameObject.SetActive(false);
        }
    }
}
