using System;
using System.Collections;
using UnityEngine;

public class GodMode : MonoBehaviour
{
    [SerializeField] private float GodDuration = 5f;   // 효과 지속 시간
   
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == (int)EnumLayer.LayerType.TeamA || other.gameObject.layer == (int)EnumLayer.LayerType.TeamB)
        {
            PlayerHandler playerHandler = other.GetComponent<PlayerHandler>();

            float baseHp = playerHandler.Status.Health;


            void HealBack(float hp)
            {
                if (hp < baseHp)                       // 기준선보다 줄었으면
                    playerHandler.UpdateHealthByDelta(baseHp - hp);
                
            }
            //playerHandler.OnHealthChanged += HealBack;

            StartCoroutine(GodModeRoutine(playerHandler, HealBack));
        }
    }
    private IEnumerator GodModeRoutine(PlayerHandler playerHandler, Action<float> healBack)
    {
        yield return new WaitForSeconds(GodDuration);
        //playerHandler.OnHealthChanged -= healBack;

    }
}
