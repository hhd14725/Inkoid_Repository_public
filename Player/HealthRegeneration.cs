using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthRegeneration : MonoBehaviour
{
    private PlayerHandler playerHandler;

    public float regenDelay = 2f;
    public float regenRate = 20f;

    private Coroutine regenCoroutine;

    [SerializeField] private Image bloodScreenImage;

    private void Awake()
    {
        playerHandler = GetComponent<PlayerHandler>();
    }

    private void Start()
    {
        SetBloodScreenColorByTeam();
    }

    private void OnEnable()
    {
        InitBloodScreen();
        playerHandler.OnDamaged += StartHealthRegenDelay;
        playerHandler.OnDamaged += UpdateBloodScreen;
    }

    private void OnDisable()
    {
        playerHandler.OnDamaged -= StartHealthRegenDelay;
        playerHandler.OnDamaged -= UpdateBloodScreen;
    }

    private void StartHealthRegenDelay()
    {
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        regenCoroutine = StartCoroutine(HealthRegenRoutine());
    }

    private IEnumerator HealthRegenRoutine()
    {
        //UpdateBloodScreen();
        
        yield return new WaitForSeconds(regenDelay); // ✅ 일정 시간 대기 후

        while (playerHandler.Status.Health < playerHandler.Status.MaximumHealth)
        {
            playerHandler.Status.Health += regenRate * Time.deltaTime;
            playerHandler.Status.Health =
                Mathf.Clamp(playerHandler.Status.Health, 0f, playerHandler.Status.MaximumHealth);

            UpdateBloodScreen();

            yield return null;
        }
    }

    private void UpdateBloodScreen()
    {
        float max = playerHandler.Status.MaximumHealth;
        float current = playerHandler.Status.Health;

        float normalized = 1f - (current / max);

        Color color = bloodScreenImage.color;
        color.a = Mathf.Clamp01(normalized * 0.8f);
        bloodScreenImage.color = color;
    }

    private void SetBloodScreenColorByTeam()
    {
      
        var color = playerHandler.Info.TeamId == (int)EnumLayer.LayerType.TeamA-6
            ? DataManager.Instance.colorTeamB_Darker
            : DataManager.Instance.colorTeamA_Darker;
        
        ////Debug.Log("팀 : "+playerHandler.Info.TeamId+"    색 : "+color);
        ////Debug.Log("colorTeamA_Darker : "+DataManager.Instance.colorTeamA_Darker+"    colorTeamB_Darker : "+DataManager.Instance.colorTeamB_Darker);

        bloodScreenImage.color = new Color(color.r, color.g, color.b, 0); // 처음엔 투명
    }

    private void InitBloodScreen()
    {
        var color = bloodScreenImage.color;
        color.a = 0f;
        bloodScreenImage.color = color;
    }
}