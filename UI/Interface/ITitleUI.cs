using System;

public interface ITitleUI
{
    event Action<string> OnConnectClicked;
    void SetConnectButtonEnabled(bool enabled);
}