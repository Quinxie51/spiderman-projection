using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderProjection.Runtime
{
    public sealed class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AudioSource idleSource;
        [SerializeField] private AudioSource actionSource;
        [SerializeField] private AudioClip idleMusic;
        [SerializeField] private AudioClip actionMusic;
        [SerializeField] private float actionCrossfadeSeconds = 0.35f;
        private Coroutine crossfade;

        public void Configure(AudioMixerGroup output, AudioClip idle, AudioClip action)
        {
            idleMusic = idle;
            actionMusic = action;
            EnsureSources(output);
        }

        private void Awake()
        {
            EnsureSources(null);
            idleSource.clip = idleMusic;
            idleSource.loop = true;
            actionSource.clip = actionMusic;
            actionSource.loop = true;
            if (idleSource.clip != null)
            {
                idleSource.volume = 1f;
                idleSource.Play();
            }
        }

        public void Bind(PlayerBrain brain)
        {
            if (brain != null)
            {
                brain.StateChanged += OnStateChanged;
            }
        }

        private void OnStateChanged(GameplayState previous, GameplayState next)
        {
            bool action = next == GameplayState.Run ||
                          next == GameplayState.Roll ||
                          next == GameplayState.SwingLoop ||
                          next == GameplayState.WallCrawl;
            if (crossfade != null)
            {
                StopCoroutine(crossfade);
            }
            crossfade = StartCoroutine(Crossfade(action));
        }

        private IEnumerator Crossfade(bool toAction)
        {
            if (actionSource.clip != null && !actionSource.isPlaying)
            {
                actionSource.Play();
            }

            float duration = Mathf.Max(0.01f, actionCrossfadeSeconds);
            float elapsed = 0f;
            float idleStart = idleSource.volume;
            float actionStart = actionSource.volume;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                idleSource.volume = Mathf.Lerp(idleStart, toAction ? 0f : 1f, t);
                actionSource.volume = Mathf.Lerp(actionStart, toAction ? 1f : 0f, t);
                yield return null;
            }
        }

        private void EnsureSources(AudioMixerGroup output)
        {
            if (idleSource == null)
            {
                idleSource = gameObject.AddComponent<AudioSource>();
            }
            if (actionSource == null)
            {
                actionSource = gameObject.AddComponent<AudioSource>();
            }
            idleSource.playOnAwake = false;
            actionSource.playOnAwake = false;
            idleSource.outputAudioMixerGroup = output;
            actionSource.outputAudioMixerGroup = output;
        }
    }
}
