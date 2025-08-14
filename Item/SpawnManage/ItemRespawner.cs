using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemRespawner : MonoBehaviourPun
{
    private int spawnPointIndex;
    private ItemCategory itemCategory;


    // SpawnManager 에서 생성 직후 호출해서 세팅.
 
    public void Initialize(int spawnPointIndex, ItemCategory itemCategory)
    {
        this.spawnPointIndex = spawnPointIndex;
        this.itemCategory = itemCategory;
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;
        bool isPlayerLayer = layer == (int)EnumLayer.LayerType.TeamA
                          || layer == (int)EnumLayer.LayerType.TeamB;
        if (!isPlayerLayer) return;

        PhotonView playerPhotonView = other.GetComponent<PhotonView>();
        if (playerPhotonView == null) return;

        // MasterClient 에 픽업 요청 RPC
        ItemSpawnManager.Instance.photonView.RPC(
            nameof(ItemSpawnManager.HandlePickupOnMaster),
            RpcTarget.MasterClient,
            spawnPointIndex,
            photonView.ViewID,
            playerPhotonView.ViewID,
            (int)itemCategory
        );
    }
}
