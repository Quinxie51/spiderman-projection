using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class WebTargetResolver2D : MonoBehaviour
    {
        [SerializeField] private LayerMask obstructionMask;

        public void Configure(LayerMask mask)
        {
            obstructionMask = mask;
        }

        public WebAnchor2D Resolve(Vector2 origin, Vector2 aim, int facing, PlayerTuning tuning)
        {
            WebAnchor2D best = null;
            float bestScore = float.NegativeInfinity;
            float bestDistance = float.PositiveInfinity;
            Vector2 normalizedAim = aim.sqrMagnitude > 0.01f
                ? aim.normalized
                : new Vector2(facing == 0 ? 1f : facing, 0.65f).normalized;

            foreach (WebAnchor2D candidate in WebAnchor2D.ActiveAnchors)
            {
                if (candidate == null || !candidate.GameplayEnabled)
                {
                    continue;
                }

                Vector2 delta = candidate.Position - origin;
                float distance = delta.magnitude;
                if (distance <= 0.05f || distance > tuning.swingSearchRadius)
                {
                    continue;
                }

                float alignment = Vector2.Dot(normalizedAim, delta / distance);
                if (delta.y < -0.25f && alignment < 0.75f)
                {
                    continue;
                }

                RaycastHit2D hit = Physics2D.Linecast(origin, candidate.Position, obstructionMask);
                if (hit.collider != null && Vector2.Distance(hit.point, candidate.Position) > 0.3f)
                {
                    continue;
                }

                float score = ScoreCandidate(origin, candidate.Position, normalizedAim, facing, tuning.swingSearchRadius);
                if (score > bestScore || (Mathf.Approximately(score, bestScore) && distance < bestDistance))
                {
                    best = candidate;
                    bestScore = score;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public static float ScoreCandidate(
            Vector2 origin,
            Vector2 candidate,
            Vector2 aim,
            int facing,
            float searchRadius)
        {
            Vector2 delta = candidate - origin;
            float distance = Mathf.Max(0.001f, delta.magnitude);
            Vector2 direction = delta / distance;
            float aimAlignment = Mathf.Clamp01((Vector2.Dot(aim.normalized, direction) + 1f) * 0.5f);
            float heightAdvantage = Mathf.Clamp01((delta.y / Mathf.Max(0.001f, searchRadius) + 1f) * 0.5f);
            float distanceScore = 1f - Mathf.Clamp01(distance / Mathf.Max(0.001f, searchRadius));
            float forwardBias = Mathf.Clamp01((direction.x * (facing == 0 ? 1 : facing) + 1f) * 0.5f);
            return aimAlignment * 0.45f +
                   heightAdvantage * 0.25f +
                   distanceScore * 0.20f +
                   forwardBias * 0.10f;
        }
    }
}
