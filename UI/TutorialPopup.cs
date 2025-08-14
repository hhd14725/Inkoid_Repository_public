using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] private Image imageDisplay;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Sprite[] tutorialImages;
    [SerializeField] private string[] tutorialDescriptions;

    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    private int currentIndex = 0;

    void Start()
    {
        if (tutorialImages.Length != tutorialDescriptions.Length)
        {
            //Debug.LogError("튜토리얼 이미지 수와 설명 텍스트 수가 일치하지 않습니다.");
            return;
        }

        prevButton.onClick.AddListener(OnPrev);
        nextButton.onClick.AddListener(OnNext);
        closeButton.onClick.AddListener(ClosePopup);

        UpdatePage();
    }

    private void OnPrev()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdatePage();
        }
    }

    private void OnNext()
    {
        if (currentIndex < tutorialImages.Length - 1)
        {
            currentIndex++;
            UpdatePage();
        }
    }

    private void UpdatePage()
    {
        imageDisplay.sprite = tutorialImages[currentIndex];
        descriptionText.text = tutorialDescriptions[currentIndex];

        prevButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < tutorialImages.Length - 1;
    }

    private void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
