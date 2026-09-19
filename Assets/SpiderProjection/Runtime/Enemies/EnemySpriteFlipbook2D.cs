using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class EnemySpriteFlipbook2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 2.5f;

        private float timer;
        private int index;

        private void Awake()
        {
            target ??= GetComponentInChildren<SpriteRenderer>();
            if (target != null && frames != null && frames.Length > 0)
            {
                target.sprite = frames[0];
            }
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0)
            {
                return;
            }

            timer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(0.01f, frameRate);
            if (timer < frameDuration)
            {
                return;
            }

            timer -= frameDuration;
            index = (index + 1) % frames.Length;
            target.sprite = frames[index];
        }
    }
}
