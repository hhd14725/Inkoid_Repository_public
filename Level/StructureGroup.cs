using System.Collections.Generic;
using UnityEngine;

public class StructureGroup : MonoBehaviour
{
    [Tooltip("이 그룹에 속한 모든 PaintableObject 목록")]
    public List<PaintableObject> members;

    [HideInInspector] public float PercentA;
    [HideInInspector] public float PercentB;

    [HideInInspector] public float TotalWorldArea;
    void Start()
    {
        TotalWorldArea = 0f;
        foreach (var m in members)
            TotalWorldArea += m.worldArea;
    }
}
