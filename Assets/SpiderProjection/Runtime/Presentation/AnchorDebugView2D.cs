using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class AnchorDebugView2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer candidateMarker;
        [SerializeField] private bool showInBuild;

        private void Awake()
        {
            if (candidateMarker == null)
            {
                candidateMarker = GetComponentInChildren<SpriteRenderer>(true);
            }
            if (candidateMarker != null)
            {
                candidateMarker.enabled = false;
            }
        }

        public void ShowCandidate(WebAnchor2D candidate)
        {
            if (candidateMarker == null)
            {
                return;
            }

            bool visible = candidate != null && (showInBuild || Debug.isDebugBuild || Application.isEditor);
            candidateMarker.enabled = visible;
            if (visible)
            {
                candidateMarker.transform.position = new Vector3(candidate.Position.x, candidate.Position.y, -0.1f);
            }
        }
    }
}
