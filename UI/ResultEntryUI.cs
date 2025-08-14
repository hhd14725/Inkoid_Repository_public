using UnityEngine;
using TMPro;

public class ResultEntryUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text kdText;


    // 호출 순서: Instantiate 한 뒤 바로 SetData(info).

    public void SetData(ResultInfo info)
    {
        nameText.text = info.Nickname;
        kdText.text = $"K {info.KillCount}  /  D {info.DeathCount}";
    }
}
