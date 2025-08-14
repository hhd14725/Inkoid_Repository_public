using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class WaitingRoomChangeWeaponUI : MonoBehaviourPun
{
    //[SerializeField]private TMP_Text selectWeaponText;
    public WaitingRoomChangeWeaponUIButtons waitingRoomChangeWeaponUIButtons;
    public Action<Player, int> OnWeaponChangeAction;

    [SerializeField] private TMP_Text WeaponGuideText;
    [SerializeField] [TextArea] private string[] weaponDescriptions = new string[4];



    private void Awake()
    {

        //버튼 가져오기
        waitingRoomChangeWeaponUIButtons = GetComponentInChildren<WaitingRoomChangeWeaponUIButtons>();
    }

    private IEnumerator Start()
    {
        //그래서 좀 늦춤
        yield return new WaitForSeconds(0.5f);
        // 제일 기본 총을 기본으로 고른 상태

        // 기본 총 선택으로 초기화
        OnWeaponChange(0);
        waitingRoomChangeWeaponUIButtons.buttons[0].Select();


        //첨에 설정이 안 되있네 
        //if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CustomPropKey.WeaponSelection, out object indexObj))
        //{
        //    int weaponIndex = (int)indexObj;
        //    //selectWeaponText.text = DataManager.Instance.WeaponPrefabs[weaponIndex].name;
        //}
        //else
        //{
        //    // 기본 무기 선택 일단 기본 무기로 적어두자
        //    //selectWeaponText.text = DataManager.Instance.WeaponPrefabs[0].name;
        //}
    }

    public void OnWeaponChange(int index)
    {
        //selectWeaponText.text = DataManager.Instance.WeaponPrefabs[index].name;
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable
        {
            { CustomPropKey.WeaponSelection, index }
        };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            var player = PhotonNetwork.LocalPlayer;
            OnWeaponChangeAction?.Invoke(player, index);

        }
        // 무기 설명 텍스트 변경
        if (WeaponGuideText != null && index >= 0 && index < weaponDescriptions.Length)
        {
            WeaponGuideText.text = weaponDescriptions[index];
        }
    }
}
