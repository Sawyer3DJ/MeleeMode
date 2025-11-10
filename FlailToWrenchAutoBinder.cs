
using UnityEngine;
using System.Linq;

/// Convenience: if you don't have an equip event yet, this polls once to locate
/// the live Flail weapon under the player, then hands it to MeleeWrenchVisualController.
[DisallowMultipleComponent]
public class FlailToWrenchAutoBinder : MonoBehaviour
{
    public MeleeWrenchVisualController controller;
    public Transform playerRoot;                // assign this (local player root)
    public string flailMarkerName = "Flail";    // substring to identify the weapon instance
    public float searchDelay = 0.25f;           // seconds after Start before we search

    float t;
    bool applied;

    void Start() { t = 0f; }

    void Update()
    {
        if (applied || !controller || !playerRoot) return;

        t += Time.deltaTime;
        if (t < searchDelay) return;

        var tfs = playerRoot.GetComponentsInChildren<Transform>(true);
        var flailRoot = tfs.FirstOrDefault(tf =>
            tf.name.IndexOf(flailMarkerName, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tf.name.ToLowerInvariant().Contains("weapon"));

        if (flailRoot)
        {
            controller.flailRoot = flailRoot;
            controller.enabled = true; // triggers Awake() in the controller
            applied = true;
            Debug.Log("[FlailToWrenchAutoBinder] Bound to: " + flailRoot.GetHierarchyPath());
        }
    }
}
