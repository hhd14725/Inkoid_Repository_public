public static class CustomPropKey //커스텀프로퍼티용 string key 관리, 필요하면 추가.
{
    public const string Sequence = "Seq"; // 포톤 액터넘버 int 1부터.
    public const string IsReady = "IsReady"; // 플레이어 준비 상태 bool.
    public const string TeamId = "TeamID"; // 팀 아이디 byte.
    public const string TeamColorA = "TeamColorA";
    public const string TeamColorB = "TeamColorB";
    public const string PlayerId = "PlayerID"; // 플레이어 아이디 string, 스팀인증시 얻어오는 steamID.
    public const string Nickname = "Nickname";// 플레이어 닉네임 string , playerprefs로 저장해서 photonnetwork.NickName에 저장됨.
    public const string InGameFlag = "InGame"; // bool, 게임중인지 여부.
    public const string CharacterSelection = "CharacterIndex"; // 캐릭터 선택 인덱스 int.
    public const string WeaponSelection = "WeaponIndex"; // 무기 선택 인덱스 int.
    public const string MatchTimeSeconds = "TimeLimitSeconds"; // 매치 시간 제한 초 단위 int.
    public const string InGameStartReady = "InGameStartReady"; // bool 인게임들어가서 준비 상태
    public const string MapName = "MapName"; // 맵 선택 이름, enum int -> string 파싱
    public const string IsHost = "IsHost"; // bool, 방장 여부
    public const string MatchStartingLock = "MatchStartingLock"; //bool, 매치 시작시 잠금여부
    public const string UseImoticon = "UseImoticon"; // bool, 이모티콘 사용 여부
    public const string IsTutorial = "IsTutorial"; // bool, 튜토리얼 여부
    public const string ReturnToWaitingRoomTime = "ReturnToWaitingRoomTime";//networkManager가 CreateRoom 할 때 부터 생성하는 int값, 게임 끝나고 돌아가는 시간 동기화 하는데 사용  
    public const string PreMatchStartTime = "PreMatchStartTime"; // 게임씬 스프라이트 카운트다운 시작 시각 (PhotonNetwork.Time)
    public const string MatchStartTime = "MatchStartTime";    // 카운트다운 끝, 실제 게임 시작 시각 (T0)
}