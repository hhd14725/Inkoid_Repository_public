using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using System;
using Photon.Pun;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TitleUI titleUI;
    public event Action<bool> OnConnectResult;

    void OnEnable()
    {
        UIManager.Instance.RegisterTitleUI(titleUI, this);
        SteamAuthManager.Instance.OnAuthenticated += OnSteamAuthenticated;
        NetworkManager.Instance.OnConnectedEvent += OnMasterConnected;
        NetworkManager.Instance.OnDisconnectedEvent += OnMasterDisconnected;
    }

    void OnDisable()
    {
        SteamAuthManager.Instance.OnAuthenticated -= OnSteamAuthenticated;
        NetworkManager.Instance.OnConnectedEvent -= OnMasterConnected;
        NetworkManager.Instance.OnDisconnectedEvent -= OnMasterDisconnected;
    }

    public void HandleConnectClicked(string nickname)
    {
        OnConnectResult(false);
        PlayerPrefs.SetString("PlayerNickname", nickname);
        PlayerPrefs.Save();
        PhotonNetwork.NickName = nickname;
        SteamAuthManager.Instance.Authenticate();
    }

    private void OnSteamAuthenticated(string steamId, string authTicketHex)
        => NetworkManager.Instance.ConnectToMaster(steamId, authTicketHex);

    private void OnMasterConnected()
        => StartCoroutine(WaitAndChangeScene());

    IEnumerator WaitAndChangeScene()
    {
        yield return new WaitUntil(() => titleUI.isProducionEnd);
        SceneManager.LoadScene("Lobby");
    }

    private void OnMasterDisconnected(DisconnectCause cause)
    {
        //Debug.LogWarning($"서버 연결 해제: {cause}");
        OnConnectResult(true);
    }
}