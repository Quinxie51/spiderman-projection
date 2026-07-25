using UnityEngine;

namespace SpiderProjection.Runtime
{
    public enum MappedSurfaceKind
    {
        Ground,
        PictureFrame,
        Shelf,
        Wall,
        Hazard
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class MappedSurface2D : MonoBehaviour
    {
        [SerializeField] private string surfaceId = "surface";
        [SerializeField] private MappedSurfaceKind kind = MappedSurfaceKind.Wall;
        [SerializeField] private bool landable = true;
        [SerializeField] private bool oneWay;
        [SerializeField] private bool webAttachable;
        [SerializeField] private SpriteRenderer[] demoRenderers;

        public string SurfaceId => surfaceId;
        public MappedSurfaceKind Kind => kind;
        public bool Landable => landable;
        public bool OneWay => oneWay;
        public bool WebAttachable => webAttachable;

        public void Configure(
            string id,
            MappedSurfaceKind surfaceKind,
            bool canLand,
            bool isOneWay,
            bool canWebAttach,
            params SpriteRenderer[] visibleRenderers)
        {
            surfaceId = id;
            kind = surfaceKind;
            landable = canLand;
            oneWay = isOneWay;
            webAttachable = canWebAttach;
            demoRenderers = visibleRenderers;
        }

        public void SetDemoVisible(bool visible)
        {
            if (demoRenderers == null)
            {
                return;
            }
            foreach (SpriteRenderer spriteRenderer in demoRenderers)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = visible;
                }
            }
        }
    }
}
