using UnityEngine;
using System;
#if USE_STEAM
using Steamworks;
#endif

public class SteamAuthManager : MonoBehaviour, IAuthService
{
    public static SteamAuthManager Instance { get; private set; }
    public event Action<string, string> OnAuthenticated;

#if USE_STEAM
    private HAuthTicket steamAuthTicket;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }

        if (!SteamAPI.Init())
            //Debug.LogError("Steam API 초기화 실패");
    }

    void Update() => SteamAPI.RunCallbacks();
    void OnApplicationQuit() => SteamAPI.Shutdown();

    public void Authenticate()
    {
        byte[] buffer = new byte[1024];
        uint size;
        steamAuthTicket = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out size);
        if (steamAuthTicket == HAuthTicket.Invalid)
        {
            //Debug.LogError("Steam 인증 티켓 발급 실패");
            return;
        }

        byte[] ticket = new byte[size];
        Array.Copy(buffer, ticket, size);

        var sb = new System.Text.StringBuilder((int)size * 2);
        foreach (var b in ticket)
            sb.AppendFormat("{0:x2}", b);

        OnAuthenticated?.Invoke(
            SteamUser.GetSteamID().ToString(),
            sb.ToString()
        );
    }

    public void CancelAuthentication()
    {
        if (steamAuthTicket != HAuthTicket.Invalid)
            SteamUser.CancelAuthTicket(steamAuthTicket);
        steamAuthTicket = HAuthTicket.Invalid;
    }
#else
    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    public void Authenticate()
    {
        // 스텁용: 프로세스별 고유 GUID를 생성·저장해서
        // ParrelSync 복제 간 충돌을 방지합니다.
        string localId = Guid.NewGuid().ToString();
        //Debug.Log($"[Stub] Steam 인증 호출, LocalId = {localId}");

        OnAuthenticated?.Invoke(localId, "LOCAL_TICKET_HEX");
    }

    public void CancelAuthentication() { }
#endif
}
