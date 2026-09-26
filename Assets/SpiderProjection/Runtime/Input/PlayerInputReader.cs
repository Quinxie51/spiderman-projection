using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SpiderProjection.Runtime
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction aimAction;
        private InputAction jumpAction;
        private InputAction swingAction;
        private InputAction rollAction;
        private InputAction shootAction;
        private InputAction pauseAction;
        private InputAction resetAction;
        private InputAction calibrationAction;

        private bool swingPressed;
        private bool swingReleased;
        private bool rollPressed;
        private bool shootPressed;
        private bool pausePressed;
        private bool resetPressed;
        private bool calibrationPressed;
        private float jumpPressedAt = float.NegativeInfinity;

        public Vector2 Move { get; private set; }
        public Vector2 Aim { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SwingHeld { get; private set; }
        public string ActiveControlScheme { get; private set; } = "Unknown";

        public event Action<string> ControlsChanged;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            InputActionMap gameplay = playerInput.actions?.FindActionMap("Gameplay", true);
            moveAction = gameplay.FindAction("Move", true);
            aimAction = gameplay.FindAction("Aim", true);
            jumpAction = gameplay.FindAction("Jump", true);
            swingAction = gameplay.FindAction("Swing", true);
            rollAction = gameplay.FindAction("Roll", true);
            shootAction = gameplay.FindAction("Shoot", true);
            pauseAction = gameplay.FindAction("Pause", true);
            resetAction = gameplay.FindAction("Reset", true);
            calibrationAction = gameplay.FindAction("ToggleCalibration", true);

            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
            aimAction.performed += OnAim;
            aimAction.canceled += OnAim;
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;
            swingAction.performed += OnSwingPerformed;
            swingAction.canceled += OnSwingCanceled;
            rollAction.performed += _ => rollPressed = true;
            shootAction.performed += _ => shootPressed = true;
            pauseAction.performed += _ => pausePressed = true;
            resetAction.performed += _ => resetPressed = true;
            calibrationAction.performed += _ => calibrationPressed = true;

            playerInput.onControlsChanged += OnControlsChanged;
            OnControlsChanged(playerInput);
        }

        private void Update()
        {
            if (IsCalibrationChordHeld())
            {
                pausePressed = false;
                resetPressed = false;
            }
        }

        private void OnDestroy()
        {
            if (moveAction == null)
            {
                return;
            }

            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            aimAction.performed -= OnAim;
            aimAction.canceled -= OnAim;
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
            swingAction.performed -= OnSwingPerformed;
            swingAction.canceled -= OnSwingCanceled;
        }

        public bool ConsumeJump(float now, float bufferDuration)
        {
            if (now - jumpPressedAt > Mathf.Max(0f, bufferDuration))
            {
                return false;
            }

            jumpPressedAt = float.NegativeInfinity;
            return true;
        }

        public bool ConsumeSwingPressed()
        {
            return ConsumeFlag(ref swingPressed);
        }

        public bool ConsumeSwingReleased()
        {
            return ConsumeFlag(ref swingReleased);
        }

        public bool ConsumeRollPressed()
        {
            return ConsumeFlag(ref rollPressed);
        }

        public bool ConsumeShootPressed()
        {
            return ConsumeFlag(ref shootPressed);
        }

        public bool ConsumePausePressed()
        {
            return ConsumeFlag(ref pausePressed);
        }

        public bool ConsumeResetPressed()
        {
            return ConsumeFlag(ref resetPressed);
        }

        public bool ConsumeCalibrationPressed()
        {
            return ConsumeFlag(ref calibrationPressed);
        }

        private static bool ConsumeFlag(ref bool flag)
        {
            bool value = flag;
            flag = false;
            return value;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Move = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
        }

        private void OnAim(InputAction.CallbackContext context)
        {
            Aim = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpHeld = true;
            jumpPressedAt = Time.time;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpHeld = false;
        }

        private void OnSwingPerformed(InputAction.CallbackContext context)
        {
            SwingHeld = true;
            swingPressed = true;
        }

        private void OnSwingCanceled(InputAction.CallbackContext context)
        {
            SwingHeld = false;
            swingReleased = true;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            ActiveControlScheme = string.IsNullOrEmpty(input.currentControlScheme)
                ? input.devices.Count > 0 ? input.devices[0].layout : "Unknown"
                : input.currentControlScheme;
            ControlsChanged?.Invoke(ActiveControlScheme);
        }

        private bool IsCalibrationChordHeld()
        {
            if (playerInput == null)
            {
                return false;
            }

            foreach (InputDevice device in playerInput.devices)
            {
                if (device is Gamepad gamepad &&
                    gamepad.selectButton.isPressed &&
                    gamepad.startButton.isPressed)
                {
                    return true;
                }

                if (device is Joystick joystick)
                {
                    ButtonControl button8 = joystick.allControls
                        .OfType<ButtonControl>()
                        .FirstOrDefault(control => control.name == "button8");
                    ButtonControl button9 = joystick.allControls
                        .OfType<ButtonControl>()
                        .FirstOrDefault(control => control.name == "button9");
                    if (button8 != null && button9 != null &&
                        button8.isPressed && button9.isPressed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
