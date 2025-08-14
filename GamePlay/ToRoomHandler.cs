using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class ToRoomHandler : MonoBehaviourPunCallbacks
{
    [Tooltip("씬에 하나뿐인 GameHUDUI 오브젝트 연결")]
    [SerializeField] private GameHUDUI gameHUDUI;

    [Header("time for AutoReturn")]
    public int autoReturnDelay = 10;
    bool started = false;

    private bool InRoom => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        //Debug.Log("[ToRoomHandler] 컴포넌트 활성화 → 콜백 구독 시작");
        if (gameHUDUI != null)
            gameHUDUI.OnResultPopupShown += StartAutoReturn;

        //게임 재시작 시 대비해서 돌아가는 시간 초기화
        if (InRoom)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(
                new Hashtable { { CustomPropKey.ReturnToWaitingRoomTime, 10 } });
        }
        //else
        //{
        //    Debug.LogWarning("[ToRoomHandler] OnEnable 시점에 아직 방이 없음. 초기값 기록 생략.");
        //}
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        //Debug.Log("[ToRoomHandler] 컴포넌트 비활성화 → 콜백 구독 해제");
        if (gameHUDUI != null)
            gameHUDUI.OnResultPopupShown -= StartAutoReturn;
    }

    void StartAutoReturn()
    {
        if (!PhotonNetwork.IsMasterClient || started) return;
        started = true;
        StartCoroutine(AutoReturnRoutine());
    }

    IEnumerator AutoReturnRoutine()
    {
        //이미 지나간 시간이 있으면 넣어주기
        if (InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue
            (CustomPropKey.ReturnToWaitingRoomTime, out object inGameValue))
        {
            autoReturnDelay =  (int)inGameValue;
        }
        
        int timer = autoReturnDelay;
        while (timer > 0)
        {
            if (!InRoom)
            {
                //Debug.LogWarning("[ToRoomHandler] 방을 떠남(또는 아직 없음). 카운트다운 중단.");
                yield break;
            }

            // UI에 남은 시간 표시
            photonView.RPC(
              nameof(RPC_UpdateReturnCountdown),
              RpcTarget.All,
              timer);

            yield return new WaitForSeconds(1f); //1초씩 쉬어줌
            timer--;

            //지나간 시간 저장하기
            if (InRoom)
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(
                    new Hashtable { { CustomPropKey.ReturnToWaitingRoomTime, timer } });
            }
            else
            {
                //Debug.LogWarning("[ToRoomHandler] 기록 시점에 방이 없음. 카운트다운 중단.");
                yield break;
            }
        }
        photonView.RPC(
       nameof(RPC_UpdateReturnCountdown),
       RpcTarget.All,
       0);

        if (InRoom)
        {
            // 게임 종료 표시
            PhotonNetwork.CurrentRoom.SetCustomProperties(
                new Hashtable { { CustomPropKey.InGameFlag, false } });
        }

        if (gameHUDUI.ResultPopup != null)
            gameHUDUI.ResultPopup.SetActive(false);
        
        gameHUDUI.isPopupActive = false;
        
        //마우스 커서 다 켜지게 rpc 호출
        photonView.RPC(nameof(RPC_MouseCurserVisible), RpcTarget.All);
        
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.DestroyAll();
        // 호스트가 씬 전환 호출 → AutomaticallySyncScene=true 면 나머지도 같이 넘어갑니다
        PhotonNetwork.LoadLevel("WaitingRoom");
    }

    [PunRPC]
    void RPC_UpdateReturnCountdown(int secondsLeft)
    {
        gameHUDUI.SetReturnCountdown(secondsLeft);
    }

    [PunRPC]
    private void RPC_MouseCurserVisible()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }

    public override void OnMasterClientSwitched(Player newmasterClient)
    {
        //Debug.Log($"[ToRoomHandler] 새로운 호스트: {newmasterClient.NickName}, isPopupActive={gameHUDUI.isPopupActive}, started={started}");
        if (PhotonNetwork.IsMasterClient && gameHUDUI.isPopupActive && !started)
        {
            StartAutoReturn();
        }
    }
}