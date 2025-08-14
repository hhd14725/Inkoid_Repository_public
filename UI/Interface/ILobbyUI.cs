using System;
using System.Collections.Generic;
using Photon.Realtime;

public interface ILobbyUI
{
    event Action<string, byte, int, Map> OnCreateRoomClicked;
    event Action<string> OnJoinRoomClicked;
    void DisplayRoomList(List<RoomInfo> rooms);
    void SetLobbyButtonsInteractable(bool canCreate, bool canRefresh);
}