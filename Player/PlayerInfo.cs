using System;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class PlayerInfo
{
    public int SequenceNumber;
    public string PlayerId;
    public string Nickname;
    public byte TeamId;
    public byte TeamColorA;
    public byte TeamColorB;
    public int CharacterSelection;
    public int WeaponSelection;

    public Hashtable ToHashtable() => new Hashtable
    {
        { CustomPropKey.Sequence,           SequenceNumber },
        { CustomPropKey.PlayerId,           PlayerId },
        { CustomPropKey.Nickname,           Nickname },
        { CustomPropKey.TeamId,             TeamId },
        { CustomPropKey.TeamColorA,          TeamColorA },
         { CustomPropKey.TeamColorB,          TeamColorB },
        { CustomPropKey.CharacterSelection, CharacterSelection },
        { CustomPropKey.WeaponSelection,    WeaponSelection },
        { CustomPropKey.IsReady,            false },
        { CustomPropKey.InGameFlag,         false },
        { CustomPropKey.InGameStartReady,         false }
    };

    public static PlayerInfo FromHashtable(Hashtable ht) => new PlayerInfo
    {
        SequenceNumber = (int)ht[CustomPropKey.Sequence],
        PlayerId = ht[CustomPropKey.PlayerId] as string,
        Nickname = ht[CustomPropKey.Nickname] as string,
        TeamId = (byte)ht[CustomPropKey.TeamId],
        TeamColorA = (byte)ht[CustomPropKey.TeamColorA],
        CharacterSelection = (int)ht[CustomPropKey.CharacterSelection],
        WeaponSelection = (int)ht[CustomPropKey.WeaponSelection]
    };
}