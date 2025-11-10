
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// Player-agnostic resolver:
/// - Scans the entire scene for objects whose names/materials/meshes suggest the flail HANDLE (8454) and BALL (8459).
/// - Computes a common root as the flail root.
/// - Writes stable relative paths into the controller and enables it.
[DisallowMultipleComponent]
public class PlayerAgnosticFlailResolver : MonoBehaviour
{
    public MeleeWrenchVisualController controller;

    [Header("Search keys (case-insensitive)")]
    public string[] handleIdKeys = { "8454" };
    public string[] handleNameKeys = { "weapon", "handle" };

    public string[] ballIdKeys = { "8459" };
    public string[] ballNameKeys = { "ball", "bangle", "spike", "head" };

    [Header("Timing")]
    public float scanInterval = 0.5f;
    public float maxSearchSeconds = 20f;

    float tAccum;
    float tTotal;
    Transform foundHandle;
    Transform foundBall;

    void Reset()
    {
        controller = GetComponent<MeleeWrenchVisualController>();
    }

    void Update()
    {
        if (!controller || !controller.wrenchPrefab) return;
        if (controller.enabled) return; // already bound/enabled

        tAccum += Time.deltaTime;
        tTotal += Time.deltaTime;
        if (tAccum < scanInterval) return;
        tAccum = 0f;

        // keep best candidates across scans
        foundHandle = foundHandle ? foundHandle : FindCandidate(handleIdKeys, handleNameKeys);
        foundBall   = foundBall   ? foundBall   : FindCandidate(ballIdKeys,   ballNameKeys);

        if (foundHandle && foundBall)
        {
            var root = LowestCommonAncestor(foundHandle, foundBall);
            if (!root) root = (foundHandle.parent ? foundHandle.parent : foundHandle);

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
            // Try best-effort: if we have at least one, enable with what we have.
            Transform root = null;
            if (foundHandle && foundBall)
                root = LowestCommonAncestor(foundHandle, foundBall);
            else if (foundHandle) root = foundHandle.parent;
            else if (foundBall)   root = foundBall.parent;

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
                               "Consider swinging once to spawn ball, or adjust search keys.");
            }
            enabled = false;
        }
    }

    Transform FindCandidate(string[] idKeys, string[] nameKeys)
    {
        var allRenderers = GameObject.FindObjectsOfType<Renderer>(true);
        foreach (var r in allRenderers)
        {
            var go = r.gameObject;
            var nm = go.name.ToLowerInvariant();

            if (Matches(nm, idKeys) || Matches(nm, nameKeys)) return go.transform;

            // mesh / material clues
            var mf = go.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh && Matches(mf.sharedMesh.name.ToLowerInvariant(), idKeys)) return go.transform;

            var mats = r.sharedMaterials;
            if (mats != null)
            {
                foreach (var m in mats)
                {
                    if (!m) continue;
                    var mn = m.name.ToLowerInvariant();
                    if (Matches(mn, idKeys) || Matches(mn, nameKeys)) return go.transform;
                }
            }
        }
        // Also consider non-renderer transforms (some builds name the nodes)
        var all = GameObject.FindObjectsOfType<Transform>(true);
        foreach (var t in all)
        {
            var nm = t.name.ToLowerInvariant();
            if (Matches(nm, idKeys) || Matches(nm, nameKeys)) return t;
        }
        return null;
    }

    bool Matches(string s, string[] keys)
    {
        foreach (var k in keys)
            if (!string.IsNullOrEmpty(k) and s.IndexOf(k.lower(), System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    Transform LowestCommonAncestor(Transform a, Transform b)
    {
        if (!a || !b) return null;
        var ancestors = new System.Collections.Generic.HashSet<Transform>();
        var cur = a;
        while (cur) { ancestors.Add(cur); cur = cur.parent; }
        cur = b;
        while (cur) { if (ancestors.Contains(cur)) return cur; cur = cur.parent; }
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
