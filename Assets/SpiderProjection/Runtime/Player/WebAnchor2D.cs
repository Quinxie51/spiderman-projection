using System.Collections.Generic;
using UnityEngine;

namespace SpiderProjection.Runtime
{
    public enum WebSurfaceType
    {
        Wall,
        Wood,
        Metal
    }

    public sealed class WebAnchor2D : MonoBehaviour
    {
        private static readonly List<WebAnchor2D> ActiveAnchorsInternal = new List<WebAnchor2D>();

        [SerializeField] private WebSurfaceType surfaceType = WebSurfaceType.Wall;
        [SerializeField] private bool gameplayEnabled = true;

        public static IReadOnlyList<WebAnchor2D> ActiveAnchors => ActiveAnchorsInternal;
        public Vector2 Position => transform.position;
        public WebSurfaceType SurfaceType => surfaceType;
        public bool GameplayEnabled => gameplayEnabled && isActiveAndEnabled;

        public void Configure(WebSurfaceType type, bool enabledForGameplay = true)
        {
            surfaceType = type;
            gameplayEnabled = enabledForGameplay;
        }

        private void OnEnable()
        {
            if (!ActiveAnchorsInternal.Contains(this))
            {
                ActiveAnchorsInternal.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveAnchorsInternal.Remove(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gameplayEnabled ? new Color(0.1f, 0.9f, 1f, 0.9f) : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.12f);
        }
    }
}
