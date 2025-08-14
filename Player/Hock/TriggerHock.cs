using System;
using Photon.Pun;
using UnityEngine;

public class TriggerHock : MonoBehaviourPun
{
    public event Action OnHookTriggered;
    
    int layerMask = (1 << (int)EnumLayer.LayerType.HockBackTriggered);

    private void Start()
    {
        if (photonView.IsMine) return;
        
        transform.gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & layerMask) != 0)
        {
            //OnHookTriggered?.Invoke();
            ////Debug.Log("훅 돌아가기");
        }
    }
}