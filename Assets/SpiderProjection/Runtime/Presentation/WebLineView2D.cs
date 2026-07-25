using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class WebLineView2D : MonoBehaviour
    {
        [SerializeField] private Transform wristSocket;
        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            if (wristSocket == null)
            {
                wristSocket = transform.parent != null ? transform.parent.Find("WristSocket") : null;
            }
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.enabled = false;
        }

        public void Present(bool visible, Vector2 anchorPosition)
        {
            line.enabled = visible;
            if (!visible)
            {
                return;
            }

            Vector3 start = wristSocket != null ? wristSocket.position : transform.position;
            line.SetPosition(0, new Vector3(start.x, start.y, 0f));
            line.SetPosition(1, new Vector3(anchorPosition.x, anchorPosition.y, 0f));
        }
    }
}
