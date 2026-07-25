using UnityEngine;

namespace SpiderProjection.Runtime
{
    [CreateAssetMenu(menuName = "Spider Projection/Player Tuning", fileName = "PlayerTuning")]
    public sealed class PlayerTuning : ScriptableObject
    {
        [Header("Run")]
        public float runSpeed = 7f;
        public float groundAcceleration = 52f;
        public float groundDeceleration = 64f;
        public float airAcceleration = 30f;

        [Header("Jump and fall")]
        public float jumpSpeed = 11f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float normalGravityScale = 3.5f;
        public float jumpCutGravityScale = 5.5f;
        public float maxFallSpeed = 18f;
        public float apexThreshold = 0.6f;
        public float hardLandingSpeed = 13f;

        [Header("Roll and skid")]
        public float rollSpeed = 9f;
        public float rollDuration = 0.43f;
        public float skidThreshold = 5.5f;

        [Header("Swing")]
        public float swingSearchRadius = 8f;
        public float swingMinRopeLength = 1.4f;
        public float swingMaxRopeLength = 8.5f;
        public float swingPumpForce = 18f;
        public float swingReelSpeed = 2.4f;
        public float swingReleaseBoost = 1.25f;

        [Header("Wall traversal")]
        public float wallCrawlSpeed = 3f;
        public float wallSlideSpeed = 2.5f;
        public float wallJumpHorizontalSpeed = 7.5f;
        public float ledgeClimbDuration = 0.25f;

        [Header("Sensors")]
        public Vector2 groundProbeSize = new Vector2(0.55f, 0.12f);
        public Vector2 wallProbeSize = new Vector2(0.12f, 0.8f);
        public Vector2 ledgeProbeSize = new Vector2(0.12f, 0.2f);
    }
}
