using System;
using UnityEngine;

namespace Hermes
{
    public class FPSounds : FPControllerModule
    {
        [SerializeField] AudioSource AudioSource;

        [Header("Default Clips (used when no SurfaceAudioMap override exists)")]
        [SerializeField] AudioClip[] FootstepSounds;
        [SerializeField] AudioClip[] LandSounds;
        [SerializeField] AudioClip[] WallJumpSounds;
        [SerializeField] AudioClip[] JumpSounds;
        [SerializeField] AudioClip[] DoubleJumpSounds;

        // Slide loop sound
        [SerializeField] AudioSource SlideAudioSource;
        [SerializeField] AudioClip SlideLoopClip;
        [SerializeField, Range(0f, 1f)] float SlideVolume = 0.6f;

        // Wall run sounds
        [SerializeField] AudioClip[] WallRunStartSounds;
        [SerializeField] AudioSource WallRunAudioSource;
        [SerializeField] AudioClip WallRunLoopClip;
        [SerializeField, Range(0f, 1f)] float WallRunVolume = 0.6f;

        [Header("Surface Audio")]
        [Tooltip("Optional. When assigned, overrides default clips based on the surface the player is touching.")]
        [SerializeField] SurfaceAudioMap SurfaceAudio;

        [SerializeField] float footstepTimer = 0f;
        Collider lastWallRunCollider;


        private void Start()
        {
            Controller.Jumped.AddListener(PlayJumpSound);
            Controller.DoubleJumped.AddListener(PlayDoubleJumpSound);
            Controller.Landed.AddListener(OnLanded);

            // Ensure we have a dedicated AudioSource for slide looping.
            if (SlideAudioSource == null)
            {
                // try to use a separate child AudioSource so PlayOneShot on the main source isn't affected
                var go = new GameObject("SlideAudioSource");
                go.transform.SetParent(Controller.transform, false);
                SlideAudioSource = go.AddComponent<AudioSource>();

                // basic 3D setup consistent with first-person sounds
                SlideAudioSource.spatialBlend = 0.0f; // 2D by default; change to >0 for 3D
                SlideAudioSource.playOnAwake = false;
                SlideAudioSource.loop = true;
            }

            if (WallRunAudioSource == null)
            {
                var go = new GameObject("WallRunAudioSource");
                go.transform.SetParent(Controller.transform, false);
                WallRunAudioSource = go.AddComponent<AudioSource>();
                WallRunAudioSource.spatialBlend = 0.0f;
                WallRunAudioSource.playOnAwake = false;
                WallRunAudioSource.loop = true;
            }

            // Listen to activity start/stop to trigger slide loop
            if (Controller.ActivityManager != null)
            {
                Controller.ActivityManager.ActivityStarted.AddListener((activity) =>
                {
                    if (activity.GetType() == typeof(FPSlide))
                    {
                        StartSlideLoop();
                    }
                    else if (activity.GetType() == typeof(FPWallJump))
                    {
                        PlaySound(WallJumpSounds, Preset.JumpSoundVolume);
                    }
                    else if (activity.GetType() == typeof(FPWallRun))
                    {
                        var wr = Controller.ActivityManager.GetActivity<FPWallRun>();
                        lastWallRunCollider = wr != null ? wr.WallCollider : null;

                        var startClips = GetSurfaceClips(lastWallRunCollider, s => s.WallRunStart) ?? WallRunStartSounds;
                        PlaySound(startClips, WallRunVolume);
                        StartWallRunLoop(lastWallRunCollider);
                    }
                });

                Controller.ActivityManager.ActivityStopped.AddListener((activity) =>
                {
                    if (activity.GetType() == typeof(FPSlide))
                    {
                        StopSlideLoop();
                    }
                    else if (activity.GetType() == typeof(FPWallRun))
                    {
                        StopWallRunLoop();
                    }
                });
            }
        }


        void Update()
        {
            UpdateFootstepSounds();
            UpdateWallRunSurface();
        }



        void GetFootstepIntervalAndVolume(out float interval, out float volume)
        {
            interval = Preset.FootstepWalkInterval;
            volume = Preset.FootstepWalkVolume;


            if (!Controller.Crouching)
            {
                float t = Mathf.InverseLerp(Preset.WalkSpeed, Preset.SprintSpeed, Controller.HorizontalSpeed);

                interval = Mathf.Lerp(Preset.FootstepWalkInterval, Preset.FootstepSprintInterval, t);
                volume = Mathf.Lerp(Preset.FootstepWalkVolume, Preset.FootstepSprintVolume, t);
            }
            else
            {
                float t = Mathf.InverseLerp(Preset.CrouchSpeed, Preset.WalkSpeed, Controller.HorizontalSpeed);

                interval = Mathf.Lerp(Preset.FootstepCrouchInterval, Preset.FootstepWalkInterval, t);
                volume = Mathf.Lerp(Preset.FootstepCrouchVolume, Preset.FootstepWalkVolume, t);
            }
        }


