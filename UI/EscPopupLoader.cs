using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class EscPopupLoader : MonoBehaviourPun
{
    [SerializeField] SettingUI settingPopupPrefab;
    private InputAction escAction;
    private PlayerHandler localPlayerHandler;

    void Awake()
    {
        settingPopupPrefab.gameObject.SetActive(true);
        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ => Toggle();
    }

    private void Start()
    {
        settingPopupPrefab.FovScreenOpen();
        settingPopupPrefab.EachScreenOpen();
        settingPopupPrefab.gameObject.SetActive(false);

        //로컬 플레이어 누군지 찾기
        if (GameManager.Instance.players.TryGetValue
                (PhotonNetwork.LocalPlayer.ActorNumber, out PlayerHandler localPlayer))
        {
            localPlayerHandler = localPlayer;
        }
        //else
        //{
        //    //Debug.LogWarning("로컬 플레이어 못 찾음");
        //}
    }

    void OnEnable()
    {
        escAction.Enable();
    }

    void OnDestroy()
    {
        escAction.Disable();
    }

    public void Toggle()
    {
        if (!settingPopupPrefab.gameObject.activeSelf)
            //로컬 플레이어 누군지 찾기(스타스에서 찾고 들어가고 싶은데 왠지 모르게 안 찾아짐... 왤까)(창민)
            if (GameManager.Instance.players.TryGetValue
                    (PhotonNetwork.LocalPlayer.ActorNumber, out PlayerHandler localPlayer))
            {
                localPlayerHandler = localPlayer;
            }
            else
            {
                //Debug.LogWarning("로컬 플레이어 못 찾음");
            }

        if (!settingPopupPrefab.gameObject.activeSelf && GameManager.Instance.isGamePlaying)
        {
            settingPopupPrefab.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //플레이어 조작 끄기
            localPlayerHandler.playerAction.Disable();
        }
        else
        {
            settingPopupPrefab.gameObject.SetActive(false);

            if (!GameManager.Instance.playerRespawnManager.respawnPopUp.gameObject.activeInHierarchy
                && !GameManager.Instance.hudUI.resultPopup.gameObject.activeInHierarchy
                && GameManager.Instance.isGamePlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                //플레이어 조작 켜기
                localPlayerHandler.playerAction.Enable();
            }
        }
    }
    // 미사용으로 인한 주석 처리
    //public void ForceClosePopup()
    //{
    //    if (settingPopupPrefab.activeSelf)
    //    {
    //        settingPopupPrefab.SetActive(false);
    //        var current = SceneManager.GetActiveScene().name;
    //        if (Enum.TryParse<Map>(current, out _)
    //            && !GameManager.Instance.playerRespawnManager.respawnPopUp.gameObject.activeInHierarchy)
    //        {
    //            Cursor.lockState = CursorLockMode.Locked;
    //            Cursor.visible = false;
    //        }
    //    }
    //}
}