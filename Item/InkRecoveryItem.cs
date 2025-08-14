using System.Collections;
using UnityEngine;


public class InkRecoveryItem : MonoBehaviour
{
    [SerializeField] private float boostDuration = 5f;   // 효과 지속 시간
    
    



    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.layer == (int)EnumLayer.LayerType.TeamA || other.gameObject.layer == (int)EnumLayer.LayerType.TeamB)
        {
            PlayerInkHandler playerInkHandler = other.GetComponent<PlayerInkHandler>();

            // 잉크를 최대치로 회복
            playerInkHandler.Ink = playerInkHandler.maximumInk;

            // 헨들러에 있는 거 받아옴
            InkEvents.OnInkConsumed -= playerInkHandler.ConsumeInk;

            // 5초 뒤 복구
            StartCoroutine(ResetAfterDelay(playerInkHandler));
        }
    }

    private IEnumerator ResetAfterDelay(PlayerInkHandler playerInkHandler)
    {
        
        yield return new WaitForSeconds(boostDuration);
        InkEvents.OnInkConsumed += playerInkHandler.ConsumeInk;
    }
}
