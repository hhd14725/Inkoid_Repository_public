using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    // 게임 씬 중 플레이어 표시 테두리 추가 및 색상 표시로 인한 수정 (7/26 유빈)
    [SerializeField] private Image background, frame, weaponImage;
    [SerializeField] private TMP_Text nameText;

    Color teamColor,teamColorDark;
    string deadDarkGrayCode = "#292929";
    private Color deadGray = Color.gray, deadDarkGray;
    private Color defaultColor = Color.white;


    public void SetData(string nick, int weaponIndex, byte teamId)
    {
        // Hex 색상 코드로부터 진한 회색 색상으로 변환
        ColorUtility.TryParseHtmlString(deadDarkGrayCode, out deadDarkGray);
        nameText.text = nick;
        weaponImage.sprite = DataManager.Instance.WeaponImages_GameScene[weaponIndex];
        teamColor = (teamId == 0)
       ? DataManager.Instance.colorTeamA
       : DataManager.Instance.colorTeamB;
        background.color = teamColor;
        // 프레임에 쓰기 위해 팀 컬러의 어두운 색상을 가져오기 및 적용
        //teamColorDark = (teamId == 0)
        //? DataManager.Instance.colorTeamA_Darker
        //: DataManager.Instance.colorTeamB_Darker;
        frame.color = defaultColor;
        gameObject.SetActive(true);
    }

    public void SetAlive(bool alive)
    {
        background.color = alive
          ? teamColor
          : deadGray;
        frame.color = alive 
            ? defaultColor
            : deadGray;
    }

    public void UpdateWeapon(int weaponIndex)
    {
        weaponImage.sprite = DataManager.Instance.WeaponImages_GameScene[weaponIndex];
    }
}
