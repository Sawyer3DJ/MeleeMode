
using UnityEngine;
using System.Linq;

/// Melee Mode visual driver:
/// - hides flail visuals (handle/ball/chain/trails)
/// - instantiates two wrench visuals:
///     * wrenchGrip  -> attached to handle/hand (idle)
///     * wrenchSwing -> attached to ball (during attack)
/// - auto-switches between them when it detects a swing.
[DisallowMultipleComponent]
public class MeleeWrenchVisualController : MonoBehaviour
{
    [Header("Assign your wrench FBX/prefab (e.g., rc4/9210)")]
    public GameObject wrenchPrefab;

    [Header("Flail instance root (set to live weapon root at runtime)")]
    public Transform flailRoot;

    [Header("Optional explicit attach paths (paste from a hierarchy dump)")]
    public string handleAttachPath = ""; // e.g. "Armature/Hand_R/Weapon"
    public string ballAttachPath   = ""; // e.g. "Flail/Chain/Ball"

    [Header("Heuristic bone names to try for handle/ball (order matters)")]
    public string[] handleBoneNames = { "Weapon", "WeaponRoot", "r_hand", "RightHand", "Hand_R" };
    public string[] ballNodeNames   = { "Ball", "bangle", "Head", "Spike", "FlailBall" };

    [Header("Hide any renderer whose name contains these (case-insensitive)")]
    public string[] hideKeys = { "flail", "bangle", "ball", "chain", "trail", "8454", "8459" };

    [Header("Swing detection (heuristics)")]
    [Tooltip("m/s threshold for ball movement to count as 'swinging'")]
    public float ballSpeedSwingThreshold = 3.0f;
    [Tooltip("If any Trail/Line Renderer under the flail is enabled, treat as swinging")]
    public bool  useTrailAsSwingSignal = true;

    [Header("Debug")]
    public bool  logOnceOnBind = true;
    public bool  showGizmos = false;

    Transform handleAttach;
    Transform ballAttach;
    GameObject wrenchGrip;
    GameObject wrenchSwing;

    // tracking ball speed
    Transform ballTracker;
    Vector3 lastBallPos;
    bool lastSwingState;

    void Awake()
    {
        if (!flailRoot || !wrenchPrefab)
        {
            Debug.LogWarning("[MeleeWrenchVisualController] Assign flailRoot and wrenchPrefab.");
            enabled = false;
            return;
        }

        // 1) Hide flail visuals (keep logic alive)
        foreach (var r in flailRoot.GetComponentsInChildren<Renderer>(true))
        {
            var n = r.gameObject.name.ToLowerInvariant();
            if (hideKeys.Any(k => n.Contains(k))) r.enabled = false;
        }
        foreach (var tr in flailRoot.GetComponentsInChildren<TrailRenderer>(true))  tr.enabled = false;
        foreach (var lr in flailRoot.GetComponentsInChildren<LineRenderer>(true))   lr.enabled = false;
        foreach (var ps in flailRoot.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 2) Find handle & ball attach points
        handleAttach = ResolveAttach(flailRoot, handleAttachPath, handleBoneNames, t =>
        {
            var n = t.name.ToLowerInvariant();
            return n.Contains("weapon") || n.Contains("hand") || n.Contains("r_hand");
        });

        ballAttach = ResolveAttach(flailRoot, ballAttachPath, ballNodeNames, t =>
        {
            var n = t.name.ToLowerInvariant();
            return n.Contains("ball") || n.Contains("bangle") || n.Contains("spike");
        });

        if (!handleAttach) handleAttach = flailRoot;
        if (!ballAttach)   ballAttach   = flailRoot;

        // 3) Spawn two wrench instances
        wrenchGrip  = Instantiate(wrenchPrefab, handleAttach, false);
        wrenchSwing = Instantiate(wrenchPrefab, ballAttach,   false);

        // default: idle shows grip wrench; swing wrench hidden
        SetActiveSafe(wrenchGrip,  true);
        SetActiveSafe(wrenchSwing, false);

        // speed tracker on ball
        ballTracker = ballAttach;
        lastBallPos = ballTracker.position;

        if (logOnceOnBind)
        {
            Debug.Log($"[MeleeWrenchVisualController] handleAttach: {handleAttach.GetHierarchyPath()}");
            Debug.Log($"[MeleeWrenchVisualController] ballAttach:   {ballAttach.GetHierarchyPath()}");
        }
    }

    void Update()
    {
        bool swinging = DetectSwing();

        if (swinging != lastSwingState)
        {
            SetActiveSafe(wrenchGrip,  !swinging);
            SetActiveSafe(wrenchSwing,  swinging);
            lastSwingState = swinging;
        }
    }

    bool DetectSwing()
    {
        // A) Trail/Line as signal (if any got toggled by the engine)
        if (useTrailAsSwingSignal)
        {
            var trailOn = flailRoot.GetComponentsInChildren<TrailRenderer>(true).Any(t => t.enabled);
            var lineOn  = flailRoot.GetComponentsInChildren<LineRenderer>(true).Any(l => l.enabled);
            if (trailOn || lineOn) return true;
        }

        // B) Ball speed heuristic
        if (ballTracker)
        {
            var cur = ballTracker.position;
            var speed = (cur - lastBallPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastBallPos = cur;
            if (speed >= ballSpeedSwingThreshold) return true;
        }

        return false;
    }

    Transform ResolveAttach(Transform root, string explicitPath, string[] preferredNames, System.Func<Transform,bool> fallbackHeuristic)
    {
        if (!string.IsNullOrEmpty(explicitPath))
        {
            var found = root.Find(explicitPath);
            if (found) return found;
        }
        var all = root.GetComponentsInChildren<Transform>(true);
        // preferred names (exact match)
        foreach (var nm in preferredNames)
        {
            var t = all.FirstOrDefault(x => string.Equals(x.name, nm, System.StringComparison.OrdinalIgnoreCase));
            if (t) return t;
        }
        // heuristic fallback
        return all.FirstOrDefault(fallbackHeuristic);
    }

    void SetActiveSafe(GameObject go, bool on)
    {
        if (go && go.activeSelf != on) go.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.green;
        if (handleAttach) Gizmos.DrawWireSphere(handleAttach.position, 0.05f);
        Gizmos.color = Color.red;
        if (ballAttach) Gizmos.DrawWireSphere(ballAttach.position, 0.05f);
    }
}

public static class TransformPathExt
{
    public static string GetHierarchyPath(this Transform t)
    {
        if (!t) return "<null>";
        string p = t.name;
        var cur = t.parent;
        while (cur)
        {
            p = $"{cur.name}/{p}";
            cur = cur.parent;
        }
        return p;
    }
}
