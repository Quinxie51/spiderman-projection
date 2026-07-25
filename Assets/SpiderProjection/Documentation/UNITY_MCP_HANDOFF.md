# Unity MCP implementation handoff

Use this file as the build prompt for the Unity-connected Codex agent.

## Objective

Turn the existing Unity 6.3 URP 2D template into a clean vertical slice where the Crimson Crawler can run, jump, fall, land, roll, auto-target an anchor, swing with preserved momentum, release, wall-cling, and land on example picture frames/shelves. Support keyboard/mouse and generic Gamepad/Joystick, including an 8BitDo controller. Add a separate projection-calibration scene for matching invisible colliders to real wall objects.

Do not use Unity AI generation or spend Unity credits. Do not modify or copy the third-party reference art in `/Users/sle2/Documents/Projects/Spiderman`.

## Source of truth

Read these before changing Unity state:

1. `Assets/SpiderProjection/README.md`
2. `Assets/SpiderProjection/Documentation/ARCHITECTURE.md`
3. `Assets/SpiderProjection/Documentation/gameplay_spec.json`
4. `Assets/SpiderProjection/Documentation/animator_state_machine_spec.json`
5. `Assets/SpiderProjection/Documentation/input_action_spec.json`
6. `Assets/SpiderProjection/Documentation/audio_event_spec.json`
7. `Assets/SpiderProjection/Documentation/projection_layout.example.json`
8. `Assets/SpiderProjection/Documentation/asset_manifest.json`

Preserve the existing user-modified ProjectSettings files. Never hand-write `.meta` files. Refresh/import through Unity and let Unity own serialization.

## Phase 1 — import and generated presentation assets

1. Take a read-only baseline: scene, Console, project assets, Git status.
2. Refresh the Asset Database and wait for compilation/import to settle.
3. Configure sprite importers exactly as listed in `README.md`.
4. Slice character strips at 64×64 with pivot `(0.5, 0.32)`.
5. Generate one AnimationClip for every entry in `animator_state_machine_spec.json`.
6. Create `CrimsonCrawler.controller`; state names and frame rates must match the spec. The gameplay driver, not transition conditions hidden in Animator, owns state.
7. Configure SFX and music import settings from the manifest.
8. Build `SpiderProjectionMixer.mixer` with Master/Music/SFX/UI groups.
9. Validate representative sprites visually and confirm no bilinear filtering, compression, gaps, or pink materials.

Prefer one Editor utility that reads the JSON specs and generates clips/controller deterministically. Put it in `Assets/SpiderProjection/Editor`; make it safe to rerun.

## Phase 2 — vertical-slice runtime

Create:

- `SpiderProjection.Runtime.asmdef`
- input, player, physics, presentation, audio, and projection modules from `ARCHITECTURE.md`
- `PF_CrimsonCrawler`
- `PF_WebAnchor`
- `PF_MappedSurface`
- `PF_ProjectionCalibration`
- `WallDemo.unity`
- `ProjectionCalibration.unity`

Use the existing Input System package. Create or extend an InputActionAsset from `input_action_spec.json`; do not remove the existing asset until the new map is verified.

For the first playable milestone, implement in this order:

1. run, facing, ground detection
2. buffered/coyote jump, apex, fall, soft/hard land
3. roll and skid
4. web candidate debug visualization
5. attach, DistanceJoint2D swing, pump/reel, release
6. wall cling/crawl and ledge climb
7. animation/audio/VFX event wiring
8. pause/reset and action/idle music transitions

Use the tuning values in `gameplay_spec.json` as starting points, not sacred constants. Expose them in a `PlayerTuning` ScriptableObject.

## Phase 3 — demo wall and projection calibration

1. Build a visible demo using the generated frame and shelf sprites.
2. Put physics on separate invisible `MappedSurface2D` objects.
3. Add one-way top colliders for landable frames/shelves.
4. Add web anchors to top corners/edges.
5. Add the calibration image, blackout image, and safe-area overlay.
6. Load `projection_layout.example.json` only as an example; make it clear that measured values must replace it.
7. Add a mode that hides demo sprites but keeps colliders and anchors for the real wall.
8. Keep gameplay coordinates unwarped. If corner pin is required, warp only the final RenderTexture presentation.

## Phase 4 — verification

Automated:

- EditMode tests for anchor score, state priority, input buffers, and projection coordinate conversion.
- PlayMode tests for jump, swing attach/release momentum, rope limits, and one-way surfaces.

Unity checks:

- zero compile errors
- no new Console exceptions
- all 21 animation clips open and animate
- character material renders correctly in URP 2D
- controller and keyboard schemes both change active controls
- scene capture of `WallDemo`

Manual checklist to report as remaining:

- 8BitDo physical button verification
- real projector resolution/display selection
- real wall measurements
- physical collider/anchor alignment
- perceived latency and black-level test
- any public-use IP clearance

## Tool guidance

Use structured Unity MCP tools for scene, asset, GameObject, menu, and capture operations. Direct C# file edits are appropriate for runtime/editor code, followed by Unity compilation and Console checks. Use `Unity_RunCommand` only for deterministic Editor operations that lack a structured tool. Do not use Unity's paid asset-generation tools.

## Completion report

Return:

- created scenes/prefabs/scripts/settings
- test results
- one 2D Scene View or Game View capture
- measured controller status
- any manual projection steps still required
- a short list of tuning values that felt wrong in playtest
