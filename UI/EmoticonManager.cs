using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class EmoticonManager : MonoBehaviourPunCallbacks
{
    [SerializeField] WaitingRoomUI waitingRoomUI;
    [SerializeField] Transform EmoticonPanel;
    // public float showEmoteTime = 2f;

    const string emoticonHolderName = "EmoticonHolder";
    Button[] emoticonButtons;
    private void Start()
    {
        emoticonButtons = EmoticonPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < emoticonButtons.Length; i++)
        {
            int tmpIndex = i;
            emoticonButtons[i].onClick.AddListener(() => OnEmoteButtonClicked(tmpIndex));
        }
    }
    public override void OnDisable()
    {
        base.OnDisable();
        for (int i = 0; i < emoticonButtons.Length; i++)
        {
            int tmpIndex = i;
            emoticonButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void OnEmoteButtonClicked(int emoteIndex)
    {
        // 플레이어 리스트에서 자신이 몇번째인지
        int playerIndex = 0;
        foreach(var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal)
            {
                break;
            }
            playerIndex++;
        }

        // 자신의 패널에 맞게 이모티콘 호출
        try
        {
            if(photonView != null)
                photonView.RPC(nameof(CallEmote_RPC), RpcTarget.All, playerIndex, emoteIndex);
        }
        catch { }
    }

    [PunRPC]
    void CallEmote_RPC(int entryIndex, int emoteIndex)
    {
        if (waitingRoomUI == null || waitingRoomUI.playerListParent == null)
            return;

        int childCount = waitingRoomUI.playerListParent.childCount;
        if (entryIndex < 0 || entryIndex >= childCount)
            return; // 이미 대상 플레이어가 삭제됨

        Transform playerSlot = waitingRoomUI.playerListParent.GetChild(entryIndex);
        if (playerSlot == null)
            return;

        Transform emoticonHolder = playerSlot.Find(emoticonHolderName);
        if (emoticonHolder == null || emoteIndex < 0 || emoteIndex >= emoticonHolder.childCount)
            return; // 해당 홀더나 이모티콘 없음

        // waitingRoomUI 안의 playerListParent에 접근해야 해
        GameObject preEmoticonObj = emoticonHolder.GetChild(emoteIndex).gameObject;
        if (preEmoticonObj != null)
        {
            preEmoticonObj.SetActive(true);
        }

        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Click);
    }
}
