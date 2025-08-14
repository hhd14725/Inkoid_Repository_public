using UnityEngine;

// 모든 색칠 가능한 오브젝트들에 부착
public class PaintableObject : MonoBehaviour
{
    int maskTextureID = Shader.PropertyToID("_MaskTexture");
    int teamColorAID = Shader.PropertyToID("_TeamColorA");
    int teamColorBID = Shader.PropertyToID("_TeamColorB");

    public float extendsIslandOffset = 1;

    // 오리지널 텍스쳐는 머티리얼에서 각자 넣어주면 되는걸로
    RenderTexture maskToAdd, maskTmp, result, teamIDMap;
    RenderTexture uvIslandsRenderTexture;
    Renderer rend;

    //그룹 점유율 계산 가중치를 위한 바운더리박스 담아두는 속성
    public float worldArea { get; private set; }

    [HideInInspector] public uint islandPixelCount;

    //List<Renderer> rends;
    [HideInInspector] public float percentA;
    [HideInInspector] public float percentB;
    public RenderTexture getUVIslands() => uvIslandsRenderTexture;
    // 랜더텍스쳐, 랜더러에 접근하기 위한 메서드들
    public RenderTexture GetMaskToAdd()
    {
        return maskToAdd;
    }

    public RenderTexture GetMaskTmp()
    {
        return maskTmp;
    }

    public RenderTexture GetResultTex()
    {
        return result;
    }

    //public Renderer[] GetRenderers() => rends.ToArray();
    public Renderer GetRenderer()
    {
        return rend;
    }
    private void Awake()
    {
        rend = GetComponent<Renderer>();

        //그루핑점유율용도: 월드 좌표에서의 면적 계산을 위해 개별 구조물 바운더리 박스 크기 구하기
        var size = rend.bounds.size;
        worldArea = size.x * size.y;
    }

    void Start()
    {
        // 1024로 쓰는 이유 : 텍스쳐가 뭉개지지 않는 2의 거듭제곱이면서 가장 경제적인 현실적인 선. 2048을 넘어가면 너무 무거워짐
        int textureSize = 1024;

        // RenderTexture들 생성
        // 1. 마스크를 찍을 때 사용하는 랜더텍스쳐
        maskToAdd = new RenderTexture(textureSize, textureSize, 0);
        maskToAdd.filterMode = FilterMode.Bilinear;

        // 2. 찍은 물감 스탬프들을 모아둔 마스크
        maskTmp = new RenderTexture(textureSize, textureSize, 0);
        maskTmp.filterMode = FilterMode.Bilinear;

        // 3. 오리지널 + 마스크 누적 적용 텍스쳐
        result = new RenderTexture(textureSize, textureSize, 0);
        result.filterMode = FilterMode.Bilinear;

        // 4. 점령중인 팀 ID를 저장하기 위한 랜더텍스쳐
        // (위 랜더텍스쳐들은 색상을 쓰고 있기에 따로 써줘야 색상이 변질되지 않음)
        teamIDMap = new RenderTexture(textureSize, textureSize, 0);
        result.filterMode = FilterMode.Bilinear;

        uvIslandsRenderTexture = new RenderTexture(textureSize, textureSize, 0); // , RenderTextureFormat.RFloat
        uvIslandsRenderTexture.filterMode = FilterMode.Point;

        // 랜더텍스쳐들을 명시적으로 생성(안정적으로 동작하기 위해 필요)
        maskToAdd.Create();
        maskTmp.Create();
        result.Create();
        teamIDMap.Create();

        // 랜더러가 오리지널 텍스쳐로 사용할 것으로 result를 선택
        // rends = GetComponentsInChildren<MeshRenderer>()
        //.Cast<Renderer>()
        //.Concat(GetComponentsInChildren<SkinnedMeshRenderer>())
        //.ToList();
        // foreach (var r in rends)
        // {
        //     r.material.SetTexture(maskTextureID, result);
        // }


        //rend = GetComponent<Renderer>();
        rend.material.SetTexture(maskTextureID, result);
        rend.material.SetColor(teamColorAID, DataManager.Instance.colorTeamA);
        rend.material.SetColor(teamColorBID, DataManager.Instance.colorTeamB);
        PaintManager.Instance.initTextures(this);
    }

    // 해당 오브젝트가 비활성화 될 때 생성했던 랜더텍스쳐들 풀어주기
    private void OnDisable()
    {
        maskToAdd.Release();
        maskTmp.Release();
        result.Release();
    }

    // 면적을 계산하는 메서드 필요 >> 복잡한 구조물 외부 면적도 알 수 있을까? >> 방법 찾아보고 어느 정도 복잡한 구조물에 대한 면적을 구할 수 있는지?

    // 칠해진 영역
    // 매개변수에 따라 해당 구조물에 대한 TeamA, B의 점유율
    // isTeamA = true : 팀A

    //안쓰는 계산 함수 - 최적화에 매우 좋지않은 퍼포먼스를 보임
    //public float GetPaintPercentage(bool isTeamA)
    //{
    //    RenderTexture rt = maskTmp;  // 누적 마스크
    //    // 1) RenderTexture를 읽기용으로 활성화
    //    RenderTexture prev = RenderTexture.active;
    //    RenderTexture.active = rt;

    //    // 2) Texture2D에 복사
    //    Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
    //    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
    //    tex.Apply();

    //    RenderTexture.active = prev;

    //    // 3) 픽셀 샘플링
    //    Color[] pixels = tex.GetPixels();
    //    float count = 0;
    //    for (int i = 0; i < pixels.Length; i++)
    //    {
    //        var c = pixels[i];
    //        // 예: 팀A(blue) 판단: 파란 채널이 빨간 채널보다 크면
    //        if (isTeamA)
    //        {
    //            if (c.b > c.r && c.b > 0.01f) count++;
    //        }
    //        else
    //        {
    //            if (c.r > c.b && c.r > 0.01f) count++;
    //        }
    //    }
    //    float ratio = count / pixels.Length;

    //    // 4) 메모리 해제
    //    Destroy(tex);
    //    return ratio;
    //}
}
