using Photon.Pun;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private ITitleUI titleUI;
    private ILobbyUI lobbyUI;
    private IWaitingRoomUI waitingRoomUI;
    private IGameHUDUI gameHUDUI;
    private GameTimer gameTimer;
    private AudioClip defaultClickSound;
    private float defaultVolume = 1f;
    public IGameHUDUI GameHUDUI { get => gameHUDUI; }
    //void Start()
    //{
    //    RegisterButtonSounds();
    //}
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    //private void RegisterButtonSounds()
    //{
    //    Button[] buttons = FindObjectsOfType<Button>(true); // 비활성 버튼도 포함

    //    foreach (Button button in buttons)
    //    {
    //        button.onClick.AddListener(() =>
    //        {
    //            var sound = button.GetComponent<ButtonClickSound>();
    //            if (sound != null && sound.overrideSound != null)
    //                SoundManager.Instance.PlaySfx(sound.overrideSound, sound.volume);
    //            else
    //                SoundManager.Instance.PlaySfx(defaultClickSound, defaultVolume);
    //        });
    //    }
    //}
    public void RegisterTitleUI(ITitleUI ui, TitleManager manager)
    {
        titleUI = ui;
        titleUI.OnConnectClicked += manager.HandleConnectClicked;
        manager.OnConnectResult += titleUI.SetConnectButtonEnabled;
    }

    public void RegisterLobbyUI(ILobbyUI ui, LobbyManager manager)
    {
        lobbyUI = ui;
        lobbyUI.OnCreateRoomClicked += manager.HandleCreateRoom;
        lobbyUI.OnJoinRoomClicked += manager.HandleJoinRoom;
        manager.OnLobbyEntering += () => lobbyUI.SetLobbyButtonsInteractable(false, false);
        manager.OnLobbyEntered += () => lobbyUI.SetLobbyButtonsInteractable(true, true);
        manager.OnRoomListUpdated += lobbyUI.DisplayRoomList;
    }

    public void RegisterWaitingRoomUI(IWaitingRoomUI ui, RoomManager manager)
    {
        if (waitingRoomUI != null)
        {
            manager.OnStartButtonVisible -= waitingRoomUI.ChangeHost;
            manager.OnPlayerListChanged -= waitingRoomUI.UpdatePlayerListDisplay;
            waitingRoomUI.OnKickPlayer -= manager.HandleKickPlayer;
            waitingRoomUI.OnReadyToggled -= manager.HandleReadyToggled;
            waitingRoomUI.OnStartMatchClicked -= manager.HandleStartMatchClicked;
            waitingRoomUI.OnMapChanged -= manager.HandleMapChanged;
        }
           

        waitingRoomUI = ui;
        waitingRoomUI.OnReadyToggled += manager.HandleReadyToggled;
        waitingRoomUI.OnStartMatchClicked += manager.HandleStartMatchClicked;
        waitingRoomUI.OnMapChanged += manager.HandleMapChanged;
        waitingRoomUI.OnKickPlayer += manager.HandleKickPlayer;

        manager.OnPlayerListChanged += waitingRoomUI.UpdatePlayerListDisplay;
        manager.OnStartButtonVisible += waitingRoomUI.ChangeHost;
    }

    public void RegisterGameHUDUI(IGameHUDUI ui, GameTimer timer)
    {
        gameHUDUI = ui;
        gameTimer = timer;
        timer.OnTimeChanged += ui.UpdateTimeDisplay;
        timer.OnTimerEnded += () =>
        {
            if (PhotonNetwork.IsMasterClient)
            {
                RPCManager.Instance.photonView_RPCManager.RPC(
                    nameof(RPCManager.RPC_ShowEndPopupAll),
                    RpcTarget.AllViaServer
                );
            }
        };
    }
}