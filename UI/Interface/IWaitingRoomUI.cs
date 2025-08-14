using System;
using System.Collections.Generic;
using Photon.Realtime;

public interface IWaitingRoomUI
{
    event Action<bool> OnReadyToggled;
    event Action OnStartMatchClicked;

    event Action<Map> OnMapChanged;
    event Action<Player> OnKickPlayer;

    void ChangeHost(bool isActive);
    void UpdatePlayerListDisplay(List<Player> players);
    void SetReadyButtonActive(bool isActive);
    void SetMap(Map map);
    bool MapDropdownInteractable { set; }

    void LockForMatchStart();
}