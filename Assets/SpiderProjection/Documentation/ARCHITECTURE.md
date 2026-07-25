# Recommended Unity architecture

## Core choice

Use one authoritative hierarchical gameplay state machine in C#. The Animator is presentation-only. This avoids the common failure where Animator transitions, Rigidbody2D physics, and input callbacks each believe they own movement.

```text
PlayerInputReader
      ↓ intent
PlayerBrain (authoritative state)
 ├─ SpiderMotor2D
 ├─ SwingController2D ── WebTargetResolver2D
 ├─ WallTraversal2D
 └─ PlayerSensors2D
      ↓ events/state
AnimationDriver2D + PlayerAudioView + WebLineView2D
```

The exact components, starting values, state IDs, and transition rules are in `gameplay_spec.json`.

## Suggested assemblies and folders

```text
Assets/SpiderProjection/
├── Runtime/
│   ├── Core/
│   ├── Input/
│   ├── Player/
│   ├── Projection/
│   ├── Audio/
│   └── Presentation/
├── Editor/
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
├── Prefabs/
├── Scenes/
└── Settings/
```

Create `SpiderProjection.Runtime.asmdef`, `SpiderProjection.Editor.asmdef`, and test assemblies. Keep editor-only sprite slicing, clip generation, and projection-authoring utilities outside the runtime assembly.

## Player prefab

Recommended root/child arrangement:

```text
PF_CrimsonCrawler
├── Visual
│   └── SpriteRenderer + Animator
├── WristSocket
├── GroundProbe
├── WallProbe
├── LedgeProbe
├── WebLine
└── Debug
```

Root components:

- `Rigidbody2D`: Dynamic, Interpolate, Continuous, freeze Z rotation.
- `CapsuleCollider2D`: main body collider.
- `DistanceJoint2D`: disabled until a swing attaches; auto distance off.
- `PlayerInput`: C# event callbacks, `Gameplay` map.
- The player modules from `gameplay_spec.json`.

Never animate the root transform. A clip changes only the child `SpriteRenderer.sprite`. Collider shape changes for crouch/roll belong to gameplay state profiles, not animation events.

## Swing mechanics

1. On Swing performed, obtain aim from the right stick or pointer. If aim is near zero, use velocity, then facing/up.
2. Query `WebAnchor2D` candidates inside the search radius.
3. Reject candidates without line of sight or outside the configured distance.
4. Score remaining candidates using `gameplay_spec.json`.
5. Enter `WebShoot`, show muzzle VFX, and play one randomized web-shot sound.
6. Enable the `DistanceJoint2D` at the selected point, set its distance, then enter `SwingAttach` and `SwingLoop`.
7. Apply tangential pump force from horizontal input. Use up/down to shorten/lengthen the joint within configured limits.
8. On Swing canceled, disable the joint without zeroing velocity. Apply the small configured release boost and enter `SwingRelease`.

Keep the anchor position in world space. Render the web independently between `WristSocket` and anchor with a `LineRenderer` or a tiled one-pixel sprite strip using an unlit material. Do not bake the rope into animation frames.

## Physical wall mapping

A `MappedSurface2D` is an empty GameObject with a `Collider2D` and metadata. Colliders are inherently invisible in a player build; there is no mesh to disable.

Suggested layers:

- `Player`
- `WorldSolid`
- `OneWayPlatform`
- `WebAnchor`
- `Hazard`
- `ProjectionDebug`

Suggested sorting layers:

- `Background`
- `PhysicalGuide`
- `World`
- `WebBehind`
- `Player`
- `WebFront`
- `VFX`
- `UI`
- `Debug`

For a real picture frame or shelf:

- Place a `BoxCollider2D` or `PolygonCollider2D` over its measured projected boundary.
- Use `PlatformEffector2D` only for surfaces that should be landable from above and passable from below.
- Add explicit `WebAnchor2D` children at useful top/corner points, or let the resolver use `Collider2D.ClosestPoint`.
- Turn off the visible demo SpriteRenderer after calibration.

## Projection pipeline

Start with the simple pipeline:

1. Set the projector as a normal display at a fixed 1920×1080 resolution.
2. Use one orthographic camera with a pure-black clear color.
3. Project `projection_calibration_1920x1080.png`.
4. Align the projector/corner pin, then place surface colliders using the calibration scene.
5. Save measured geometry into a `ProjectionLayout` ScriptableObject or JSON.

Add a RenderTexture plus four-corner warp only when the projector cannot be physically aligned or when the wall plane needs software correction. Keep gameplay in normal orthographic world coordinates; apply the warp only to the final presentation image. If the room has non-planar geometry, use a subdivided warp mesh or external mapping software rather than distorting gameplay physics.

The example normalized layout is `projection_layout.example.json`.

## Materials and Lens-to-Unity conversion

No Lens Studio material is used by this pack. CLAD generated the audio as WAV files, while visuals are ordinary PNGs. In Unity:

- Pixel art: `Sprite-Unlit-Default`
- Optional lit demo scene: `Sprite-Lit-Default`
- Web line and overlay: URP 2D unlit
- Music/SFX: `AudioClip`

Lens Studio centimeters, `-Z` forward, material pass names, and shader graphs do not enter this 2D Unity pipeline. Unity 2D gameplay lives in XY, with SpriteRenderer facing the camera along Z.

## Input and controller behavior

The project already has Input System 1.19 and generic `Gamepad`/`Joystick` schemes. Create the action map from `input_action_spec.json`. Use `PlayerInput` so Unity assigns the active device and control scheme. The callback may read the value during the callback and copy it to a plain `Vector2`/bool intent field; never store an `InputValue` reference for later use.

An 8BitDo controller can report as XInput Gamepad, Switch-style Gamepad, or generic Joystick depending on its mode. Use Input Debugger and interactive rebinding instead of hard-coding one model.

## Testing targets

Edit Mode:

- anchor scoring and line-of-sight rejection
- state transition priority
- jump buffer/coyote timers
- normalized projection coordinates to world conversion
- audio-event lookup and variation selection

Play Mode:

- attach/release preserves momentum
- rope length stays within limits
- no tunneling through thin frame colliders
- one-way shelves behave correctly
- animation follows gameplay state without root motion
- calibration geometry reloads consistently

Manual installation:

- projector black level and color visibility
- collider alignment on all four corners of each physical object
- 8BitDo button/stick mapping
- latency at target display mode
- safe reset/calibration controls from across the room
