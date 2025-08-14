using Photon.Pun;
public class TitanItem : MonoBehaviourPun
{
    //[SerializeField] private float TitanDuration = 5f;
    //[SerializeField] private float TitanScale = 2f;
    //[SerializeField] private float TitanHealthMultiplier = 5f;
    
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.layer == (int)EnumLayer.LayerType.TeamA ||
    //        other.gameObject.layer == (int)EnumLayer.LayerType.TeamB)
    //    {
    //        PlayerHandler playerHandler = other.GetComponent<PlayerHandler>();
    //        if (playerHandler == null)
    //        {
    //            //Debug.LogWarning("TitanItem: PlayerHandler not found on the collided object.");
    //            return;
    //        }

    //        int team = other.gameObject.layer; // TeamA 또는 TeamB
    //        int viewID = playerHandler.playerPhotonView.ViewID;
    //        // 아이템 효과를 모든 클라이언트에게 알림
    //        photonView.RPC("ApplyTitanEffect", RpcTarget.All, viewID, team);

    //    }
    //}
    //[PunRPC]
    //private void ApplyTitanEffect(int targetViewID, int targetTeam)
    //{
    //    foreach (PlayerHandler player in FindObjectsOfType<PlayerHandler>())
    //    {
    //        int playerTeam = player.gameObject.layer;
    //        if (player.photonView.ViewID == targetViewID)
    //        {
    //            // 본인 : 커지고 체력 증가
    //            player.ApplyTitanEffect(TitanScale, TitanHealthMultiplier, TitanDuration);
    //        }
            
         
    //    }
    //}
   
}









