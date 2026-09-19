using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyFlyChase2D : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 2.5f;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 14f;
        [SerializeField] private float flightHeight = 3.5f;
        [SerializeField] private float horizontalOffset = 1.5f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private SpriteRenderer spriteToFlip;

        private Rigidbody2D body;
        private Transform player;
        private float bobTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            spriteToFlip ??= GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            PlayerBrain brain = FindFirstObjectByType<PlayerBrain>();
            player = brain != null ? brain.transform : null;
        }

        private void FixedUpdate()
        {
            bobTimer += Time.fixedDeltaTime * bobSpeed;

            float targetX = player != null
                ? Mathf.Clamp(player.position.x + horizontalOffset, minX, maxX)
                : body.position.x;
            float x = Mathf.MoveTowards(body.position.x, targetX, followSpeed * Time.fixedDeltaTime);
            float y = flightHeight + Mathf.Sin(bobTimer) * bobAmplitude;
            body.MovePosition(new Vector2(x, y));

            if (spriteToFlip != null && player != null)
            {
                spriteToFlip.flipX = player.position.x < body.position.x;
            }
        }
    }
}
