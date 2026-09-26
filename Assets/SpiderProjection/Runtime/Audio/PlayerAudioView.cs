using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderProjection.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerAudioView : MonoBehaviour
    {
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip jump;
        [SerializeField] private AudioClip landSoft;
        [SerializeField] private AudioClip landHard;
        [SerializeField] private AudioClip roll;
        [SerializeField] private AudioClip skid;
        [SerializeField] private AudioClip wallCrawl;
        [SerializeField] private AudioClip webRelease;
        [SerializeField] private AudioClip hurt;
        [SerializeField] private AudioClip pauseOpen;
        [SerializeField] private AudioClip pauseClose;
        [SerializeField] private AudioClip[] webShoot;
        [SerializeField] private AudioClip[] webAttach;

        private GameplayState lastContinuousState = (GameplayState)(-1);

        private void Awake()
        {
            oneShotSource ??= GetComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
        }

        public void Configure(
            AudioMixerGroup output,
            AudioClip jumpClip,
            AudioClip softLandClip,
            AudioClip hardLandClip,
            AudioClip rollClip,
            AudioClip skidClip,
            AudioClip wallCrawlClip,
            AudioClip releaseClip,
            AudioClip hurtClip,
            AudioClip pauseOpenClip,
            AudioClip pauseCloseClip,
            AudioClip[] webShootClips,
            AudioClip[] webAttachClips)
        {
            oneShotSource ??= GetComponent<AudioSource>();
            oneShotSource.outputAudioMixerGroup = output;
            jump = jumpClip;
            landSoft = softLandClip;
            landHard = hardLandClip;
            roll = rollClip;
            skid = skidClip;
            wallCrawl = wallCrawlClip;
            webRelease = releaseClip;
            hurt = hurtClip;
            pauseOpen = pauseOpenClip;
            pauseClose = pauseCloseClip;
            webShoot = webShootClips;
            webAttach = webAttachClips;
        }

        public void OnStateChanged(GameplayState previous, GameplayState next, bool hardLanding)
        {
            switch (next)
            {
                case GameplayState.JumpStart:
                    Play(jump);
                    break;
                case GameplayState.Land:
                    Play(hardLanding ? landHard : landSoft);
                    break;
                case GameplayState.Roll:
                    Play(roll);
                    break;
                case GameplayState.Skid:
                    Play(skid);
                    break;
                case GameplayState.WebShoot:
                    PlayRandom(webShoot);
                    break;
                case GameplayState.SwingRelease:
                    Play(webRelease);
                    break;
                case GameplayState.WallCrawl:
                    if (lastContinuousState != GameplayState.WallCrawl)
                    {
                        Play(wallCrawl, 0.52f);
                    }
                    break;
            }
            lastContinuousState = next;
        }

        public void PlayWebAttach(WebSurfaceType surfaceType)
        {
            int index = surfaceType == WebSurfaceType.Wood ? 1 :
                surfaceType == WebSurfaceType.Metal ? 2 : 0;
            if (webAttach != null && index < webAttach.Length)
            {
                Play(webAttach[index]);
            }
        }

        public void PlayPause(bool opened)
        {
            Play(opened ? pauseOpen : pauseClose);
        }

        public void PlayShot()
        {
            PlayRandom(webShoot);
        }

        public void PlayHurt()
        {
            Play(hurt);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }
            Play(clips[UnityEngine.Random.Range(0, clips.Length)]);
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && oneShotSource != null)
            {
                oneShotSource.PlayOneShot(clip, volume);
            }
        }
    }
}
