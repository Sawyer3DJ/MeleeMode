
# MeleeModeV0.0.1

A minimal, **add-only** Melee Mode visual layer for DZO/Forge maps that makes the **Flail** play **Wrench** visuals.

## What it does (v0.0.1)
- Hides Flail handle/ball/chain/trail renderers.
- Spawns **two Wrench visuals**:
  - **Grip Wrench**: attached to the handle/hand for idle/ready.
  - **Swing Wrench**: attached to the spiked ball (or equivalent) during a swing.
- Auto-switches visibility between Grip/Swing using **heuristics**:
  - active **Trail/Line** renderers under the Flail OR
  - **ball node speed** exceeding a threshold.
- No edits to existing engine scripts; **add-only** C#/Unity behaviour.

> NOTE: This package is **visual-only**. Gameplay still uses the Flail's melee logic/timings/netcode. 
> Binding Circle (O) to trigger Flail melee is a separate C-side rule you can add later if desired.

---

## Install (Unity)
1. **Drag the `Assets/` folder** from this package into your project (keep the path `Assets/Scripts/MeleeMode/`).
2. In your scene:
   - Create an empty GameObject named `MeleeMode_Visuals` and add **`MeleeWrenchVisualController`**.
   - Assign your **Wrench FBX/prefab** (e.g., `rc4/9210`) to **`wrenchPrefab`**.
   - At runtime, get a reference to the **live Flail weapon root** and assign it to **`flailRoot`**.
     - If you don't have an equip event, add the provided **`FlailToWrenchAutoBinder`** component to auto-discover the Flail and bind once.
3. (Optional) If the script doesn't find the best attach nodes automatically:
   - Add **`HierarchyDumpUtil`** anywhere.
   - Call `HierarchyDumpUtil.Dump(flailRoot)` once during play to print the full hierarchy in the Console.
   - Copy a stable path for the **handle** and **ball** nodes into `handleAttachPath` and `ballAttachPath` in `MeleeWrenchVisualController`.

### Typical hook-up (one-liner example)
```csharp
public class ExampleBindOnSpawn : MonoBehaviour
{
    public MeleeWrenchVisualController ctrl;
    public Transform flailWeaponRoot; // set from your spawn/equip system
    public GameObject wrenchPrefab;   // set in Inspector (rc4/9210)

    void Start()
    {
        ctrl.wrenchPrefab = wrenchPrefab;
        ctrl.flailRoot = flailWeaponRoot;
        // Optional: ctrl.handleAttachPath = "Armature/Hand_R/Weapon";
        // Optional: ctrl.ballAttachPath   = "Flail/Chain/Ball";
        ctrl.enabled = true;
    }
}
```

---

## Tunables (Inspector fields)
- **wrenchPrefab** — your wrench FBX/prefab (drag `rc4/9210` here).
- **flailRoot** — live Transform of the equipped Flail root.
- **handleAttachPath / ballAttachPath** — exact hierarchy paths (optional).
- **handleBoneNames / ballNodeNames** — name heuristics if paths are empty.
- **hideKeys** — substrings of renderers to hide (flail/ball/chain/trail).
- **ballSpeedSwingThreshold** — m/s used to detect swing via ball motion.
- **useTrailAsSwingSignal** — when true, any active Trail/Line enables swing state.
- **showGizmos** — draws small spheres at attach points for sanity checks.

---

## Workflow tips
- Start **without** explicit paths; the heuristics usually find good nodes.
- If the **Swing Wrench** never appears, the flail may not spawn a separate ball node until attack begins—do a quick swing once, then the controller will find it.
- If the attach looks offset, tweak the wrench model pivot or apply a small local offset on the spawned wrench (edit the script where marked).

---

## Changelog
### v0.0.1
- Initial release: dual-attach Wrench visuals with heuristic swing detection and full flail visual mute.
- Included optional auto-binder and hierarchy dumper helpers.

---

## Files
- `Assets/Scripts/MeleeMode/MeleeWrenchVisualController.cs`
- `Assets/Scripts/MeleeMode/FlailToWrenchAutoBinder.cs`
- `Assets/Scripts/MeleeMode/HierarchyDumpUtil.cs`

---

## Next (when you want it)
- Optional C-side rule to bind **Circle (O)** to Flail melee with range clamp.
- Team-color material swapping on both wrench instances.
- Pixel-perfect swing state via a native `IsMeleeActive()` flag instead of heuristics.
