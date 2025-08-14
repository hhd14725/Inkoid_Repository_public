using UnityEngine;
using UnityEngine.UI; // RawImage/Image
using TMPro;         // TextMeshPro

[RequireComponent(typeof(CanvasRenderer))]
public class ForceUIDepthIgnore : MonoBehaviour
{
    public Material alwaysOnMat;  // Inspector에 머티리얼 드래그

    void Awake()
    {
        var cr = GetComponent<CanvasRenderer>();
        // 첫번째 슬롯(0)에 override 머티리얼 설정
        cr.materialCount = 1;              // 슬롯을 1개로 설정
        cr.SetMaterial(alwaysOnMat, 0);    // 이제 인덱스 0에 안전하게 할당
    

        // Image나 TextMeshPro 컴포넌트가 있을 경우에도 보장하려면:
        var img = GetComponent<Image>();
        if (img != null) img.material = alwaysOnMat;

        var raw = GetComponent<RawImage>();
        if (raw != null) raw.material = alwaysOnMat;

        var tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSharedMaterial = alwaysOnMat;
            var mats = tmp.fontSharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = alwaysOnMat;
            tmp.fontSharedMaterials = mats;
        }
    }
}
