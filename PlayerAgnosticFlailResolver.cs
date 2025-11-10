using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlayerAgnosticFlailResolver : MonoBehaviour
{
    public MeleeWrenchVisualController controller;

    [Header("Search keys (case-insensitive)")]
    public string[] handleIdKeys  = { "8454" };
    public string[] handleNameKeys = { "weapon", "handle" };

    public string[] ballIdKeys    = { "8459" };
    public string[] ballNameKeys  = { "ball", "bangle", "spike", "head" };

    [Header("Timing")]
    public float scanInterval = 0.5f;
    public float maxSearchSeconds = 20f;

    float tAccum, tTotal;
    Transform foundHandle, foundBall;

    void Reset() { controller = GetComponent<MeleeWrenchVisualController>(); }

    void Update()
    {
        if (!controller || !controller.wrenchPrefab) return;
        if (controller.enabled) return; // already bound

        tAccum += Time.deltaTime;
        tTotal += Time.deltaTime;
        if (tAccum < scanInterval) return;
        tAccum = 0f;

        // keep best candidates across scans
        if (!foundHandle) foundHandle = FindCandidate(handleIdKeys, handleNameKeys);
        if (!foundBall)   foundBall   = FindCandidate(ballIdKeys,   ballNameKeys);

        if (foundHandle && foundBall)
        {
            var root = LowestCommonAncestor(foundHandle, foundBall) ?? (foundHandle.parent ? foundHandle.parent : foundHandle);
            controller.flailRoot = root;
            controller.handleAttachPath = MakeRelativePath(root, foundHandle);
            controller.ballAttachPath   = MakeRelativePath(root, foundBall);
            controller.enabled = true; // triggers controller's Awake()
            Debug.Log("[FlailResolver] Bound:\n  root:   " + root.GetHierarchyPath() +
                      "\n  handle: " + controller.handleAttachPath +
                      "\n  ball:   " + controller.ballAttachPath);
            enabled = false;
            return;
        }

        if (tTotal >= maxSearchSeconds)
        {
            Transform root = null;
            if (foundHandle && foundBall)      root = LowestCommonAncestor(foundHandle, foundBall);
            else if (foundHandle)              root = foundHandle.parent;
            else if (foundBall)                root = foundBall.parent;

            if (root)
            {
                controller.flailRoot = root;
                controller.handleAttachPath = foundHandle ? MakeRelativePath(root, foundHandle) : "";
                controller.ballAttachPath   = foundBall   ? MakeRelativePath(root, foundBall)   : "";
                controller.enabled = true;
                Debug.LogWarning("[FlailResolver] Partial bind (timeout). " +
                                 "root=" + root.GetHierarchyPath() +
                                 ", handle=" + controller.handleAttachPath +
                                 ", ball=" + controller.ballAttachPath);
            }
            else
            {
                Debug.LogError("[FlailResolver] Failed to locate flail handle/ball within time. " +
                               "Swing once to spawn the ball, or adjust search keys.");
            }
            enabled = false;
        }
    }

    Transform FindCandidate(string[] idKeys, string[] nameKeys)
    {
        // Prefer renderers (real geometry)
        foreach (var r in GameObject.FindObjectsOfType<Renderer>(true))
        {
            var go = r.gameObject;
            var n = go.name;
            if (Matches(n, idKeys) || Matches(n, nameKeys)) return go.transform;

            var mf = go.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && Matches(mf.sharedMesh.name, idKeys)) return go.transform;

            var mats = r.sharedMaterials;
            if (mats != null)
                foreach (var m in mats)
                    if (m && Matches(m.name, idKeys)) return go.transform;
        }
        // Fallback: any transform name
        foreach (var t in GameObject.FindObjectsOfType<Transform>(true))
            if (Matches(t.name, idKeys) || Matches(t.name, nameKeys)) return t;

        return null;
    }

    bool Matches(string s, string[] keys)
    {
        if (string.IsNullOrEmpty(s) || keys == null) return false;
        foreach (var k in keys)
        {
            if (string.IsNullOrEmpty(k)) continue;
            if (s.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    Transform LowestCommonAncestor(Transform a, Transform b)
    {
        if (!a || !b) return null;
        var ancestors = new HashSet<Transform>();
        for (var c = a; c; c = c.parent) ancestors.Add(c);
        for (var c = b; c; c = c.parent) if (ancestors.Contains(c)) return c;
        return null;
    }

    string MakeRelativePath(Transform root, Transform leaf)
    {
        if (!root || !leaf) return "";
        var stack = new System.Collections.Generic.Stack<string>();
        var cur = leaf;
        while (cur && cur != root) { stack.Push(cur.name); cur = cur.parent; }
        if (cur != root) return leaf.GetHierarchyPath(); // fallback absolute
        return string.Join("/", stack.ToArray());
    }
}
