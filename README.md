
# MeleeModeV0.0.2

Enhancement over v0.0.1: **no player root required** and **no 'Flail' name assumption**.
Automatically finds the **handle (8454)** and **ball (8459)** in a live scene, computes a common flail root,
and wires `MeleeWrenchVisualController` for you.

## What's new
- `PlayerAgnosticFlailResolver.cs`: scans the entire scene at runtime, looks for known IDs/keywords
  on meshes/materials/nodes (defaults: handle **8454**, ball **8459**, plus "handle/weapon" and "ball/bangle/spike").
- Computes a stable **relative path** to the handle/ball nodes and sets the controller's
  `handleAttachPath` / `ballAttachPath` automatically.
- Assigns `controller.flailRoot` to the **lowest common ancestor** of handle & ball (the effective flail root).
- Keeps polling until both attachments are found (ball can spawn only during an attack).

> You do **not** need to supply a player root, flail root, or exact names.

## Install / Use
1. Import `Assets/` into your project.
2. In your scene, create an empty `MeleeMode_Visuals` object and add:
   - `MeleeWrenchVisualController` (assign your wrench FBX/prefab to `wrenchPrefab`).
   - `PlayerAgnosticFlailResolver` (drag the controller into its `controller` field).
3. Play. The resolver will print which nodes it bound to and enable the controller.

## Tunables
- In `PlayerAgnosticFlailResolver`:
  - `handleIdKeys`: defaults to `["8454"]` and keywords like "weapon/handle".
  - `ballIdKeys`: defaults to `["8459"]` and keywords like "ball/bangle/spike".
  - `scanInterval`: how often to rescan (seconds) until both are found.
  - `maxSearchSeconds`: stop trying after this time (keeps last best guess).

## Changelog
- v0.0.2: Added `PlayerAgnosticFlailResolver` that finds handle/ball by IDs; no player root needed.
- v0.0.1: Initial visual controller + simple auto binder and hierarchy dump.
