using UnityEngine;
using TMPro;

public class KillLogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public void SetData(string killer, string victim)
    {
        text.text = $"{killer} 처치 {victim}";
        // 3초 뒤 자동 제거
        Destroy(gameObject, 3f);
    }
}
