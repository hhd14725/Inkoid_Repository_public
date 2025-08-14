using UnityEngine;

public class Emoticon : MonoBehaviour
{
    public float showEmoteTime = 2f;

    // 켜질 때 다른 이모티콘 비활성화
    private void OnEnable()
    {
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform childTmp = transform.parent.GetChild(i);
            if (childTmp != transform)
                childTmp.gameObject.SetActive(false);
        }

        // 비활성 예약
        Invoke("DisableAfterShowTime", showEmoteTime);
    }

    void DisableAfterShowTime()
    {
        gameObject.SetActive(false);
    }
}
