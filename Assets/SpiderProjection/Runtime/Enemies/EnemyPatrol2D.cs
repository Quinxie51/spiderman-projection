using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyPatrol2D : MonoBehaviour
    {
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckDistance = 0.6f;
        [SerializeField] private float wallCheckDistance = 0.15f;
        [SerializeField] private SpriteRenderer spriteToFlip;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private int direction = -1;
        private Vector2 startPosition;
        private PlayerBrain player;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            bodyCollider = GetComponent<Collider2D>();
            spriteToFlip ??= GetComponentInChildren<SpriteRenderer>();
            startPosition = body.position;
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerBrain>();
            if (player != null)
            {
                player.Restarted += Restart;
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.Restarted -= Restart;
            }
        }

        private void Restart()
        {
            direction = -1;
            body.position = startPosition;
            transform.position = startPosition;
        }

        private void FixedUpdate()
        {
            Bounds bounds = bodyCollider.bounds;

            Vector2 groundOrigin = new Vector2(
                bounds.center.x + direction * (bounds.extents.x + 0.05f),
                bounds.min.y + 0.05f);
            bool groundAhead = Physics2D.Raycast(groundOrigin, Vector2.down, groundCheckDistance, groundMask);

            Vector2 wallOrigin = new Vector2(
                bounds.center.x + direction * (bounds.extents.x + 0.02f),
                bounds.center.y);
            bool wallAhead = Physics2D.Raycast(wallOrigin, new Vector2(direction, 0f), wallCheckDistance, groundMask);

            if (!groundAhead || wallAhead)
            {
                direction = -direction;
            }

            body.MovePosition(body.position + new Vector2(direction * speed * Time.fixedDeltaTime, 0f));
            if (spriteToFlip != null)
            {
                spriteToFlip.flipX = direction < 0;
            }
        }
    }
}
