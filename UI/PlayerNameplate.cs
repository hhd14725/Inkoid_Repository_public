using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class PlayerNameplate : MonoBehaviourPun
{
    [SerializeField] private TMP_Text nameText;
    

    
    void Start()
    {
        if (photonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        } 

        // 각 클라이언트에 동기화된 플레이어 닉네임
        nameText.text = photonView.Owner.NickName;
        // 플레이어가 사용하는 효과
        
    }
    
    void LateUpdate()
    {
        // 카메라를 따라오게
        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
