using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class AnimationDriver2D : MonoBehaviour
    {
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int SpeedX = Animator.StringToHash("SpeedX");
        private static readonly int SpeedY = Animator.StringToHash("SpeedY");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int FacingRight = Animator.StringToHash("FacingRight");
        private static readonly int GameplayStateParameter = Animator.StringToHash("GameplayState");

        [SerializeField] private float crossFadeSeconds = 0.04f;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private GameplayState presentedState = (GameplayState)(-1);

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator.applyRootMotion = false;
        }

        public void Present(
            GameplayState state,
            int facing,
            Vector2 move,
            Vector2 velocity,
            bool grounded)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(MoveX, move.x);
            animator.SetFloat(MoveY, move.y);
            animator.SetFloat(SpeedX, Mathf.Abs(velocity.x));
            animator.SetFloat(SpeedY, velocity.y);
            animator.SetBool(Grounded, grounded);
            animator.SetBool(FacingRight, facing >= 0);
            animator.SetInteger(GameplayStateParameter, (int)state);
            spriteRenderer.flipX = facing < 0;

            if (state == presentedState)
            {
                return;
            }

            int stateHash = Animator.StringToHash(state.ToString());
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, crossFadeSeconds, 0);
            }
            presentedState = state;
        }
    }
}
