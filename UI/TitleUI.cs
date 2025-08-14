using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

public class TitleUI : MonoBehaviour, ITitleUI
{
    readonly string playerNickNameDataKey = "PlayerNickname";

    [SerializeField] TextMeshProUGUI titleText, infoText;
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject errorText;
    [SerializeField] private Image TitleTextImage;
    [SerializeField] private Image TitleAlertBar;

    [Header("Title Production 0")] [SerializeField]
    Image inkImage0;

    [SerializeField, Tooltip("닉네임 입력 때 타이틀 텍스트 색상 그레디언트")]
    Color titleGradient_EnterNickName;

    [Header("Title Production 1")] [SerializeField]
    RectTransform inkoidImage1;

    [SerializeField] Image[] inkImages1;

    // 기존의 아무 키 입력 > 닉네입 입력 각 패널을 따로 두지 않기로 하면서 제거 > isBeforeIntro 변수 하나로 대체
    // isFadeOut: 아무 키 입력 안내 텍스트 깜빡임에 쓸 변수
    bool isBeforeIntro = true, isFadeOut = true;
    public event Action<string> OnConnectClicked;

    // 타이틀 연출을 위한 두트윈 시퀀스
    Sequence titleProduction0, titleProduction1;

    // 씬 전환 연출이 끝났다면 true
    public bool isProducionEnd { get; private set; } = false;

    void Awake()
    {
        //PlayerPrefs.DeleteKey(playerNickNameDataKey);
        string saved = PlayerPrefs.GetString(playerNickNameDataKey, "");
        if (!string.IsNullOrEmpty(saved))
            nicknameInputField.text = saved;

        // 버튼 클릭 시 동작 정의
        connectButton.onClick.AddListener(() =>
        {
            // string.IsNullOrWhiteSpace()를 사용해 닉네임이 비어있거나 공백만 있는지 확인
            if (!string.IsNullOrWhiteSpace(nicknameInputField.text))
            {
                // 닉네임이 유효한 경우
                ConnectProductionStart_Button(); // 1. 씬 전환 연출 시작
                OnConnectClicked?.Invoke(nicknameInputField.text); // 2. 실제 연결 로직 호출
            }
            else
            {
                // 닉네임이 비어있는 경우
                errorText.SetActive(true);
            }
        });

        // 엔터 키 입력(Submit) 시 동작 정의
        nicknameInputField.onSubmit.AddListener((text) =>
        {
            // 버튼을 직접 누른 것처럼 동작하게 만듦
            connectButton.onClick.Invoke();
        });
    }

    private void Start()
    {
        // 아무 키 입력 시 연출
        // 타이틀 연출을 위한 명령들을 더하여 하나의 연출로 묶어주기 > 각 단계가 끝나면 다음으로 진행
        titleProduction0 = DOTween.Sequence();
        // 시퀀스 멈춰두기
        titleProduction0.Pause();
        // 물감 촥 뿌리는 효과
        titleProduction0.Append(inkImage0.transform.DOScale(20, 0.2f));
        // infoText 비활성화 + 닉네임 입력 UI 활성화 + 타이틀 텍스트 색상 변경
        titleProduction0.AppendCallback(() => { ChangeToEnterNickNameProcess(); });
        // 뿌린 물감이 사라짐(Join()을 통해 잉코이드가 올라갈 때와 동시에 진행
        titleProduction0.Join(inkImage0.DOFade(0, 0.5f));

        /////////////////////////////////////////////////////////////////////////////////////

        // 닉네임 입력 후 다음 씬으로 이동하기 전 연출
        titleProduction1 = DOTween.Sequence();
        titleProduction1.Pause();
        // 오른쪽 위로부터 오렌지색 잉코이드가 등장해 대각선 왼쪽 아래로 이동
        titleProduction1.Append(inkoidImage1.DOAnchorPos(new Vector2(-1300, -1000), 1.5f));
        // 동시에 약간의 딜레이를 주어 물감을 뿌리며 화면을 가림
        for (int i = 0; i < inkImages1.Length; i++)
        {
            float scale = 20 + i * 2.5f;
            titleProduction1.Join(inkImages1[i].transform.DOScale(scale, 0.2f).SetDelay(0.2f * (i + 1)));
        }

        // 씬 전환 연출이 끝났음을 알림
        titleProduction1.AppendCallback(() => { isProducionEnd = true; });
    }

    void Update()
    {
        // 알림 텍스트(아무 키 입력) 깜빡이게끔
        if (infoText.gameObject.activeSelf)
        {
            Color tmp = infoText.color;
            if (isFadeOut)
            {
                tmp.a = Mathf.Max(tmp.a - Time.deltaTime, 0);
                if (tmp.a <= 0)
                    isFadeOut = false;
            }
            else
            {
                tmp.a = Mathf.Min(tmp.a + 0.1f * Time.deltaTime, 1);
                if (tmp.a >= 1)
                    isFadeOut = true;
            }

            infoText.color = tmp;
        }


        // 아무 키 입력 감지 부분
        if (!isBeforeIntro)
            return;

        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        bool mouseClicked = false;
        if (Mouse.current != null)
        {
            mouseClicked =
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame;
        }

        if (keyPressed || mouseClicked)
        {
            isBeforeIntro = false;
            // 타이틀 연출 시퀀스 실행
            titleProduction0.Play();
        }
    }


    public void SetConnectButtonEnabled(bool enabled)
        => connectButton.interactable = enabled;

    // 닉네임 입력 확인 버튼을 눌렀을 때,
    public void ConnectProductionStart_Button()
    {
        // 씬을 넘어가기 전 효과 재생
        titleProduction1.Play();
    }

    // 닉네임 입력 과정으로 전환
    void ChangeToEnterNickNameProcess()
    {
        // 아무 키를 누르라는 안내 문구 텍스트 비활성화
        infoText.gameObject.SetActive(false);
        // 기존 타이틀 텍스트 비활성화, 그아래 바도 비활성화
        titleText.gameObject.SetActive(false);
        TitleAlertBar.gameObject.SetActive(false);
        // 닉네임 입력 UI 활성화
        nicknameInputField.gameObject.SetActive(true);
        // 타이틀 텍스트 이미지로 교체 - 페인트 뭍은 느낌
        TitleTextImage.gameObject.SetActive(true);
        // 처음 접속하는 유저에게 설정 창 및 조작 튜토 띄우게끔
        //if (!PlayerPrefs.HasKey(playerNickNameDataKey))
        //{
        //    // 설정 창 띄우게(조작 튜토)
        //    settingPanel.SetActive(true);
        //}

        //// titleText에 그레디언트 색상 부여
        //VertexGradient gradient = new VertexGradient(Color.white, Color.white, titleGradient_EnterNickName,
        //    titleGradient_EnterNickName);
        //titleText.colorGradient = gradient;
    }

    public void CloseSettingPanel()
    {
        settingPanel.SetActive(false);
    }
}