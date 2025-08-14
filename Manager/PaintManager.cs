using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

public class PaintManager : MonoBehaviourPun
{
    public static PaintManager Instance;

    // 원형으로 찍는 스탬프(여러 모양의 스탬프가 필요하다면 여러 스탬프에 대한 셰이더?)
    public Shader texturePaint;
    // 물감 테두리가 흩뿌려지는 느낌을 주기 위한 셰이더
    public Shader extendIslands;

    // 위의 셰이더들을 담은 머티리얼들
    // >> 만약 탄마다 다른 스탬프를 두려면 paintMaterial을 탄마다 부여
    // >> 탄이 페인트를 찍을 때 해당 탄이 가진 스탬프의 Material을 받아올 수 있게
    Material paintMaterial;
    Material extendMaterial;

    // 셰이더에 사용하는 프로퍼티들의 명칭을 ID(int)형으로 바꿔둠
    // 애니메이터에서 프로퍼티들을 Animator.StringToHash(string name); 으로 Hash 값으로 저장해두고 쓰는 것과 같은 이유
    // string으로 사용하기보다 int형으로 사용하면 보다 가벼움
    int prepareUVID = Shader.PropertyToID("_PrepareUV");
    int positionID = Shader.PropertyToID("_PainterPosition");
    int hardnessID = Shader.PropertyToID("_Hardness");
    int strengthID = Shader.PropertyToID("_Strength");
    int radiusID = Shader.PropertyToID("_Radius");
    int blendOpID = Shader.PropertyToID("_BlendOp");
    int colorID = Shader.PropertyToID("_PainterColor");
    int textureID = Shader.PropertyToID("_MainTex");
    int uvOffsetID = Shader.PropertyToID("_OffsetUV");
    int uvIslandsID = Shader.PropertyToID("_UVIslands");
    int teamID = Shader.PropertyToID("_TeamID");

    //  GPU에서 커스텀 렌더링을 누적적으로 제어
    // 멀티 플레이 중 동시다발적으로 물감 뿌리기 요청 발생
    // 물감을 뿌린 명령들을 모아놓을 버퍼 >> 물감을 뿌린 순서대로 표현 가능해 위에 덧씌우기 가능
    // 버퍼는 매니저에서 하나만 두고 타겟이 되는 텍스쳐만 바꿔가면서(SetRenderTarget(texture)) 순차적으로 실행
    // 실행이 끝난 내용들은 Clear()로 제거
    CommandBuffer commandBuffer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        paintMaterial = new Material(texturePaint);
        extendMaterial = new Material(extendIslands);

