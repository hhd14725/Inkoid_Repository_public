using UnityEngine;
using TMPro;

public class GroupResultEntryUI : MonoBehaviour
{
    [SerializeField] TMP_Text groupNameText;
    [SerializeField] TMP_Text percentText;
    [SerializeField, Header("Icons")] InkoidFace[] inkoidIcons;
    private void Start()
    {
        // 팀 색상 부여
        for (int i = 0; i < inkoidIcons.Length; i++)
        {
            inkoidIcons[i].SetColor(i);
        }
    }

    //percentA, percentB: 0~1 사이. 
    //winner: 0 = Blue, 1 = Red, 255 = Draw.
    public void SetData(string groupName, float percentA, float percentB, byte winner)
    {
        groupNameText.text = groupName;

        int aPct = Mathf.RoundToInt(percentA * 100f);
        int bPct = Mathf.RoundToInt(percentB * 100f);

        if (winner == 0 || winner == 1)
        {
            // 승리/패배 팀에 따라 아이콘 표정 변화
            if (winner == 0)
            {
                inkoidIcons[0].SetFace(FaceSort.Smile);
                inkoidIcons[1].SetFace(FaceSort.Sad);
            }
            else if (winner == 1)
            {
                inkoidIcons[0].SetFace(FaceSort.Sad);
                inkoidIcons[1].SetFace(FaceSort.Smile);
            }

            // 각 팀 점유율 색상을 팀 색상으로 변화
                string teamAColorHex = ColorToHex(DataManager.Instance.colorTeamA),
                       teamBColorHex = ColorToHex(DataManager.Instance.colorTeamB);
            percentText.text = $"<color={teamAColorHex}>{aPct}%</color> VS <color={teamBColorHex}>{bPct}%</color>";
        }
        else
            percentText.text = $"{aPct}% / {bPct}% (Draw)";
    }

    // RGBA 색상 타입 > 16진수("#00000000") 색상 타입으로 변환
    string ColorToHex(Color color)
    {
        Color32 c = color;
        return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
    }
}
