using System.Collections;
using NUnit.Framework;
using SpiderProjection.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SpiderProjection.Tests.PlayMode
{
    public sealed class SpiderProjectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Jump_UsesBufferedInputAndConfiguredSpeed()
        {
            PlayerTuning tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            InputActionAsset actions = CreateInputActions();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("SpiderProjectionTestKeyboard");
            GameObject floor = new GameObject("TestFloor");
            GameObject player = new GameObject("TestPlayer");
            player.SetActive(false);

            try
            {
                BoxCollider2D floorCollider = floor.AddComponent<BoxCollider2D>();
                floorCollider.size = new Vector2(8f, 0.2f);
                floor.transform.position = new Vector3(0f, -0.1f, 0f);

                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
                capsule.size = new Vector2(0.82f, 1.65f);
                capsule.offset = new Vector2(0f, 0.25f);
                PlayerInput playerInput = player.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                player.AddComponent<PlayerInputReader>();
                PlayerSensors2D sensors = player.AddComponent<PlayerSensors2D>();
                SpiderMotor2D motor = player.AddComponent<SpiderMotor2D>();
                player.transform.position = new Vector3(0f, 0.58f, 0f);
                player.SetActive(true);
                playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard);
                sensors.Initialize(tuning, ~0);
                motor.Initialize(tuning);

                Physics2D.SyncTransforms();
                sensors.Sample(1);
                Assert.That(sensors.IsGrounded, Is.True, "Test player must begin grounded.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null;
                sensors.Sample(1);
                int facing = 1;
                GameplayState state = motor.PhysicsTick(Time.fixedDeltaTime, ref facing);

                Assert.That(body.linearVelocity.y, Is.EqualTo(tuning.jumpSpeed).Within(0.05f));
                Assert.That(state, Is.EqualTo(GameplayState.JumpStart));
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(floor);
                Object.Destroy(actions);
                Object.Destroy(tuning);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Swing_AttachAndReleasePreserveAndBoostMomentum()
        {
            PlayerTuning tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            GameObject player = new GameObject("SwingPlayer");
            GameObject anchorObject = new GameObject("SwingAnchor");

            try
            {
                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                DistanceJoint2D joint = player.AddComponent<DistanceJoint2D>();
                SwingController2D swing = player.AddComponent<SwingController2D>();
                WebAnchor2D anchor = anchorObject.AddComponent<WebAnchor2D>();
                anchorObject.transform.position = new Vector3(4f, 3f, 0f);
                swing.Initialize(tuning);

                Vector2 initialVelocity = new Vector2(3f, 4f);
                body.linearVelocity = initialVelocity;
                Assert.That(swing.Attach(anchor), Is.True);
                Assert.That(swing.IsAttached, Is.True);
                Assert.That(body.linearVelocity, Is.EqualTo(initialVelocity).Using(Vector2EqualityComparer.Instance));

                Vector2 preserved = swing.Detach(true);
                Assert.That(preserved, Is.EqualTo(initialVelocity).Using(Vector2EqualityComparer.Instance));
                Assert.That(
                    body.linearVelocity,
                    Is.EqualTo(initialVelocity * tuning.swingReleaseBoost).Using(Vector2EqualityComparer.Instance));
                Assert.That(swing.IsAttached, Is.False);
                yield return null;
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(anchorObject);
                Object.Destroy(tuning);
            }
        }

        [UnityTest]
        public IEnumerator Swing_RopeLengthClampsToConfiguredLimits()
        {
            PlayerTuning tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            GameObject player = new GameObject("RopePlayer");
            GameObject anchorObject = new GameObject("RopeAnchor");

            try
            {
                player.AddComponent<Rigidbody2D>().gravityScale = 0f;
                player.AddComponent<DistanceJoint2D>();
                SwingController2D swing = player.AddComponent<SwingController2D>();
                WebAnchor2D anchor = anchorObject.AddComponent<WebAnchor2D>();
                swing.Initialize(tuning);

                anchorObject.transform.position = new Vector3(100f, 0f, 0f);
                Assert.That(swing.Attach(anchor), Is.True);
                Assert.That(swing.RopeLength, Is.EqualTo(tuning.swingMaxRopeLength).Within(0.001f));

                swing.Detach(false);
                anchorObject.transform.position = new Vector3(0.05f, 0f, 0f);
                Assert.That(swing.Attach(anchor), Is.True);
                Assert.That(swing.RopeLength, Is.EqualTo(tuning.swingMinRopeLength).Within(0.001f));
                yield return null;
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(anchorObject);
                Object.Destroy(tuning);
            }
        }

        [UnityTest]
        public IEnumerator OneWaySurface_AllowsUpwardPassageAndCatchesFallingBody()
        {
            GameObject platform = new GameObject("OneWayPlatform");
            GameObject bodyObject = new GameObject("OneWayTestBody");

            try
            {
                BoxCollider2D platformCollider = platform.AddComponent<BoxCollider2D>();
                platformCollider.size = new Vector2(4f, 0.2f);
                PlatformEffector2D effector = platform.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useSideFriction = false;
                platformCollider.usedByEffector = true;

                Rigidbody2D body = bodyObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                CircleCollider2D bodyCollider = bodyObject.AddComponent<CircleCollider2D>();
                bodyCollider.radius = 0.25f;
                body.position = new Vector2(0f, -1f);
                body.linearVelocity = new Vector2(0f, 5f);
                Physics2D.SyncTransforms();

                for (int index = 0; index < 18; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(body.position.y, Is.GreaterThan(0.3f), "Body should pass through from below.");

                body.position = new Vector2(0f, 1.5f);
                body.linearVelocity = Vector2.zero;
                body.gravityScale = 1f;
                Physics2D.SyncTransforms();
                for (int index = 0; index < 80; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(body.position.y, Is.InRange(0.30f, 0.45f), "Falling body should land on the one-way top.");
            }
            finally
            {
                Object.Destroy(platform);
                Object.Destroy(bodyObject);
            }
        }

        [UnityTest]
        public IEnumerator PlayerInput_ChangesBetweenKeyboardAndGamepadSchemes()
        {
            InputActionAsset actions = CreateInputActions();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("SpiderProjectionSchemeKeyboard");
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>("SpiderProjectionSchemeGamepad");
            GameObject inputObject = new GameObject("SchemePlayer");
            inputObject.SetActive(false);

            try
            {
                PlayerInput playerInput = inputObject.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                PlayerInputReader reader = inputObject.AddComponent<PlayerInputReader>();
                inputObject.SetActive(true);
                yield return null;

                playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard);
                yield return null;
                Assert.That(reader.ActiveControlScheme, Is.EqualTo("Keyboard&Mouse"));

                playerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
                yield return null;
                Assert.That(reader.ActiveControlScheme, Is.EqualTo("Gamepad"));
            }
            finally
            {
                Object.Destroy(inputObject);
                Object.Destroy(actions);
                InputSystem.RemoveDevice(gamepad);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator CalibrationChord_HoldsSelectAndStartWithoutPauseOrReset()
        {
            InputActionAsset actions = CreateInputActions();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>("SpiderProjectionChordGamepad");
            GameObject inputObject = new GameObject("ChordPlayer");
            inputObject.SetActive(false);

            try
            {
                PlayerInput playerInput = inputObject.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                PlayerInputReader reader = inputObject.AddComponent<PlayerInputReader>();
                inputObject.SetActive(true);
                playerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
                yield return null;

                GamepadState chord = new GamepadState()
                    .WithButton(GamepadButton.Select)
                    .WithButton(GamepadButton.Start);
                InputSystem.QueueStateEvent(gamepad, chord);
                yield return new WaitForSecondsRealtime(0.85f);
                yield return null;

                Assert.That(reader.ConsumeCalibrationPressed(), Is.True);
                Assert.That(reader.ConsumePausePressed(), Is.False);
                Assert.That(reader.ConsumeResetPressed(), Is.False);
            }
            finally
            {
                Object.Destroy(inputObject);
                Object.Destroy(actions);
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static InputActionAsset CreateInputActions()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2")
                .AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
            gameplay.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2")
                .AddBinding("<Gamepad>/rightStick", groups: "Gamepad");
            AddButton(gameplay, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(gameplay, "Swing", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            AddButton(gameplay, "Roll", "<Keyboard>/c", "<Gamepad>/buttonEast");
            AddButton(gameplay, "WebZip", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            AddButton(gameplay, "Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
            AddButton(gameplay, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
            AddButton(gameplay, "Reset", "<Keyboard>/r", "<Gamepad>/select");
            InputAction calibration = gameplay.AddAction(
                "ToggleCalibration",
                InputActionType.Button,
                interactions: "Hold(duration=0.75)",
                expectedControlLayout: "Button");
            calibration.AddBinding("<Keyboard>/f1", groups: "Keyboard&Mouse");
            calibration.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Gamepad>/select", "Gamepad")
                .With("Binding", "<Gamepad>/start", "Gamepad");
            asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>");
            asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");
            return asset;
        }

        private static void AddButton(
            InputActionMap map,
            string name,
            string keyboardBinding,
            string gamepadBinding)
        {
            InputAction action = map.AddAction(name, InputActionType.Button, expectedControlLayout: "Button");
            action.AddBinding(keyboardBinding, groups: "Keyboard&Mouse");
            action.AddBinding(gamepadBinding, groups: "Gamepad");
        }

        private sealed class Vector2EqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly Vector2EqualityComparer Instance = new Vector2EqualityComparer();

            public new bool Equals(object left, object right)
            {
                return left is Vector2 a && right is Vector2 b && Vector2.Distance(a, b) < 0.001f;
            }

            public int GetHashCode(object value)
            {
                return value.GetHashCode();
            }
        }
    }
}
