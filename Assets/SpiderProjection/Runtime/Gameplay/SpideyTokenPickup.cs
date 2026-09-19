using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class SpideyTokenPickup : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

        private Collider2D triggerCollider;
        private Vector3 origin;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
            origin = transform.position;
        }

        private void Update()
        {
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag) || pickupSound == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }
    }
}
