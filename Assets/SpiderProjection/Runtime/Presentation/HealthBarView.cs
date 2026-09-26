using UnityEngine;
using UnityEngine.UI;

namespace SpiderProjection.Runtime
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Sprite fullSprite;
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Vector2 margin = new Vector2(30f, 30f);
        [SerializeField] private Vector2 pipSize = new Vector2(56f, 52f);
        [SerializeField] private float spacing = 8f;

        private Image[] pips;
        private PlayerBrain player;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerBrain>();
            if (player == null)
            {
                return;
            }

            Build(player.MaxHealth);
            player.HealthChanged += Refresh;
            Refresh(player.Health, player.MaxHealth);
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.HealthChanged -= Refresh;
            }
        }

        private void Build(int count)
        {
            GameObject canvasObject = new GameObject("HealthBarCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            pips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                GameObject pip = new GameObject("Heart" + i, typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = pip.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = pipSize;
                rect.anchoredPosition = new Vector2(margin.x + i * (pipSize.x + spacing), -margin.y);
                Image image = pip.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                pips[i] = image;
            }
        }

        private void Refresh(int current, int max)
        {
            if (pips == null)
            {
                return;
            }

            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].sprite = i < current ? fullSprite : emptySprite;
            }
        }
    }
}
