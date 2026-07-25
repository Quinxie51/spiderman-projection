# Projection setup

## MVP setup

Use the projector as a normal PC monitor and keep the output at one fixed resolution. Full-screen the Unity player on that display. The runtime camera should be orthographic and clear to pure black; black pixels reduce stray projected light and let the physical wall remain visible.

Do not begin by writing a custom projection shader. First prove the game at a fixed 1920×1080 output:

1. Display `Art/Projection/projection_calibration_1920x1080.png`.
2. Physically place and focus the projector.
3. Use the projector's keystone/corner correction only enough to square the wall plane.
4. In a dedicated `ProjectionCalibration` scene, drag collider handles until they align with each real frame, shelf, floor edge, and wall boundary.
5. Switch on anchor-debug sprites and web lines; verify every automatic target ends on a real object.
6. Turn off all debug guides and visible demo props.
7. Save the room layout separately from the gameplay scene.

## Coordinate model

Store measured surfaces in normalized output coordinates `(0..1, 0..1)`, origin bottom-left. At runtime, convert them into the orthographic camera's world rectangle. This makes the layout independent of PPU and keeps a measured wall portable across gameplay tuning.

Keep three spaces separate:

- output pixels: calibration UI and screenshots
- normalized projection coordinates: saved physical measurements
- Unity world coordinates: colliders, player, physics, and camera

## Software corner pin

Only add software warping if the fixed projection cannot be squared physically:

```text
GameplayCamera → RenderTexture → ProjectionPresenter → Output display
```

`ProjectionPresenter` draws the RenderTexture on a four-corner mesh. The mesh warp changes pixels after rendering; it must not change Rigidbody2D positions or collider geometry. Calibrate physics against the final warped output.

## Physical occlusion

A projector cannot make a virtual sprite truly pass behind a protruding physical frame without a mask. For believable occlusion, draw a black polygon over the part that should disappear behind the real object, or create per-surface occlusion masks in the final presentation pass. Start without this and add it after movement works.

## Runtime safety controls

- hold `F1` for 0.75 seconds, or hold Select+Start / joystick buttons 8+9: calibration view
- `F2`: projector blackout
- `F3`: real-wall mode (hide demo sprites while preserving colliders and anchors)
- `R` or held Select: reset player
- escape/Start: pause
- persist last-known projector display and resolution

Use a visible confirmation countdown before saving a new calibration so an accidental controller chord cannot replace a working layout.
