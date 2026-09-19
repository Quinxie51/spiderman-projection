using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class EnemyShooter2D : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile2D projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireInterval = 2f;
        [SerializeField] private float projectileSpeed = 6f;
        [SerializeField] private float detectionRange = 9f;

        private Transform player;
        private float cooldown;

        private void Start()
        {
            PlayerBrain brain = FindFirstObjectByType<PlayerBrain>();
            player = brain != null ? brain.transform : null;
            cooldown = fireInterval * 0.5f;
        }

        private void Update()
        {
            if (projectilePrefab == null || player == null)
            {
                return;
            }

            cooldown -= Time.deltaTime;
            if (cooldown > 0f)
            {
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector2 toPlayer = player.position - origin;
            if (toPlayer.magnitude > detectionRange)
            {
                return;
            }

            EnemyProjectile2D projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(toPlayer, projectileSpeed);
            cooldown = fireInterval;
        }
    }
}
