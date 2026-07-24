using UnityEngine;

namespace Hermes
{
    public class FPSounds : FPControllerModule
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip[] footstepSounds;
        [SerializeField] AudioClip[] jumpSounds;
        [SerializeField] AudioClip[] doubleJumpSounds;
        [SerializeField] AudioClip[] landSounds;

        float footstepTimer = 0f;

        private void Start()
        {
            Controller.Jumped.AddListener(PlayJumpSound);
            Controller.DoubleJumped.AddListener(PlayDoubleJumpSound);
            Controller.Landed.AddListener(OnLanded);
        }

        private void Update()
        {
            UpdateFootstepSounds();
        }

        void UpdateFootstepSounds()
        {
            if (Controller.Grounded && Controller.HorizontalSpeed >= 0.2f)
            {
                float rate, volume;

                // interpolate between walk and sprint rates/volumes (crouch removed)
                float t = Mathf.InverseLerp(Preset.WalkSpeed, Preset.SprintSpeed, Controller.HorizontalSpeed);
                rate = Mathf.Lerp(Preset.FootstepWalkRate, Preset.FootstepSprintRate, t);
                volume = Mathf.Lerp(Preset.FootstepWalkVolume, Preset.FootstepSprintVolume, t);

                if (Time.time >= footstepTimer)
                {
                    PlaySound(footstepSounds, volume);
                    footstepTimer = Time.time + rate;
                }
            }
        }

        void OnLanded()
        {
            if (Controller.VerticalSpeed > -Preset.LandVelocityThreshold)
                return;

            float scale = Mathf.InverseLerp(0f, -20f, Controller.VerticalSpeed);

            scale *= Preset.MaxLandSoundVolume;

            PlaySound(landSounds, scale);
        }

        void PlaySound(AudioClip[] clips, float volumeScale = 1f)
        {
            try
            {
                int index = Random.Range(0, clips.Length);

                AudioClip clip = clips[index];

                audioSource.PlayOneShot(clip, volumeScale);
            }
            catch { }
            }

        void PlayJumpSound()
        {
            PlaySound(jumpSounds, Preset.JumpSoundVolume);
        }

        void PlayDoubleJumpSound()
        {
            PlaySound(doubleJumpSounds, Preset.JumpSoundVolume);
        }

    }
}