        void UpdateFootstepSounds()
        {
            if (Controller.Grounded && Controller.HorizontalSpeed >= 0.2f)
            {
                GetFootstepIntervalAndVolume(out float interval, out float volume);

                if (Time.time >= footstepTimer)
                {
                    var clips = GetSurfaceClips(Controller.GroundCollider, s => s.Footsteps) ?? FootstepSounds;
                    PlaySound(clips, volume);

                    footstepTimer = Time.time + interval;
                }
            }
        }




        void OnLanded()
        {
            if (Controller.VerticalSpeed >= -Preset.LandVelocityThreshold)
            {
                return;
            }

            float scale = Mathf.InverseLerp(0f, -20f, Controller.VerticalSpeed);

            scale *= Preset.MaxLandSoundVolume;

            var clips = GetSurfaceClips(Controller.GroundCollider, s => s.Land) ?? LandSounds;
            PlaySound(clips, scale);
        }


        void PlaySound(AudioClip[] clips, float volumeScale = 1f)
        {
            try
            {
                if (clips == null || clips.Length == 0 || AudioSource == null) return;

                int index = UnityEngine.Random.Range(0, clips.Length);

                AudioClip clip = clips[index];

                AudioSource.PlayOneShot(clip, volumeScale);
            }
            catch { }
        }

        void PlayJumpSound()
        {
            PlaySound(JumpSounds, Preset.JumpSoundVolume);
        }

        void PlayDoubleJumpSound()
        {
            PlaySound(DoubleJumpSounds, Preset.JumpSoundVolume);
        }


        // Slide loop control

        void StartSlideLoop()
        {
            if (SlideLoopClip == null || SlideAudioSource == null) return;

            if (SlideAudioSource.isPlaying)
            {
                // already playing � ensure correct clip/volume
                if (SlideAudioSource.clip != SlideLoopClip)
                {
                    SlideAudioSource.clip = SlideLoopClip;
                }
                SlideAudioSource.volume = SlideVolume;
                return;
            }

            SlideAudioSource.clip = SlideLoopClip;
            SlideAudioSource.loop = true;
            SlideAudioSource.volume = SlideVolume;
            SlideAudioSource.Play();
        }

        void StopSlideLoop()
        {
            if (SlideAudioSource == null) return;

            if (SlideAudioSource.isPlaying)
            {
                SlideAudioSource.Stop();
            }
        }


        // Wall run loop control

        void StartWallRunLoop(Collider wallCol)
        {
            if (WallRunAudioSource == null) return;

            AudioClip loopClip = GetSurfaceClip(wallCol, s => s.WallRunLoop) ?? WallRunLoopClip;
            if (loopClip == null) return;

            if (WallRunAudioSource.isPlaying && WallRunAudioSource.clip == loopClip)
            {
                WallRunAudioSource.volume = WallRunVolume;
                return;
            }

            WallRunAudioSource.clip = loopClip;
            WallRunAudioSource.loop = true;
            WallRunAudioSource.volume = WallRunVolume;
            WallRunAudioSource.Play();
        }

        void StopWallRunLoop()
        {
            if (WallRunAudioSource == null) return;

            if (WallRunAudioSource.isPlaying)
            {
                WallRunAudioSource.Stop();
            }

            lastWallRunCollider = null;
        }

        void UpdateWallRunSurface()
        {
            if (WallRunAudioSource == null || !WallRunAudioSource.isPlaying) return;

            var wr = Controller.ActivityManager != null ? Controller.ActivityManager.GetActivity<FPWallRun>() : null;
            if (wr == null || !wr.Active) return;

            Collider currentCol = wr.WallCollider;
            if (currentCol == lastWallRunCollider) return;

            lastWallRunCollider = currentCol;
            StartWallRunLoop(currentCol);
        }


        // Surface helpers

        ESurfaceType GetSurfaceType(Collider col)
        {
            if (col == null) return ESurfaceType.Default;
            var surface = col.GetComponentInParent<SurfaceType>();
            return surface != null ? surface.Type : ESurfaceType.Default;
        }

        AudioClip[] GetSurfaceClips(Collider col, Func<SurfaceAudioMap.SurfaceClipSet, AudioClip[]> selector)
        {
            if (SurfaceAudio == null) return null;
            var type = GetSurfaceType(col);
            var clipSet = SurfaceAudio.GetClipSet(type);
            if (clipSet == null) return null;
            var clips = selector(clipSet);
            return clips != null && clips.Length > 0 ? clips : null;
        }

        AudioClip GetSurfaceClip(Collider col, Func<SurfaceAudioMap.SurfaceClipSet, AudioClip> selector)
        {
            if (SurfaceAudio == null) return null;
            var type = GetSurfaceType(col);
            var clipSet = SurfaceAudio.GetClipSet(type);
            return clipSet != null ? selector(clipSet) : null;
        }
    }
}