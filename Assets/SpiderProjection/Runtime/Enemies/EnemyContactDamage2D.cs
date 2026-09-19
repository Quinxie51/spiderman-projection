using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyContactDamage2D : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            other.GetComponentInParent<PlayerBrain>()?.Die();
        }
    }
}
