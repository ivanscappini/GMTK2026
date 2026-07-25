using UnityEngine;

namespace Hermes
{
    public class FPSounds : FPControllerModule
    {
        [SerializeField] AudioSource AudioSource;

        [SerializeField] AudioClip[] FootstepSounds;
        [SerializeField] AudioClip[] LandSounds;
        [SerializeField] AudioClip[] WallJumpSounds;
        [SerializeField] AudioClip[] JumpSounds;
        [SerializeField] AudioClip[] DoubleJumpSounds;

        // Slide loop sound
        [SerializeField] AudioSource SlideAudioSource;
        [SerializeField] AudioClip SlideLoopClip;
        [SerializeField, Range(0f, 1f)] float SlideVolume = 0.6f;

        [SerializeField] float footstepTimer = 0f;


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
                        // play a wall-jump one-shot
                        PlaySound(WallJumpSounds, Preset.JumpSoundVolume);
                    }
                });

                Controller.ActivityManager.ActivityStopped.AddListener((activity) =>
                {
                    if (activity.GetType() == typeof(FPSlide))
                    {
                        StopSlideLoop();
                    }
                });
            }
        }


        void Update()
        {
            UpdateFootstepSounds();
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
                    PlaySound(FootstepSounds, volume);

                    footstepTimer = Time.time + interval;
                }
            }
        }




        void OnLanded()
        {
            //Debug.Log($"Velocity: {Controller.VerticalVelocity}");

            if (Controller.VerticalSpeed >= -Preset.LandVelocityThreshold)
            {
                return;
            }

            float scale = Mathf.InverseLerp(0f, -20f, Controller.VerticalSpeed);

            scale *= Preset.MaxLandSoundVolume;

            PlaySound(LandSounds, scale);
        }


        void PlaySound(AudioClip[] clips, float volumeScale = 1f)
        {
            try
            {
                if (clips == null || clips.Length == 0 || AudioSource == null) return;

                int index = Random.Range(0, clips.Length);

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
                // already playing — ensure correct clip/volume
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
    }
}