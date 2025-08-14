using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PaintableSetup : MonoBehaviour
{
    [Tooltip("불투명용 페인트 쉐이더그래프가 있는 머티리얼")]
    public Material paintableMaterial;
    [Tooltip("투명 오브젝트(pipe)용 페인트 쉐이더그래프 머티리얼")]
    public Material paintableTransparentMaterial;

    // (일반 URP/Lit이면 _BaseMap, Standard면 _MainTex)
    [Tooltip("원본 Albedo 텍스처가 할당된 프로퍼티명")]
    readonly string baseTextureProp = "_BaseMap";
    [Tooltip("페인트블 쉐이더그래프에서 MainTex 노드 Reference")]
    readonly string paintableMainTexProp = "_MainTex";

    // URP/Lit 용 Metallic & Smoothness
    readonly string metallic = "_Metallic";
    readonly string smoothNess = "_Smoothness";
    readonly string paintMetallic = "_PaintMetallic";
    readonly string paintSmoothness = "_PaintSmoothness";

    // URP/Lit 에서 알파(투명도)를 담고 있는 컬러
    readonly string baseColorProp = "_BaseColor";

    void Awake()
    {
        var rend = GetComponent<Renderer>();
        var originalMat = rend.sharedMaterial;
        if (originalMat == null)
        {
            //Debug.LogWarning("PaintableSetup: Renderer에 원본 머티리얼이 없습니다.", this);
            return;
        }

        // 태그에 따라 템플릿 머티리얼 선택
        var templateMat = gameObject.CompareTag("Pipe")
            ? paintableTransparentMaterial
            : paintableMaterial;
        if (templateMat == null)
        {
            //Debug.LogWarning($"PaintableSetup: 템플릿 머티리얼이 할당되지 않았습니다.", this);
            return;
        }

        // 렌더러에 템플릿 복제 인스턴스 붙이고, 그 인스턴스를 paintMat으로 참조
        rend.material = templateMat;
        var paintMat = rend.material;

        // 1) Albedo(MainTex) 복사
        Texture baseTex = null;
        if (originalMat.HasProperty(baseTextureProp))
        {
            baseTex = originalMat.GetTexture(baseTextureProp);
            if (baseTex != null && paintMat.HasProperty(paintableMainTexProp))
                paintMat.SetTexture(paintableMainTexProp, baseTex);
        }

        // 2) Metallic & Smoothness 복사
        if (originalMat.HasProperty(metallic))
            paintMat.SetFloat(metallic, originalMat.GetFloat(metallic));
        if (originalMat.HasProperty(smoothNess))
            paintMat.SetFloat(smoothNess, originalMat.GetFloat(smoothNess));

        // 3) Emission 복사
        const string emissionColorProp = "_EmissionColor";
        if (originalMat.HasProperty(emissionColorProp))
        {
            var eCol = originalMat.GetColor(emissionColorProp);
            paintMat.SetColor(emissionColorProp, eCol);
            paintMat.EnableKeyword("_EMISSION");
        }
        const string emissionMapProp = "_EmissionMap";
        if (originalMat.HasProperty(emissionMapProp) && paintMat.HasProperty(emissionMapProp))
            paintMat.SetTexture(emissionMapProp, originalMat.GetTexture(emissionMapProp));

        // 4) BaseColor 알파 복사 & 파이프일 땐 불투명 강제
        if (originalMat.HasProperty(baseColorProp) && paintMat.HasProperty(baseColorProp))
        {
            var col = originalMat.GetColor(baseColorProp);
            if (gameObject.CompareTag("Pipe"))
                col.a = 1f;  // 파이프에 칠할 땐 반드시 불투명
            paintMat.SetColor(baseColorProp, col);
        }

        // 5) renderQueue 유지
        paintMat.renderQueue = originalMat.renderQueue;
        rend.material = paintMat;
    }
}
