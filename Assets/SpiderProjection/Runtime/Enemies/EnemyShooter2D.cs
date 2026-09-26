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

        private PlayerBrain brain;
        private Transform player;
        private float cooldown;

        private void Start()
        {
            brain = FindFirstObjectByType<PlayerBrain>();
            player = brain != null ? brain.transform : null;
            cooldown = fireInterval * 0.5f;
            if (brain != null)
            {
                brain.Restarted += Restart;
            }
        }

        private void OnDestroy()
        {
            if (brain != null)
            {
                brain.Restarted -= Restart;
            }
        }

        private void Restart()
        {
            cooldown = fireInterval * 0.5f;
            foreach (EnemyProjectile2D projectile in FindObjectsByType<EnemyProjectile2D>(FindObjectsSortMode.None))
            {
                Destroy(projectile.gameObject);
            }
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
