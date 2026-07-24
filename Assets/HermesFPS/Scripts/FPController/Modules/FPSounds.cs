using UnityEngine;

namespace Hermes
{
    public class FPSounds : FPControllerModule
    {
        [SerializeField] AudioSource AudioSource;


        [SerializeField] AudioClip[] FootstepSounds;
        [SerializeField] AudioClip[] LandSounds;
        [SerializeField] AudioClip[] JumpSounds;
        [SerializeField] AudioClip[] DoubleJumpSounds;


        [SerializeField] float footstepTimer = 0f;


        private void Start()
        {
            Controller.Jumped.AddListener(PlayJumpSound);
            Controller.DoubleJumped.AddListener(PlayDoubleJumpSound);
            Controller.Landed.AddListener(OnLanded);
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
    }
}