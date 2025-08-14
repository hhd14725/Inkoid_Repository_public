using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, 0);
    public Vector3 localoffset = new Vector3(0, 0, 0);
    public Image fillImage;

    public void Init(Transform _target)
    {
        target = _target;
        var pv = target.GetComponentInParent<PhotonView>();
        //Debug.Log($"[HealthBar] Init | target: {target.name}, IsMine: {pv.IsMine}");

        if (pv.IsMine)
            gameObject.SetActive(false); // 내 체력바는 꺼짐
        else
            gameObject.SetActive(true);  // 상대는 켜짐
    }

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.forward = Camera.main.transform.forward;
        }
    }

    public void SetHealth(float current, float max)
    {
        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
