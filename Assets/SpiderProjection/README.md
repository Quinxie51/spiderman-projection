# Spider Projection asset pack

This folder is a Unity-ready vertical-slice pack for a 2D projection-mapped wall-crawling and web-swinging game. The runtime stand-in is the original **Crimson Crawler** design so the generated pack does not copy the downloaded KidKinobi sprites. The private fan project can rename or replace the character later if the appropriate character-art rights are available.

## Contents

- `Art/Character`: 21 transparent 64×64-cell animation strips, an 89-frame atlas, and a slicing manifest.
- `Art/Environment`: picture frames, shelves, and other visible demo obstacles. Real installation geometry should normally use invisible colliders instead.
- `Art/VFX`: web muzzle/attach, dust, impact, swing, danger, landing, sparkle, and anchor-debug sprites.
- `Art/UI`: generic keyboard/gamepad glyph atlas.
- `Art/Projection`: 1920×1080 calibration, safe-area, and blackout images.
- `Audio/SFX`: 30 synthesized movement, web, impact, feedback, and UI cues.
- `Audio/Music`: projection-idle bed, action theme, and victory stinger.
- `Documentation`: architecture, import settings, state machine/input/audio specs, example wall layout, and the Unity MCP implementation handoff.
- `../../ProjectTools/AssetGeneration`: deterministic visual/audio regeneration sources and asset validation.

The full file-level inventory, dimensions, hashes, audio durations, and provenance are in `Documentation/asset_manifest.json`.

## Unity import defaults

Pixel art:

- Texture Type: `Sprite (2D and UI)`
- Pixels Per Unit: `32`
- Filter Mode: `Point`
- Compression: `None`
- Generate Mip Maps: off
- Wrap Mode: `Clamp`
- Alpha Is Transparency: on
- Character strips: Multiple, grid-slice at `64×64`
- Character atlas: Multiple, grid-slice at `64×64`
- Obstacle atlas: Multiple, grid-slice at `128×128`
- VFX atlas: Multiple, grid-slice at `64×64`
- Input glyph atlas: Multiple, grid-slice at `96×64`
- Pivot: normalized `(0.5, 0.32)` for character frames

Audio:

- SFX: Decompress On Load, Preload Audio Data on, Force To Mono off
- Music: Streaming, Vorbis, quality around 0.7, Force To Mono off
- All source WAVs are 44.1 kHz stereo

Rendering:

- Use `Sprite-Unlit-Default` for the character, props, VFX, and calibration view so projected colors do not depend on scene lights.
- Use a 2D unlit line material for the web.
- Set the runtime camera clear color to pure black.
- Do not create Lens Studio materials. These PNG/WAV assets transfer directly; there is no Lens shader conversion step.

## Preview

Open `Documentation/Previews/crimson_crawler_contact_sheet.png` or `crimson_crawler_animation_reel.gif`. GIFs are previews only; Unity should animate the PNG strips.

## Regeneration

From the project root:

```bash
python3 ProjectTools/AssetGeneration/generate_visual_assets.py
python3 ProjectTools/AssetGeneration/generate_asset_manifest.py
```

The copied CLAD audio scripts retain their original Lens-project output path. Update `PROJECT_ASSETS_SFX` only if you intentionally regenerate audio somewhere else, then copy the resulting WAVs back into this pack.

## Implemented Unity scenes and controls

- `Scenes/WallDemo.unity`: playable projection-wall vertical slice with visible demo props, separate invisible physics, one-way tops, and 12 web anchors.
- `Scenes/ProjectionCalibration.unity`: 1920×1080 calibration grid/safe-area view with the same normalized collider and anchor layout.
- Move: WASD, arrow keys, left stick, d-pad, or generic joystick stick.
- Jump: Space / gamepad south button.
- Swing: left mouse or F / right trigger; release to detach.
- Roll: left Ctrl or C / gamepad east button.
- Pause: Escape / Start.
- Reset: hold R, Select, or joystick button 8 for 0.5 seconds.
- Calibration: hold F1, Select+Start, or joystick buttons 8+9 for 0.75 seconds.
- Blackout: F2.
- Real-wall mode: F3 hides the generated frame/shelf sprites while retaining colliders and anchors.

`Settings/ProjectionLayout_Example.asset` and `Documentation/projection_layout.example.json` are examples only. Replace their normalized rectangles with measured room values before installation.
