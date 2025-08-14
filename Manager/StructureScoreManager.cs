using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class StructureScoreManager : MonoBehaviourPunCallbacks
{
    [SerializeField] float updateInterval = 1f;
    [SerializeField] ComputeShader countShader;
    int maskSize = 1024;

    ComputeBuffer countBuffer;   // 팀A/팀B 페인트용
    uint[] rawCounts = new uint[2]; 

    ComputeBuffer islandBuffer;  // 아이슬랜드 픽셀 카운트용
    uint[] rawIslandCount = new uint[1];

    List<StructureGroup> groups;

    int prevBlue = 0;
    int prevRed = 0;
    
    private GameTimer gameTimer;

    IEnumerator Start()
    {
        yield return null;
        
        // 1) 씬의 모든 그룹을 찾아서
        groups = new List<StructureGroup>(FindObjectsOfType<StructureGroup>());
        countBuffer = new ComputeBuffer(2, sizeof(uint));

        islandBuffer = new ComputeBuffer(1, sizeof(uint));
        int kernelIsland = countShader.FindKernel("CSCountIsland");
        int dispatch = Mathf.CeilToInt(maskSize / 8f);
        foreach (var g in groups)
        {
            foreach (var m in g.members)
            {
                // 초기화
                islandBuffer.SetData(new uint[1] { 0 });
                // 커널 파라미터
                countShader.SetTexture(kernelIsland, "Mask", m.getUVIslands());
                countShader.SetBuffer(kernelIsland, "IslandCount", islandBuffer);
                countShader.SetInt("Width", maskSize);
                countShader.SetInt("Height", maskSize);
                // 실행
                countShader.Dispatch(kernelIsland, dispatch, dispatch, 1);
                // 결과 가져오기
                islandBuffer.GetData(rawIslandCount);
                m.islandPixelCount = rawIslandCount[0];
            }
        }
        StartCoroutine(RecalculateLoop());
    }

    IEnumerator RecalculateLoop()
    {
        int kernel = countShader.FindKernel("CSCount");
        int groupsX = Mathf.CeilToInt(maskSize / 8f);

        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            int bluePoints = 0, redPoints = 0;

            // 2) 그룹 단위로 집계
            foreach (var g in groups)
            {
                float paintedA = 0f, paintedB = 0f;

                // 자식 구조물들 마스크마다 결과 누적
                foreach (var m in g.members)
                {
                    countBuffer.SetData(new uint[2] { 0, 0 });
                    countShader.SetTexture(kernel, "Mask", m.GetMaskTmp());
                    countShader.SetBuffer(kernel, "Result", countBuffer);
                    countShader.SetInt("Width", maskSize);
                    countShader.SetInt("Height", maskSize);
                    countShader.Dispatch(kernel, groupsX, groupsX, 1);
                    countBuffer.GetData(rawCounts);

                    //float ratioA = rawCounts[0] / (float)(maskSize * maskSize);
                    //float ratioB = rawCounts[1] / (float)(maskSize * maskSize);
                    float denom = m.islandPixelCount > 0 ? (float)m.islandPixelCount : 1f;
                    float ratioA = rawCounts[0] / denom;
                    float ratioB = rawCounts[1] / denom;

                    paintedA += ratioA * m.worldArea;
                    paintedB += ratioB * m.worldArea;
                }

                // 전체 픽셀 수 = maskSize*maskSize * 멤버 개수
                //float totalPix = maskSize * maskSize * g.members.Count;
                //g.PercentA = sumA / totalPix;
                //g.PercentB = sumB / totalPix;

                g.PercentA = g.TotalWorldArea > 0f ? paintedA / g.TotalWorldArea : 0f;
                g.PercentB = g.TotalWorldArea > 0f ? paintedB / g.TotalWorldArea : 0f;


                if (g.PercentA > g.PercentB) bluePoints++;
                else if (g.PercentB > g.PercentA) redPoints++;
            }

            int neutral = groups.Count - bluePoints - redPoints;

            if (bluePoints > prevBlue || redPoints > prevRed)
            {
                SoundManager.Instance.PlaySfx(ClipIndex_SFX.mwfx_capture);
            }
            prevBlue = bluePoints;
            prevRed = redPoints;


            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom && PhotonNetwork.IsConnectedAndReady)
                UploadRoomScores(bluePoints, neutral, redPoints);
        }
    }

    void UploadRoomScores(int blue, int neutral, int red)
    {
        var props = new Hashtable {
            {"BluePoints", blue},
            {"NeutralPoints", neutral},
            {"RedPoints", red}
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        base.OnRoomPropertiesUpdate(changed);
        if (changed.ContainsKey("BluePoints") ||
            changed.ContainsKey("NeutralPoints") ||
            changed.ContainsKey("RedPoints"))
        {
            var p = PhotonNetwork.CurrentRoom.CustomProperties;
            UIManager.Instance.GameHUDUI.UpdateCaptureUI(
                (int)p["BluePoints"],
                (int)p["NeutralPoints"],
                (int)p["RedPoints"]
            );
        }
    }

    void OnDestroy()
    {
        countBuffer?.Release();
        islandBuffer?.Release();
    }

    public override void OnLeftRoom()
    {
        StopAllCoroutines();    // RecalculateLoop 중단
        base.OnLeftRoom();
    }
}
