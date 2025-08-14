using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using UnityEngine.SceneManagement;

public class StructureProgressUI : MonoBehaviour
{
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image fillA;
    [SerializeField] private Image fillB;
    [SerializeField] private TMP_Text percentAText;
    [SerializeField] private TMP_Text percentBText;
    [SerializeField] private TMP_Text nameText;

    private StructureGroup group;

    void Awake()
    {
        group = GetComponentInParent<StructureGroup>();
        //if (group == null)
            //Debug.LogError("StructureProgressUI: 부모 중에 StructureGroup이 없습니다.", this);

    }

    private void Start()
    {
        // 팀 색상에 따라 점령 바 색상 초기화.. 인데 이거 거꾸로 들어간 거 같은데? > 초기화 거꾸로 해주면 됨(안됨)
        fillA.color = DataManager.Instance.colorTeamA;
        fillB.color = DataManager.Instance.colorTeamB;
        //string sceneName = SceneManager.GetActiveScene().name;
        if (nameText != null)
        {
            nameText.text = group.name;
            //if (sceneName == Map.GameScene2.ToString() || sceneName == Map.GameScene3.ToString())
            //{
            //    // close 태그도 꼭 닫아주세요
            //    nameText.text = $"<rotate=90>{group.name}</rotate>";
            //}
            //else
            //{
            //    nameText.text = group.name;
            //}
        }

    }

    void Update()
    {
        if (group == null) return;

        float a = group.PercentA;
        float b = group.PercentB;

        backgroundBar.color = Color.gray;
        fillA.fillAmount = a; fillA.enabled = a > 0f;
        fillB.fillAmount = b; fillB.enabled = b > 0f;
        percentAText.text = a > 0f ? $"{Mathf.RoundToInt(a * 100)}%" : "";
        percentBText.text = b > 0f ? $"{Mathf.RoundToInt(b * 100)}%" : "";
    }
}
