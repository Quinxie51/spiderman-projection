using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class PlayerSensors2D : MonoBehaviour
    {
        [SerializeField] private Transform groundProbe;
        [SerializeField] private Transform wallProbe;
        [SerializeField] private Transform ledgeProbe;
        [SerializeField] private LayerMask worldMask = ~0;

        private Collider2D bodyCollider;
        private PlayerTuning tuning;

        public bool IsGrounded { get; private set; }
        public bool HasWall { get; private set; }
        public bool HasLedge { get; private set; }
        public int WallDirection { get; private set; }
        public Collider2D GroundCollider { get; private set; }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            groundProbe ??= transform.Find("GroundProbe");
            wallProbe ??= transform.Find("WallProbe");
            ledgeProbe ??= transform.Find("LedgeProbe");
        }

        public void Initialize(PlayerTuning playerTuning, LayerMask collisionMask)
        {
            bodyCollider ??= GetComponent<Collider2D>();
            tuning = playerTuning;
            worldMask = collisionMask;
        }

        public void Sample(int facingDirection)
        {
            if (tuning == null)
            {
                return;
            }

            Bounds bounds = bodyCollider.bounds;
            Vector2 groundPosition = groundProbe != null
                ? groundProbe.position
                : new Vector2(bounds.center.x, bounds.min.y - 0.04f);
            GroundCollider = Physics2D.OverlapBox(groundPosition, tuning.groundProbeSize, 0f, worldMask);
            IsGrounded = GroundCollider != null && GroundCollider != bodyCollider;

            int direction = facingDirection == 0 ? 1 : facingDirection;
            Vector2 wallPosition = wallProbe != null
                ? wallProbe.position
                : new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.05f), bounds.center.y);
            Collider2D wallHit = Physics2D.OverlapBox(wallPosition, tuning.wallProbeSize, 0f, worldMask);
            HasWall = wallHit != null && wallHit != bodyCollider;
            WallDirection = HasWall ? direction : 0;

            Vector2 ledgePosition = ledgeProbe != null
                ? ledgeProbe.position
                : new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.05f), bounds.max.y + 0.12f);
            Collider2D ledgeHit = Physics2D.OverlapBox(ledgePosition, tuning.ledgeProbeSize, 0f, worldMask);
            HasLedge = HasWall && ledgeHit == null;
        }

        private void OnDrawGizmosSelected()
        {
            if (tuning == null)
            {
                return;
            }

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundProbe != null ? groundProbe.position : transform.position, tuning.groundProbeSize);
            Gizmos.color = HasWall ? Color.cyan : Color.gray;
            Gizmos.DrawWireCube(wallProbe != null ? wallProbe.position : transform.position, tuning.wallProbeSize);
            Gizmos.color = HasLedge ? Color.yellow : Color.gray;
            Gizmos.DrawWireCube(ledgeProbe != null ? ledgeProbe.position : transform.position, tuning.ledgeProbeSize);
        }
    }
}
