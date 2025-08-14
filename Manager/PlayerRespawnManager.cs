using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PlayerRespawnManager : MonoBehaviourPun
{
    [HideInInspector] public SpawnPointHandler spawnPointHandler;
    [HideInInspector] public PlayerHandler playerHandlerInRespawnManager;

    private string OffPlayerText = "OffPlayer";
    private string OnPlayerInvokeText = "OnPlayerInvoke";
    private string OnPlayerText = "OnPlayer";

    private bool isRespawnReady = false;

    [SerializeField] public GameObject respawnPopUp;
    public Image CurrantReSwpawnTimeBarImage;
    private float respawnTime = 5f;
    public Button respawnButton;
    
    private int currentWeaponIndex;
    private int lastWeaponIndex;
    
    private Rigidbody playerRigidbody;

    private void Start()
    {
        respawnButton.onClick.AddListener(() => OnRespawnButtonPressed());
    }

    public void SetOnDeathEvent(PlayerHandler playerHandler)
    {
        if (!playerHandler.playerPhotonView.IsMine) return;
        playerHandler.OnDeath += HandleDeath;
    }

    public void HandleDeath(PlayerHandler playerHandler)
    {
        //리스폰 코루틴 시작
        StartCoroutine(RespawnCoroutine(playerHandler));
    }
    
    private IEnumerator RespawnCoroutine(PlayerHandler playerHandler) //리스폰 코루틴
    {
        //플레이어 조작 잠금
        playerHandler.playerAction.Disable();
        
        playerRigidbody = playerHandler.GetComponent<Rigidbody>();
        playerRigidbody.velocity = Vector3.zero;
        playerHandlerInRespawnManager = playerHandler;
        
        ////Debug.Log("찾은 무기 :" + playerHandlerInRespawnManager.weapon);
        lastWeaponIndex = playerHandler.Info.WeaponSelection;
        currentWeaponIndex = playerHandler.Info.WeaponSelection;
        
        yield return (null);

        playerHandler.transform.position = spawnPointHandler.GetSpawnPoint
            (playerHandler.Info.TeamId, playerHandler.playerPhotonView.Owner.ActorNumber);

        playerHandler.transform.rotation = spawnPointHandler.GetSpawnRotation
            (playerHandler.Info.TeamId, playerHandler.playerPhotonView.Owner.ActorNumber);
        
        playerHandler.playerPhotonView.RPC(OffPlayerText, RpcTarget.All);

        // 사망 후 1초 간 게임 화면을 확인하여 전장 상황 확인 및 사인 확인할 수 있도록
        yield return new WaitForSeconds(1);
        respawnPopUp.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        
        float timer = 0f;
        if (CurrantReSwpawnTimeBarImage != null)
        {
            Vector3 scale = CurrantReSwpawnTimeBarImage.transform.localScale;
            scale.x = 0f;
            CurrantReSwpawnTimeBarImage.transform.localScale = scale;
        }
        
        while (timer < respawnTime)
        {
            timer += Time.deltaTime;

            Vector3 scale = CurrantReSwpawnTimeBarImage.transform.localScale;
            scale.x = timer/respawnTime;
            CurrantReSwpawnTimeBarImage.transform.localScale = scale;

            yield return null;
        }
        respawnButton.gameObject.SetActive(true);
        
        yield return new WaitWhile(() => !isRespawnReady);

        ChangeWeapon();
        
        //피 초기화
        float healthDeficit = playerHandler.Status.MaximumHealth - playerHandler.Status.Health;
        playerHandler.playerPhotonView.RPC(
            nameof(playerHandler.UpdateHealthByDelta), 
            playerHandler.playerPhotonView.Owner, 
            healthDeficit
            );
        
        //잉크 초기화
        playerHandler.Status.Ink = playerHandler.Status.MaximumInk;
        
        respawnButton.gameObject.SetActive(false);
        respawnPopUp.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //StatsManager.Instance.RegisterRespawn(playerHandler.Info.Nickname);

        //플레이어 조작 활성화
        playerHandler.playerAction.Enable();

        isRespawnReady = false;
        playerHandler.GetComponent<PlayerEffectHandler>().PlayRespawnAura(2f);
        SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_Respawn,2f);
        playerHandler.playerPhotonView.RPC(OnPlayerText, RpcTarget.All);
        RPCManager.Instance.photonView.RPC(nameof(RPCManager.RPC_SetAlive),RpcTarget.AllViaServer,playerHandler.Info.SequenceNumber,true);

        playerHandler.playerPhotonView.RPC(
       nameof(PlayerEffectHandler.RPC_ApplyBarrierInvincibility),RpcTarget.All,3f);
    }

    public void OnRespawnButtonPressed()
    {
        isRespawnReady = true;
        ////Debug.Log("버튼 눌림");
    }

    public void OnChangeWeaponIndex(int index)
    {
        currentWeaponIndex = index; 
    }

    private void ChangeWeapon()
    {
        if (currentWeaponIndex != lastWeaponIndex)
        {
            //무기 삭제
            playerHandlerInRespawnManager.photonView.RPC(
                nameof(playerHandlerInRespawnManager.DestroyWeapon),
                RpcTarget.All
                );
            //무기 프리팹 선택 최신화(이러면 로컬만 되네; 나중에 문제 될수도?)
            playerHandlerInRespawnManager.Info.WeaponSelection = currentWeaponIndex;
            //무기 생성
            playerHandlerInRespawnManager.photonView.RPC(
                    nameof(playerHandlerInRespawnManager.EquipWeapon), 
                    RpcTarget.All, 
                    playerHandlerInRespawnManager.Info.WeaponSelection
                    );

            //무기 이름 업데이트- 플레이어 statusUI에 반영
            // string newWeaponName = DataManager.Instance.WeaponPrefabs[currentWeaponIndex];
            RPCManager.Instance.photonView.RPC(
                nameof(RPCManager.RPC_UpdatePlayerWeapon),
                RpcTarget.AllViaServer,
                playerHandlerInRespawnManager.Info.SequenceNumber,
                currentWeaponIndex
            );
        }
    }
}