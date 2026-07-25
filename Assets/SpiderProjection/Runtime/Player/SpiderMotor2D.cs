using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SpiderMotor2D : MonoBehaviour
    {
        private Rigidbody2D body;
        private PlayerInputReader input;
        private PlayerSensors2D sensors;
        private PlayerTuning tuning;
        private float coyoteRemaining;
        private float rollRemaining;
        private float landRemaining;
        private float jumpStartRemaining;
        private bool wasGrounded;
        private float previousVerticalSpeed;
        private int rollDirection = 1;

        public bool LastLandingWasHard { get; private set; }
        public bool IsRolling => rollRemaining > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
            sensors = GetComponent<PlayerSensors2D>();
        }

        public void Initialize(PlayerTuning playerTuning)
        {
            body ??= GetComponent<Rigidbody2D>();
            input ??= GetComponent<PlayerInputReader>();
            sensors ??= GetComponent<PlayerSensors2D>();
            tuning = playerTuning;
            body.gravityScale = tuning.normalGravityScale;
        }

        public void StartRoll(int facing)
        {
            if (tuning == null || !sensors.IsGrounded)
            {
                return;
            }

            rollDirection = facing == 0 ? 1 : facing;
            if (Mathf.Abs(input.Move.x) > 0.15f)
            {
                rollDirection = input.Move.x > 0f ? 1 : -1;
            }
            rollRemaining = tuning.rollDuration;
        }

        public GameplayState PhysicsTick(float fixedDeltaTime, ref int facing)
        {
            if (tuning == null)
            {
                return GameplayState.Idle;
            }

            bool grounded = sensors.IsGrounded;
            if (grounded)
            {
                coyoteRemaining = tuning.coyoteTime;
            }
            else
            {
                coyoteRemaining = Mathf.Max(0f, coyoteRemaining - fixedDeltaTime);
            }

            if (!wasGrounded && grounded)
            {
                LastLandingWasHard = Mathf.Abs(previousVerticalSpeed) >= tuning.hardLandingSpeed;
                landRemaining = LastLandingWasHard ? 0.16f : 0.09f;
            }

            if (rollRemaining > 0f)
            {
                rollRemaining = Mathf.Max(0f, rollRemaining - fixedDeltaTime);
                body.gravityScale = tuning.normalGravityScale;
                body.linearVelocity = new Vector2(rollDirection * tuning.rollSpeed, body.linearVelocity.y);
                wasGrounded = grounded;
                previousVerticalSpeed = body.linearVelocity.y;
                return GameplayState.Roll;
            }

            Vector2 velocity = body.linearVelocity;
            float move = Mathf.Abs(input.Move.x) < 0.12f ? 0f : input.Move.x;
            if (move != 0f)
            {
                facing = move > 0f ? 1 : -1;
            }

            float targetSpeed = move * tuning.runSpeed;
            float acceleration = grounded
                ? (Mathf.Abs(targetSpeed) > 0.01f ? tuning.groundAcceleration : tuning.groundDeceleration)
                : tuning.airAcceleration;
            bool skidding = grounded &&
                            move != 0f &&
                            Mathf.Sign(move) != Mathf.Sign(velocity.x) &&
                            Mathf.Abs(velocity.x) >= tuning.skidThreshold;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * fixedDeltaTime);

            if (input.ConsumeJump(Time.time, tuning.jumpBufferTime) && (grounded || coyoteRemaining > 0f))
            {
                velocity.y = tuning.jumpSpeed;
                coyoteRemaining = 0f;
                jumpStartRemaining = 0.05f;
                LastLandingWasHard = false;
            }

            if (!input.JumpHeld && velocity.y > 0f)
            {
                body.gravityScale = tuning.jumpCutGravityScale;
            }
            else
            {
                body.gravityScale = tuning.normalGravityScale;
            }

            velocity.y = Mathf.Max(velocity.y, -tuning.maxFallSpeed);
            body.linearVelocity = velocity;
            previousVerticalSpeed = velocity.y;
            wasGrounded = grounded;

            if (jumpStartRemaining > 0f)
            {
                jumpStartRemaining = Mathf.Max(0f, jumpStartRemaining - fixedDeltaTime);
                return GameplayState.JumpStart;
            }

            if (landRemaining > 0f)
            {
                landRemaining = Mathf.Max(0f, landRemaining - fixedDeltaTime);
                return GameplayState.Land;
            }

            if (grounded)
            {
                if (skidding)
                {
                    return GameplayState.Skid;
                }
                return Mathf.Abs(velocity.x) > 0.2f ? GameplayState.Run : GameplayState.Idle;
            }

            if (velocity.y > tuning.apexThreshold)
            {
                return GameplayState.JumpRise;
            }
            if (Mathf.Abs(velocity.y) <= tuning.apexThreshold)
            {
                return GameplayState.Apex;
            }
            return GameplayState.Fall;
        }

        public void ApplyWallJump(int wallDirection)
        {
            body.gravityScale = tuning.normalGravityScale;
            body.linearVelocity = new Vector2(-wallDirection * tuning.wallJumpHorizontalSpeed, tuning.jumpSpeed);
        }

        public void ResetMotion()
        {
            rollRemaining = 0f;
            landRemaining = 0f;
            jumpStartRemaining = 0f;
            coyoteRemaining = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = tuning != null ? tuning.normalGravityScale : 1f;
        }
    }
}
