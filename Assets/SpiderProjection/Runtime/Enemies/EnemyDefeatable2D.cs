using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class EnemyDefeatable2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private int hitsToDefeat = 3;
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float flashDuration = 0.25f;
        [SerializeField] private float flashInterval = 0.05f;
        [SerializeField] private float popDuration = 0.6f;
        [SerializeField] private float popHeight = 0.8f;
        [SerializeField] private float spinDegrees = 540f;

        private PlayerBrain player;
        private Behaviour[] behaviours;
        private Collider2D bodyCollider;
        private Vector3 startPosition;
        private Vector3 visualStartPosition;
        private Vector3 visualStartScale;
        private Quaternion visualStartRotation;
        private Color visualStartColor;
        private float timer;
        private float hitFlashTimer;
        private int hits;
        private bool defeated;

        private Vector2 lastPosition;

        public bool IsDefeated => defeated;
        public Vector2 Velocity { get; private set; }
        public Vector2 AimPoint => bodyCollider.bounds.center;

        private void Awake()
        {
            visual ??= GetComponentInChildren<SpriteRenderer>();
            bodyCollider = GetComponent<Collider2D>();
            startPosition = transform.position;
            visualStartPosition = visual.transform.localPosition;
            visualStartScale = visual.transform.localScale;
            visualStartRotation = visual.transform.localRotation;
            visualStartColor = visual.color;
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerBrain>();
            if (player != null)
            {
                player.Restarted += Restore;
            }
            lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            Vector2 position = transform.position;
            Velocity = (position - lastPosition) / Time.fixedDeltaTime;
            lastPosition = position;
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.Restarted -= Restore;
            }
        }

        public void TakeHit()
        {
            if (defeated)
            {
                return;
            }

            hits++;
            if (hits >= hitsToDefeat)
            {
                Defeat();
                return;
            }

            hitFlashTimer = hitFlashDuration;
            visual.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        public void Defeat()
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            timer = 0f;
            bodyCollider.enabled = false;
            behaviours = new Behaviour[]
            {
                GetComponent<EnemyPatrol2D>(),
                GetComponent<EnemyFlyChase2D>(),
                GetComponent<EnemyShooter2D>(),
                GetComponent<EnemyContactDamage2D>(),
            };
            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!defeated)
            {
                if (hitFlashTimer > 0f)
                {
                    hitFlashTimer -= Time.deltaTime;
                    if (hitFlashTimer <= 0f)
                    {
                        visual.color = visualStartColor;
                    }
                }
                return;
            }

            timer += Time.deltaTime;
            Transform visualTransform = visual.transform;

            if (timer < flashDuration)
            {
                bool on = Mathf.FloorToInt(timer / flashInterval) % 2 == 0;
                visual.color = on ? Color.white : new Color(1f, 0.2f, 0.2f, 1f);
                return;
            }

            float t = Mathf.Clamp01((timer - flashDuration) / popDuration);
            visualTransform.localPosition = visualStartPosition + Vector3.up * (Mathf.Sin(t * Mathf.PI) * popHeight);
            visualTransform.localRotation = visualStartRotation * Quaternion.Euler(0f, 0f, spinDegrees * t);
            visualTransform.localScale = visualStartScale * (1f - 0.6f * t);
            Color faded = visualStartColor;
            faded.a = 1f - t;
            visual.color = faded;

            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Restore()
        {
            defeated = false;
            hits = 0;
            hitFlashTimer = 0f;
            transform.position = startPosition;
            lastPosition = startPosition;
            Velocity = Vector2.zero;
            visual.transform.localPosition = visualStartPosition;
            visual.transform.localRotation = visualStartRotation;
            visual.transform.localScale = visualStartScale;
            visual.color = visualStartColor;
            bodyCollider.enabled = true;
            if (behaviours != null)
            {
                foreach (Behaviour behaviour in behaviours)
                {
                    if (behaviour != null)
                    {
                        behaviour.enabled = true;
                    }
                }
            }
            gameObject.SetActive(true);
        }
    }
}
