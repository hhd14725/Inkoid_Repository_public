using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PingSystem : MonoBehaviour
{
    public GameObject PingMenu;
    public RectTransform CursorRootObject;
    public RectTransform HighlightRootObject;
    public TextMeshProUGUI PingText;
    public string[] PingName;


    public GameObject PingObject;
    public Camera MainCamera;

    int slctedPing;

    string selected = "";

    //감지할 레이어들
    private int targetLayer = (1 << (int)EnumLayer.LayerType.Map_Printable)
                              | (1 << (int)EnumLayer.LayerType.Map_NotPrint)
                              | (1 << (int)EnumLayer.LayerType.Default);

    // Start is called before the first frame update
    void Start()
    {
        PingMenu.SetActive(false);
        if (MainCamera == null)
        {
            MainCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            CreatingPing();
        }


        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PingMenu.SetActive(true);
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            PingMenu.SetActive(false);
        }

        if (!PingMenu.activeSelf) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 centorScreenPos = RectTransformUtility.WorldToScreenPoint(null, HighlightRootObject.position);
        Vector2 dir = mousePos - centorScreenPos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        HighlightRootObject.rotation = Quaternion.Euler(0, 0, angle - 90f);

        if (angle >= 45 && angle < 135) selected = "Up";
        else if (angle >= 135 && angle < 225) selected = "Left";
        else if (angle >= 225 && angle < 315) selected = "Down";
        else selected = "Right";


        float SmoothAngle =
            Mathf.MoveTowardsAngle(CursorRootObject.eulerAngles.z, angle, dir.magnitude * Time.deltaTime);

        CursorRootObject.transform.eulerAngles = new Vector3(0, 0, SmoothAngle);

        float highlightedAngle = Mathf.Round(SmoothAngle / 90) * 90;
        HighlightRootObject.transform.eulerAngles = new Vector3(0, 0, highlightedAngle);

        //slctedPing = (int)Mathf.Round(SmoothAngle / 45);

        PingText.text = PingName[slctedPing];
    }

    void CreatingPing()
    {
        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, targetLayer))
        {
            // 오브젝트의 위쪽(Vector3.up)을 표면의 법선(hitInfo.normal) 방향으로 일치시키는 회전
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            Instantiate(PingObject, hitInfo.point, rotation);
        }
    }
}