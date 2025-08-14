using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class SpawnPointHandler : MonoBehaviour
{

    public Transform teamZeroSpawnParent;
    public Transform teamOneSpawnParent;

    private readonly List<Transform> teamZeroPoints = new();
    private readonly List<Transform> teamOnePoints = new();

    void Awake()
    {
        foreach (Transform t in teamZeroSpawnParent) teamZeroPoints.Add(t);
        foreach (Transform t in teamOneSpawnParent) teamOnePoints.Add(t);
    }

    public Vector3 GetSpawnPoint(int teamId, int actorNumber)
    {
        var list = teamId == 0 ? teamZeroPoints : teamOnePoints;
        int idx = actorNumber % list.Count;
        return list[idx].position;
    }

    public Quaternion GetSpawnRotation(int teamId, int actorNumber)
    {
        var list = teamId == 0 ? teamZeroPoints : teamOnePoints;
        int idx = actorNumber % list.Count;
        return list[idx].rotation;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (teamZeroSpawnParent != null)
        {
            foreach (Transform t in teamZeroSpawnParent)
            {
                Gizmos.DrawSphere(t.position, 0.8f);
#if UNITY_EDITOR
                Handles.Label(t.position + Vector3.up * 0.5f, $"A:{t.name}");
#endif
            }
        }

        Gizmos.color = Color.red;
        if (teamOneSpawnParent != null)
        {
            foreach (Transform t in teamOneSpawnParent)
            {
                Gizmos.DrawSphere(t.position, 0.8f);
#if UNITY_EDITOR
                Handles.Label(t.position + Vector3.up * 0.5f, $"B:{t.name}");
#endif
            }
        }
    }
}

