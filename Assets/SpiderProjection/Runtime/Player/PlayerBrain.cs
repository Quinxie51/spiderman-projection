using System;
using UnityEngine;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader), typeof(PlayerSensors2D))]
    [RequireComponent(typeof(SpiderMotor2D), typeof(SwingController2D), typeof(WallTraversal2D))]
    [RequireComponent(typeof(WebTargetResolver2D))]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [SerializeField] private PlayerTuning tuning;
        [SerializeField] private LayerMask worldMask = ~0;
        [SerializeField] private float respawnDelay = 0.6f;
        [SerializeField] private float respawnInvulnerability = 0.75f;

        private Rigidbody2D body;
        private PlayerInputReader input;
        private PlayerSensors2D sensors;
        private SpiderMotor2D motor;
        private SwingController2D swing;
        private WallTraversal2D wallTraversal;
        private WebTargetResolver2D targetResolver;
        private AnimationDriver2D animationDriver;
        private PlayerAudioView audioView;
        private WebLineView2D webLine;
        private AnchorDebugView2D anchorDebug;
        private Vector2 resetPosition;
        private int facing = 1;
        private bool paused;
        private bool isDefeated;
        private float defeatTimer;
        private float invulnerableUntil;

        public GameplayState CurrentState { get; private set; } = GameplayState.Idle;
        public int Facing => facing;
        public PlayerTuning Tuning => tuning;
        public bool IsInvulnerable => isDefeated || Time.time < invulnerableUntil;
        public event Action<GameplayState, GameplayState> StateChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
            sensors = GetComponent<PlayerSensors2D>();
            motor = GetComponent<SpiderMotor2D>();
            swing = GetComponent<SwingController2D>();
            wallTraversal = GetComponent<WallTraversal2D>();
            targetResolver = GetComponent<WebTargetResolver2D>();
            animationDriver = GetComponentInChildren<AnimationDriver2D>(true);
            audioView = GetComponent<PlayerAudioView>();
            webLine = GetComponentInChildren<WebLineView2D>(true);
            anchorDebug = GetComponentInChildren<AnchorDebugView2D>(true);

            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<PlayerTuning>();
                Debug.LogWarning("[SpiderProjection] PlayerTuning was not assigned; using runtime defaults.");
            }

            sensors.Initialize(tuning, worldMask);
            motor.Initialize(tuning);
            swing.Initialize(tuning);
            wallTraversal.Initialize(tuning);
            resetPosition = transform.position;
        }

        private void Update()
        {
            if (input.ConsumePausePressed())
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
                audioView?.PlayPause(paused);
            }

            if (input.ConsumeResetPressed())
            {
                ResetPlayer();
            }

            if (input.ConsumeCalibrationPressed())
            {
                ProjectionCalibrationController.Active?.ToggleCalibrationMode();
            }

            if (paused)
            {
                return;
            }

            if (isDefeated)
            {
                defeatTimer -= Time.deltaTime;
                if (defeatTimer <= 0f)
                {
                    Respawn();
                }
                return;
            }

            if (input.ConsumeRollPressed())
            {
                motor.StartRoll(facing);
            }

            Vector2 aim = ResolveAim();
            WebAnchor2D candidate = targetResolver.Resolve(body.position, aim, facing, tuning);
            anchorDebug?.ShowCandidate(candidate);

            if (input.ConsumeSwingPressed() && candidate != null && !swing.IsAttached)
            {
                SetState(GameplayState.WebShoot);
                if (swing.Attach(candidate))
                {
                    SetState(GameplayState.SwingAttach);
                    audioView?.PlayWebAttach(candidate.SurfaceType);
                }
            }

            if ((input.ConsumeSwingReleased() || !input.SwingHeld) && swing.IsAttached)
            {
                swing.Detach(true);
                SetState(GameplayState.SwingRelease);
            }

            animationDriver?.Present(CurrentState, facing, input.Move, body.linearVelocity, sensors.IsGrounded);
            webLine?.Present(swing.IsAttached, swing.AnchorPosition);
        }

        private void FixedUpdate()
        {
            if (paused || isDefeated)
            {
                return;
            }

            sensors.Sample(facing);
            GameplayState requested;

            if (swing.IsAttached)
            {
                swing.PhysicsTick(Time.fixedDeltaTime);
                requested = GameplayState.SwingLoop;
            }
            else if (wallTraversal.TryPhysicsTick(Time.fixedDeltaTime, out GameplayState wallState))
            {
                requested = wallState;
            }
            else
            {
                requested = motor.PhysicsTick(Time.fixedDeltaTime, ref facing);
            }

            SetState(requested);
        }

        public void SetTuning(PlayerTuning playerTuning)
        {
            tuning = playerTuning;
            if (sensors == null)
            {
                return;
            }
            sensors.Initialize(tuning, worldMask);
            motor.Initialize(tuning);
            swing.Initialize(tuning);
            wallTraversal.Initialize(tuning);
        }

        public void Configure(PlayerTuning playerTuning, LayerMask collisionMask)
        {
            tuning = playerTuning;
            worldMask = collisionMask;
            if (Application.isPlaying)
            {
                SetTuning(playerTuning);
            }
        }

        public void ResetPlayer()
        {
            isDefeated = false;
            body.simulated = true;
            if (swing.IsAttached)
            {
                swing.Detach(false);
            }
            body.position = resetPosition;
            motor.ResetMotion();
            SetState(GameplayState.Idle);
        }

        public void Die()
        {
            if (IsInvulnerable)
            {
                return;
            }

            isDefeated = true;
            defeatTimer = respawnDelay;
            if (swing.IsAttached)
            {
                swing.Detach(false);
            }
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            SetState(GameplayState.Defeat);
            audioView?.PlayHurt();
        }

        private void Respawn()
        {
            invulnerableUntil = Time.time + respawnInvulnerability;
            ResetPlayer();
        }

        private Vector2 ResolveAim()
        {
            if (input.Aim.sqrMagnitude > 0.04f)
            {
                return input.Aim.normalized;
            }
            if (body.linearVelocity.sqrMagnitude > 1f)
            {
                return body.linearVelocity.normalized;
            }
            return new Vector2(facing, 0.75f).normalized;
        }

        private void SetState(GameplayState requested)
        {
            if (requested == CurrentState)
            {
                return;
            }

            GameplayState previous = CurrentState;
            CurrentState = requested;
            animationDriver?.Present(CurrentState, facing, input.Move, body.linearVelocity, sensors.IsGrounded);
            audioView?.OnStateChanged(previous, CurrentState, motor.LastLandingWasHard);
            StateChanged?.Invoke(previous, CurrentState);
        }
    }
}