        // 커맨드버퍼 객체 생성
        commandBuffer = new CommandBuffer();
        // 디버그용으로 이름을 붙임(기본적으로 생성한 버퍼들과 구분하기 위함)
        commandBuffer.name = $"CommandBuffer_{gameObject.name}";
    }

    // 커맨드버퍼의 타겟으로 생성한 랜더텍스쳐들을 추가
    public void initTextures(PaintableObject paintable)
    {
        // 해당 PaintableObject가 가지고 있는 표현에 사용할 랜더텍스쳐들
        RenderTexture mask = paintable.GetMaskToAdd();
        RenderTexture uvIslands = paintable.getUVIslands();
        RenderTexture maskTmp = paintable.GetMaskTmp();
        RenderTexture resultTex = paintable.GetResultTex();
        // 랜더러를 상속하는 메시랜더러 클래스(컴포넌트)
        Renderer rend = paintable.GetRenderer();

        // GPU에 이하 랜더텍스쳐들에 그림을 그릴 것이라 지정해주는 명령어
        // Blit, DrawRenderer 등을 할 때 어디에 출력할지 지정
        commandBuffer.SetRenderTarget(mask);
        commandBuffer.SetRenderTarget(uvIslands);
        commandBuffer.SetRenderTarget(resultTex);

        //// 여러 오브젝트들을 자식으로 둘때 썻던 방식.
        //foreach (var r in paintable.GetRenderers())
        //{
        //    paintMaterial.SetFloat(prepareUVID, 1);
        //    commandBuffer.SetRenderTarget(uvIslands);
        //    commandBuffer.DrawRenderer(r, paintMaterial, 0);
        //}


        // 초기화 모드(이걸 안해주면 색칠이 안되요)
        paintMaterial.SetFloat(prepareUVID, 1);
        commandBuffer.SetRenderTarget(uvIslands);
        // 여기서 새로 생성한 공백 랜더텍스쳐로 갈아낌
        commandBuffer.DrawRenderer(rend, paintMaterial, 0);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }

    // 중요! : 색칠! > 매개변수 Color를 인덱스로 변경. 색상을 가져오는 부분 추가
    public void paint(PaintableObject paintable, Vector3 pos, byte teamIndex, float radius = 1f, float hardness = .5f, float strength = .5f)
    {
        //Debug.Log("색칠");

        // 해당 탄이 색칠할 컬러를 받아오기
        Team team = (Team)teamIndex;
        Color paintColor = Color.white;
        switch (team)
        {
            case Team.TeamA:
                paintColor = DataManager.Instance.colorTeamA;
                paintMaterial.SetFloat(teamID, 0);
                break;
            case Team.TeamB:
                paintColor = DataManager.Instance.colorTeamB;
                paintMaterial.SetFloat(teamID, 1);
                break;
            default:
                break;
        }

        // 해당 PaintableObject가 가지고 있는 표현에 사용할 랜더텍스쳐들
        RenderTexture mask = paintable.GetMaskToAdd();
        RenderTexture uvIslands = paintable.getUVIslands();
        RenderTexture maskTmp = paintable.GetMaskTmp();
        RenderTexture resultTex = paintable.GetResultTex();
        // 랜더러를 상속하는 메시랜더러 클래스(컴포넌트)
        Renderer rend = paintable.GetRenderer();

        // 스탬프 찍기 모드
        paintMaterial.SetFloat(prepareUVID, 0);
        // 색칠할 위치 (world position)
        paintMaterial.SetVector(positionID, pos);
        // 테두리와 관련된 요소들 >> 이거 조절해보기 > 물감이 확 퍼져서 칠해진다는 느낌을 줄 수 있을까?
        paintMaterial.SetFloat(hardnessID, hardness);
        paintMaterial.SetFloat(strengthID, strength);
        // 스탬프 크기
        paintMaterial.SetFloat(radiusID, radius);
        // 해당 오브젝트의 중첩 마스크를 대상으로 지정
        paintMaterial.SetTexture(textureID, maskTmp);
        // 스탬프 색상
        paintMaterial.SetColor(colorID, paintColor);
        extendMaterial.SetFloat(uvOffsetID, paintable.extendsIslandOffset);
        extendMaterial.SetTexture(uvIslandsID, uvIslands);

        // 원형 스탬프 1개를 찍는 임시 마스크
        commandBuffer.SetRenderTarget(mask);
        // 대상 색칠할 오브젝트의 랜더러에 스탬프 꽝!
        commandBuffer.DrawRenderer(rend, paintMaterial, 0);

        //// paintable.GetRenderers() : 해당 오브젝트에 부착된 모든 랜더러들찾는거에여
        //var rends = paintable.GetRenderers();
        //if (childIndex >= 0 && childIndex < rends.Length)
        //{
        //    commandBuffer.DrawRenderer(rends[childIndex], paintMaterial, 0);
        //}
        //else
        //{
        //    // childIndex가 -1이거나 범위 벗어나버릴경우
        //    foreach (var r in rends)
        //        commandBuffer.DrawRenderer(r, paintMaterial, 0);
        //}

        // 지금까지 해당 오브젝트에 그린 물감들 >> 마스크로 모아둠
        commandBuffer.SetRenderTarget(maskTmp);
        // .Blit을 통해 이전에 mask에 그린 1발의 물감을 누적
        commandBuffer.Blit(mask, maskTmp);
        //maskTmp : 스탬프들을 모아두고 판정에 쓸 용도

        // resultTex : 오리지널 + 스탬프(누적) >> 플레이어들이 보는 용도
        commandBuffer.SetRenderTarget(resultTex);
        commandBuffer.Blit(mask, resultTex, extendMaterial);

        // 버퍼대로 수행 후 이번에 수행한 명령들 버퍼에서 클리어 >> 다음 명령을 받을 수 있도록
        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}
