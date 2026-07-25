using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D))]
    public sealed class SwingController2D : MonoBehaviour
    {
        private Rigidbody2D body;
        private DistanceJoint2D joint;
        private PlayerInputReader input;
        private PlayerTuning tuning;

        public bool IsAttached => joint != null && joint.enabled;
        public WebAnchor2D CurrentAnchor { get; private set; }
        public Vector2 AnchorPosition => CurrentAnchor != null ? CurrentAnchor.Position : joint.connectedAnchor;
        public float RopeLength => joint != null ? joint.distance : 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            joint = GetComponent<DistanceJoint2D>();
            input = GetComponent<PlayerInputReader>();
            joint.enabled = false;
            joint.autoConfigureDistance = false;
            joint.enableCollision = false;
        }

        public void Initialize(PlayerTuning playerTuning)
        {
            body ??= GetComponent<Rigidbody2D>();
            joint ??= GetComponent<DistanceJoint2D>();
            input ??= GetComponent<PlayerInputReader>();
            joint.enabled = false;
            joint.autoConfigureDistance = false;
            joint.enableCollision = false;
            tuning = playerTuning;
        }

        public bool Attach(WebAnchor2D anchor)
        {
            if (anchor == null || tuning == null)
            {
                return false;
            }

            CurrentAnchor = anchor;
            joint.connectedBody = null;
            joint.connectedAnchor = anchor.Position;
            joint.distance = Mathf.Clamp(
                Vector2.Distance(body.position, anchor.Position),
                tuning.swingMinRopeLength,
                tuning.swingMaxRopeLength);
            joint.enabled = true;
            return true;
        }

        public void PhysicsTick(float fixedDeltaTime)
        {
            if (!IsAttached || tuning == null)
            {
                return;
            }

            if (CurrentAnchor == null || !CurrentAnchor.GameplayEnabled)
            {
                Detach(false);
                return;
            }

            joint.connectedAnchor = CurrentAnchor.Position;
            Vector2 rope = body.position - CurrentAnchor.Position;
            if (rope.sqrMagnitude > 0.001f)
            {
                Vector2 tangent = new Vector2(-rope.y, rope.x).normalized;
                float direction = Mathf.Sign(Vector2.Dot(tangent, Vector2.right));
                body.AddForce(tangent * (input.Move.x * direction) * tuning.swingPumpForce, ForceMode2D.Force);
            }

            float requestedDistance = joint.distance - input.Move.y * tuning.swingReelSpeed * fixedDeltaTime;
            joint.distance = Mathf.Clamp(requestedDistance, tuning.swingMinRopeLength, tuning.swingMaxRopeLength);
        }

        public Vector2 Detach(bool applyBoost)
        {
            Vector2 preservedVelocity = body.linearVelocity;
            if (joint != null)
            {
                joint.enabled = false;
            }
            CurrentAnchor = null;

            if (applyBoost && tuning != null)
            {
                body.linearVelocity = preservedVelocity * tuning.swingReleaseBoost;
            }
            return preservedVelocity;
        }
    }
}
