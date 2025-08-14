public interface IGameHUDUI
{
    // 남은 시간이 바뀔 때마다 호출
    void UpdateTimeDisplay(int remainingSeconds);

    // 타이머가 끝났을 때 호출
    void ShowEndPopup();
    void UpdateCaptureUI(int bluePoints, int neutralPoints, int redPoints);

    void UpdatePlayerWeapon(int playerID, int weaponIndex);

    void AddKillFeedEntry(int killerID, int victimID);

    void SetPlayerAlive(int playerID, bool alive);
}