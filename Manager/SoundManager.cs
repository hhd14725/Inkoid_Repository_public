using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// 주의! : AudioClip[] sfxClips 인스펙터의 순서와 enum 순서를 동일하게 할 것
// > 그렇지 않으면 다른 이펙트가 출력
public enum ClipIndex_SFX
{
    mwfx_paint,
    mwfx_paint2,
    mwfx_watersplash,
    mwfx_watersplash_small,
    mwfx_ping,
    mwfx_ItemPickUp,
    mwfx_Dash,
    mwfx_Hook,
    mwfx_CountDown,
    mwfx_Start,
    mwfx_Reload,
    mwfx_Respawn,
    mwfx_Die,
    mwfx_Click,
    mwfx_Charging,
    mwfx_ChargeShot,
    mwfx_RPG,
    mwfx_Shotgun,
    mwfx_RPGExplosion,
    mwfx_HookHit,
    mwfx_HookMiss,
    mwfx_ChargingFinish,
    mwfx_capture,
    mwfx_A_Winning,
    mwfx_B_Winning,
    mwfx_Same,
    mwfx_Last_Spurt,
    mwfx_Warning,
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("여기에 씬이름을 적으세여")]
    [SerializeField] private string[] sceneNames;   
    [SerializeField, Header("Audio Clips")] private AudioClip[] bgmClips;
    // 7/13 유빈
    // 사운드 효과음 클립들을 여기서 관리
    // 한번만 객체를 생성하면 되고, 매번 탄 생성 때마다 생성/메모리에 넣을 필요가 없어짐
    [SerializeField] private AudioClip[] sfxClips;

    [Header("초기 볼륨 (0~0.05)")]
    [SerializeField, Range(0f, 0.1f)] private float bgmVolume = 0.05f;

    public AudioSource bgmSource;

    [Header("SFX Volume")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;
    private AudioSource sfxSource;

    [Header("게임End")]
    [SerializeField] private AudioClip endingBgmClip;
    public AudioClip EndingBgmClip => endingBgmClip;

    private AudioSource loopSfxSource;
    private Dictionary<int,AudioSource> otherAudioSources;

    public float BgmVolume
    {
        get { return bgmVolume; }
    }
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        loopSfxSource = gameObject.AddComponent<AudioSource>();
        loopSfxSource.loop = true;
        loopSfxSource.playOnAwake = false;

        otherAudioSources = new Dictionary<int, AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        StopAllSfxImmediately();

        int idx = Array.IndexOf(sceneNames, scene.name);
        if (idx >= 0 && idx < bgmClips.Length && bgmClips[idx] != null)
        {
            if (bgmSource.clip == bgmClips[idx] && bgmSource.isPlaying)
            {
                return;
            }
            if (bgmSource.clip != bgmClips[idx] || !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmClips[idx];
                bgmSource.Play();
            }
        }
    }
    public void ChangeBgm(AudioClip newClip) // 엔드팝업에서 쓰는중
    {
        if (newClip == null) return;

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.loop = false; 
        bgmSource.Play();
    }
    public void SetBgmVolume(float volume)
    {
        
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void SetSfxVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        float finalVol = Mathf.Clamp01(volumeScale * sfxVolume);
        sfxSource.PlayOneShot(clip, finalVol);
    }
    //public void PlayButtonClick()
    //{
    //    if (sfxClips != null && sfxClips.Length > (int)ClipIndex_SFX.mwfx_ButtonClick)
    //    {
    //        PlaySfx(ClipIndex_SFX.mwfx_ButtonClick);
    //    }
    //}

    // ClipIndex_SFX를 받아 효과음을 재생할 수 있도록 오버로딩 메서드 추가
    public void PlaySfx(ClipIndex_SFX clipIndex, float volumeScale = 1f)
    {
        AudioClip clip = sfxClips[(int)clipIndex];
        if (clip == null) return;
        float finalVol = Mathf.Clamp01(volumeScale * sfxVolume);
        sfxSource.PlayOneShot(clip, finalVol);
    }
    
    // // 수정된 PlaySfx 메서드
    // public void PlayChargerSfx(ClipIndex_SFX clipIndex, float chargingTime, float volumeScale = 1f)
    // {
    //     // 기존에 재생중인 동일한 클립이 있다면 중지하고 제거
    //     StopManagedSfx(clipIndex);
    //
    //     AudioClip clip = sfxClips[(int)clipIndex];
    //     if (clip == null || chargingTime <= 0) return;
    //
    //     AudioSource tempSource = gameObject.AddComponent<AudioSource>();
    //
    //     float finalVol = Mathf.Clamp01(volumeScale * sfxVolume);
    //     tempSource.volume = finalVol;
    //     tempSource.pitch = clip.length / chargingTime;
    //
    //     tempSource.clip = clip;
    //     tempSource.Play();
    //
    //     // Dictionary에 AudioSource 추가
    //     otherAudioSources.Add((int)clipIndex, tempSource);
    // }
    
    // 관리되는 특정 SFX를 중지하는 메서드
    public void StopManagedSfx(ClipIndex_SFX clipIndex)
    {
        int key = (int)clipIndex;
        if (otherAudioSources.TryGetValue(key, out AudioSource sourceToStop))
        {
            if (sourceToStop != null)
            {
                sourceToStop.Stop();
                Destroy(sourceToStop);
            }
            otherAudioSources.Remove(key);
        }
    }
    
    // 관리되는 모든 SFX를 중지하는 메서드
    public void StopAllManagedSfx()
    {
        foreach (var source in otherAudioSources.Values)
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source);
            }
        }
        otherAudioSources.Clear();
    }
    
    
    /// <summary>
    /// [개선된 버전] 첫 사운드가 끝나면 다음 사운드를 재생합니다. 요청한 오브젝트가 사라지면 자동으로 중단됩니다.
    /// </summary>
    /// <param name="owner">이 사운드를 요청한 게임 오브젝트 (보통 this.gameObject)
    /// <param name="firstClip">먼저 재생할 클립
    /// <param name="duration">첫 클립의 재생 시간
    /// <param name="nextClip">첫 클립이 끝난 후 재생할 클립
    /// <param name="volumeScale">볼륨
    public void PlaySfxAndThen(GameObject owner, ClipIndex_SFX firstClip, float duration, ClipIndex_SFX nextClip, float volumeScale = 1f)
    {
        StartCoroutine(PlaySfxSequence(owner, firstClip, duration, nextClip, volumeScale));
    }

    private System.Collections.IEnumerator PlaySfxSequence(GameObject owner, ClipIndex_SFX firstClip, float duration, ClipIndex_SFX nextClip, float volumeScale)
    {
        // --- 1. 첫 번째 사운드(차징음) 재생 ---
        StopManagedSfx(firstClip); // 혹시 이전에 실행된게 있다면 정리

        AudioClip clip = sfxClips[(int)firstClip];
        if (clip == null || duration <= 0)
        {
            yield break; // 클립이 없거나 재생시간이 0이면 코루틴 종료
        }

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.volume = Mathf.Clamp01(volumeScale * sfxVolume);
        tempSource.pitch = clip.length / duration; // 재생시간을 duration에 맞춤
        tempSource.clip = clip;
        tempSource.Play();

        // 중간에 취소될 수 있도록 Dictionary에 등록
        otherAudioSources.Add((int)firstClip, tempSource);

        // --- 2. 첫 번째 사운드가 끝날 때까지 기다림 ---
        yield return new WaitForSeconds(duration);

        // --- 3. [안전 장치] 사운드를 요청한 오브젝트가 사라졌는지 확인 ---
        if (owner == null || !owner.activeInHierarchy)
        {
            // 요청한 오브젝트가 죽거나 비활성화되면, 여기서 코루틴을 중단!
            StopManagedSfx(firstClip); // 남아있는 사운드 찌꺼기 정리
            yield break;
        }

        // --- 4. 사운드가 중간에 취소되지 않았는지 확인 ---
        // Dictionary에 아직 AudioSource가 남아있다면, 정상적으로 재생이 끝난 것
        if (otherAudioSources.ContainsKey((int)firstClip))
        {
            // 재생이 안끊겼으니, 다음 사운드를 재생
            PlaySfx(nextClip, volumeScale);

            // 임무를 마친 첫 번째 사운드를 정리
            StopManagedSfx(firstClip);
        }
        // 만약 중간에 StopManagedSfx가 호출되었다면, AudioSource가 이미 제거되었을 것이므로
        // 이 부분은 실행되지 않고, 다음 사운드도 재생되지 않습니다.
    }



    public void PlayLoopSfx(ClipIndex_SFX clipIndex, float volume = 1f)
    {
        if ((int)clipIndex >= sfxClips.Length)
        {
            //Debug.LogError($"ClipIndex {clipIndex} is out of range");
            return;
        }

        if (loopSfxSource.isPlaying && loopSfxSource.clip == sfxClips[(int)clipIndex])
            return; // 이미 같은 사운드 재생 중이면 무시

        loopSfxSource.clip = sfxClips[(int)clipIndex];
        loopSfxSource.volume = Mathf.Clamp01(volume * sfxVolume);
        loopSfxSource.Play();
    }
    public void StopLoopSfx()
    {
        if (loopSfxSource.isPlaying)
            loopSfxSource.Stop();
    }

    public AudioClip GetClip(ClipIndex_SFX index)
    {
        return sfxClips[(int)index];
    }
    public void PlaySfx3D(ClipIndex_SFX clipIndex, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 15f)
    {
        if ((int)clipIndex >= sfxClips.Length) return;

        AudioClip clip = sfxClips[(int)clipIndex];
        if (clip == null) return;

        GameObject tempGO = new GameObject("SFX_3D");
        tempGO.transform.position = position;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.spatialBlend = 1f; // 3D
        aSource.minDistance = minDistance;
        aSource.maxDistance = maxDistance;
        aSource.rolloffMode = AudioRolloffMode.Logarithmic;
        aSource.volume = Mathf.Clamp01(volumeScale * sfxVolume);
        aSource.Play();
        Destroy(tempGO, clip.length);
    }

    public void StopAllSfxImmediately()
    {
        // 원샷 포함, 이 소스에서 재생 중인 거 전부 끊김
        if (sfxSource != null) sfxSource.Stop();

        // 루프 SFX와 관리형 SFX도 정리
        StopLoopSfx();
        StopAllManagedSfx();
    }

}
