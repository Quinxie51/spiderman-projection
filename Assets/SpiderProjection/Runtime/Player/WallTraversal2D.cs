using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WallTraversal2D : MonoBehaviour
    {
        private Rigidbody2D body;
        private PlayerInputReader input;
        private PlayerSensors2D sensors;
        private PlayerTuning tuning;
        private float climbRemaining;

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
        }

        public bool TryPhysicsTick(float fixedDeltaTime, out GameplayState state)
        {
            state = GameplayState.Fall;
            if (tuning == null || sensors.IsGrounded || !sensors.HasWall)
            {
                climbRemaining = 0f;
                return false;
            }

            int wallDirection = sensors.WallDirection;
            bool holdingTowardWall = Mathf.Abs(input.Move.x) > 0.12f &&
                                     Mathf.Sign(input.Move.x) == wallDirection;
            if (!holdingTowardWall && climbRemaining <= 0f)
            {
                return false;
            }

            if (sensors.HasLedge && input.Move.y > 0.2f && climbRemaining <= 0f)
            {
                climbRemaining = tuning.ledgeClimbDuration;
            }

            if (climbRemaining > 0f)
            {
                climbRemaining = Mathf.Max(0f, climbRemaining - fixedDeltaTime);
                body.gravityScale = 0f;
                body.linearVelocity = new Vector2(wallDirection * 1.5f, tuning.wallCrawlSpeed);
                state = climbRemaining > 0f ? GameplayState.LedgeClimb : GameplayState.LedgeGrab;
                return true;
            }

            body.gravityScale = 0f;
            float vertical = Mathf.Abs(input.Move.y) > 0.12f ? input.Move.y * tuning.wallCrawlSpeed : 0f;
            body.linearVelocity = new Vector2(0f, Mathf.Max(vertical, -tuning.wallSlideSpeed));
            state = Mathf.Abs(vertical) > 0.1f ? GameplayState.WallCrawl : GameplayState.WallCling;
            return true;
        }
    }
}
