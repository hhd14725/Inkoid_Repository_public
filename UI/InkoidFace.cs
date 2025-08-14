using UnityEngine;
using UnityEngine.UI;

public class InkoidFace : MonoBehaviour
{
    [SerializeField] Image frame, face;

    // 팀 색상에 맞추어 색상 변경
    public void SetColor(int teamID)
    {
        if (teamID == 0)
        {
            frame.color = DataManager.Instance.colorTeamA;
            //face.color = DataManager.Instance.colorTeamA_Darker;
        }
        else if (teamID == 1)
        {
            frame.color = DataManager.Instance.colorTeamB;
            //face.color = DataManager.Instance.colorTeamB_Darker;
        }  
    }

    // 표정 변화
    public void SetFace(FaceSort faceSort)
    {
        face.sprite = DataManager.Instance.inkoidFaces[(int)faceSort];
    }
}
