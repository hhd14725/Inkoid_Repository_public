using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RPGDelayUI : MonoBehaviour
{
    Image delayImage;
    Coroutine delayCoroutine;
    float alpha = 0.7f;

    private void Awake()
    {
        delayImage = GetComponent<Image>();
    }

    public void ShowDelayUI(float delay)
    {
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
        }
        delayCoroutine = StartCoroutine(Delaying(delay));
    }

    IEnumerator Delaying(float delay)
    {
        float delayLeft = delay;
        while(delay > 0)
        {
            delayLeft -= Time.deltaTime; 
            delayImage.fillAmount = delayLeft/delay;
            yield return null;
        }
    }

    public void ResetFillAmount()
    {
        delayImage.fillAmount = 0;
    }

    public void SetColor(byte teamID)
    {
        Color colorDark = Color.white;
        if(teamID == 0 )
            colorDark = DataManager.Instance.colorTeamA_Darker;
        else if (teamID == 1 )
            colorDark = DataManager.Instance.colorTeamB_Darker;
        delayImage.color = new Color(colorDark.r, colorDark.g, colorDark.b, alpha);
    }
}
