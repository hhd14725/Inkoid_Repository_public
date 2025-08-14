using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using System;
using System.Collections.Generic;

public class StatsManager : MonoBehaviourPun
{
    public static StatsManager Instance { get; private set; }
    // 런타임 중 집계되는 값들
    Dictionary<int, ResultInfo> _current = new Dictionary<int, ResultInfo>();

    // 킬 발생시 흘려줄 이벤트
    public event Action<string, string> OnKillEvent;
    // 종료 시점에 Freeze된 결과
    List<ResultInfo> _frozen;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else Destroy(gameObject);
    }

    //킬을 등록(킬러 닉네임)
   
    public void RegisterKill(int killerID, int victimID)
    {
        var info = GetOrCreate(killerID);
        info.KillCount++;
        var infoV = GetOrCreate(victimID);
        infoV.DeathCount++;
    }
 

    //리스폰(죽음→부활) 등록(부활한플레이어 닉네임)
    //public void RegisterRespawn(string playerName)
    //{
    //    var info = GetOrCreate(playerName);
    //    info.RespawnCount++;
    //}

    public ResultInfo GetOrCreate(int photonViewID)
    {
        if (!_current.TryGetValue(photonViewID, out var info))
        {
            // 처음 보는 플레이어라면, PlayerHandler에서 팀을 찾아올 수도 있고
            byte team = 0;
            string nickName = "";
            // PhotonNetwork.PlayerList: 현재 방의 모든 Player 객체
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == photonViewID)
                {
                    p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out team);
                    p.CustomProperties.TryGetValue(CustomPropKey.Nickname, out object nick);
                    nickName = nick.ToString();
                    break;
                }
            }

            info = new ResultInfo
            {
                Nickname = nickName,
                Team = team,
                KillCount = 0,
                DeathCount = 0
            };
            _current[photonViewID] = info;
        }
        return info;
    }

    // string이 아닌 int 값으로 찾으면서 닉네임을 찾을 필요가 있음
    // > GetOrCreate()에서 포톤뷰ID로부터 닉네임, 팀을 함께 찾게 함으로써 해당 메서드의 기능을 통합 (7/31 유빈)
    //byte GetPlayerTeam(int photonViewID)
    //{
    //    // PhotonNetwork.PlayerList: 현재 방의 모든 Player 객체
    //    foreach (var p in PhotonNetwork.PlayerList)
    //    {
    //        if (p.ActorNumber == photonViewID
    //         && p.CustomProperties.TryGetValue(CustomPropKey.TeamId, out object t))
    //        {
    //            return (byte)t;
    //        }
    //    }
    //    // 못 찾으면 기본 0
    //    return 0;
    //}

    //종료 시점에 통계 값을 동결

    public void FreezeStats()
    {
        _frozen = new List<ResultInfo>(_current.Values);
    }

    //동결된 결과 리스트를 반환 (없으면 빈 리스트)
    public List<ResultInfo> GetFrozenResults()
        => _frozen ?? new List<ResultInfo>();
}
