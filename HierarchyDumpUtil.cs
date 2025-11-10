
using UnityEngine;

public static class HierarchyDumpUtil
{
    public static void Dump(Transform root)
    {
        if (!root) { Debug.LogWarning("HierarchyDumpUtil: root is null"); return; }
        void Walk(Transform t, string path)
        {
            var cur = string.IsNullOrEmpty(path) ? t.name : $"{path}/{t.name}";
            Debug.Log(cur);
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), cur);
        }
        Walk(root, "");
    }
}
