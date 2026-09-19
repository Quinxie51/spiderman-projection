using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyProjectile2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private string playerTag = "Player";

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            GetComponent<Collider2D>().isTrigger = true;
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector2 direction, float speed)
        {
            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
            body.linearVelocity = normalized * speed;
            transform.right = normalized;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                other.GetComponentInParent<PlayerBrain>()?.Die();
                Destroy(gameObject);
                return;
            }

            if (((1 << other.gameObject.layer) & obstacleMask) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
