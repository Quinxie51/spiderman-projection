using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AnimationDriver2D : MonoBehaviour
    {
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite jumpSprite;
        [SerializeField] private Sprite hangSprite;

        private SpriteRenderer spriteRenderer;
        private GameplayState presentedState = (GameplayState)(-1);

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Present(
            GameplayState state,
            int facing,
            Vector2 move,
            Vector2 velocity,
            bool grounded)
        {
            spriteRenderer.flipX = facing < 0;

            if (state == presentedState)
            {
                return;
            }

            spriteRenderer.sprite = ResolveSprite(state);
            presentedState = state;
        }

        private Sprite ResolveSprite(GameplayState state)
        {
            switch (state)
            {
                case GameplayState.SwingAttach:
                case GameplayState.SwingLoop:
                case GameplayState.WallCling:
                case GameplayState.WallCrawl:
                case GameplayState.LedgeGrab:
                case GameplayState.LedgeClimb:
                    return hangSprite;

                case GameplayState.JumpStart:
                case GameplayState.JumpRise:
                case GameplayState.Apex:
                case GameplayState.Fall:
                case GameplayState.Roll:
                case GameplayState.WebShoot:
                case GameplayState.SwingRelease:
                    return jumpSprite;

                default:
                    return idleSprite;
            }
        }
    }
}
