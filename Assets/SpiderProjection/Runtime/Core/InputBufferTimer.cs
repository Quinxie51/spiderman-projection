using UnityEngine;

namespace SpiderProjection.Runtime
{
    [System.Serializable]
    public struct InputBufferTimer
    {
        [SerializeField] private float expiresAt;

        public bool IsBuffered(float now)
        {
            return expiresAt > 0f && now <= expiresAt;
        }

        public void Buffer(float now, float duration)
        {
            expiresAt = now + Mathf.Max(0f, duration);
        }

        public bool Consume(float now)
        {
            if (!IsBuffered(now))
            {
                return false;
            }

            expiresAt = 0f;
            return true;
        }

        public void Clear()
        {
            expiresAt = 0f;
        }
    }
}
