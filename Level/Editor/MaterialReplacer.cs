using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class MaterialReplacer : EditorWindow
{
    Material oldMat;
    Material newMat;

    [MenuItem("Tools/Replace Materials")]
    static void OpenWindow()
    {
        GetWindow<MaterialReplacer>("Replace Materials");
    }

    void OnGUI()
    {
        GUILayout.Label("씬에 배치된 머티리얼 일괄 교체", EditorStyles.boldLabel);
        oldMat = (Material)EditorGUILayout.ObjectField("Old Material", oldMat, typeof(Material), false);
        newMat = (Material)EditorGUILayout.ObjectField("New Material", newMat, typeof(Material), false);

        if (GUILayout.Button("Replace") && oldMat != null && newMat != null)
        {
            if (EditorUtility.DisplayDialog(
                "Confirm Replace",
                $"{oldMat.name} → {newMat.name}\n교체된 렌더러의 머티리얼이 모두 바뀝니다.\n진행하시겠습니까?",
                "네", "취소"))
            {
                ReplaceAll();
            }
        }
    }

    void ReplaceAll()
    {
        int count = 0;
        var roots = Selection.gameObjects.Length > 0
       ? Selection.gameObjects
       : SceneManager.GetActiveScene().GetRootGameObjects();
        // 활성 씬의 모든 렌더러 순회
        foreach (var go in roots)
            ReplaceInChildren(go);

        void ReplaceInChildren(GameObject g)
        {
            var rend = g.GetComponent<Renderer>();
            if (rend != null)
            {
                var mats = rend.sharedMaterials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == oldMat)
                    {
                        mats[i] = newMat;
                        dirty = true;
                        count++;
                    }
                }
                if (dirty)
                {
                    rend.sharedMaterials = mats;
                    EditorUtility.SetDirty(rend);
                }
            }
            // 재귀
            foreach (Transform c in g.transform)
                ReplaceInChildren(c.gameObject);
        }

        Debug.Log($"[MaterialReplacer] {count}개 렌더러의 머티리얼이 교체되었습니다.");
    }
}
