using Photon.Pun;

public class EnemySmall : MonoBehaviourPun
{
    //[SerializeField] private float SmallScaleDuration = 5f;
    //[SerializeField] private float SmallScale = 0.5f;
    //[SerializeField] private float SmallHealthMultiplier = 0.5f;
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.layer == (int)EnumLayer.LayerType.TeamA ||
    //        other.gameObject.layer == (int)EnumLayer.LayerType.TeamB)
    //    {
    //        PlayerHandler playerHandler = other.GetComponent<PlayerHandler>();
    //        if (playerHandler == null) return;
    //        int team = other.gameObject.layer; // 먹은 사람 팀
    //        photonView.RPC(nameof(ApplyDebuff), RpcTarget.All, team, 0.5f , 20f , 5f);
    //    }
    //}
    //[PunRPC]
    //private void ApplyDebuff(int ownerTeamLayer, float scaleMultiplier, float damage, float duration)
    //{
    //    foreach (PlayerHandler player in FindObjectsOfType<PlayerHandler>())
    //    {
    //        if (player.gameObject.layer != ownerTeamLayer)
    //        {
    //            player.ApplyDebuffEffect(scaleMultiplier, damage, duration);
    //        }
    //    }
    //}
}
