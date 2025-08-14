using UnityEngine;
using UnityEngine.UI;

public class ChargeUI : MonoBehaviour
{
    [SerializeField] Image maskImage, chargeRateImage;
    Color chargingColor, fullChargeColor;

    [SerializeField] float alpha = 0.5f;

    private void OnEnable()
    {
        SetChargeRatio(0);
    }

    // 차지 UI 색상 변경
    public void SetColor(byte teamID)
    {
        if (teamID == 0)
        {
            chargingColor = DataManager.Instance.colorTeamA_Darker;
            fullChargeColor = DataManager.Instance.colorTeamA;
        }
        else if (teamID == 1)
        {
            chargingColor = DataManager.Instance.colorTeamB_Darker;
            fullChargeColor = DataManager.Instance.colorTeamB;
        }
        chargingColor = new Color(chargingColor.r, chargingColor.g, chargingColor.b, alpha);
        fullChargeColor = new Color(fullChargeColor.r, fullChargeColor.g, fullChargeColor.b, alpha);
    }

    // 충전률 변경
    public void SetChargeRatio(float ratio)
    {
        // 해상도 변경 옵션에 대응하기 위해 충전 중에 받아오게끔
        float width = Screen.currentResolution.width;
        float height = Screen.currentResolution.height;

        maskImage.rectTransform.sizeDelta = new Vector2(width, height) * ratio;
        if (ratio < 1)
            chargeRateImage.color = chargingColor;
        else
            chargeRateImage.color = fullChargeColor;
    }
}
