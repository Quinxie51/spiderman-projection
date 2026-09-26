using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerProjectile2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private LayerMask obstacleMask;

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
            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            body.linearVelocity = normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EnemyDefeatable2D enemy = other.GetComponentInParent<EnemyDefeatable2D>();
            if (enemy != null)
            {
                if (!enemy.IsDefeated)
                {
                    enemy.TakeHit();
                    Destroy(gameObject);
                }
                return;
            }

            if (((1 << other.gameObject.layer) & obstacleMask) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
