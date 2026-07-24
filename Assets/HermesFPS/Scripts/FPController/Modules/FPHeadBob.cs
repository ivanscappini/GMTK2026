using UnityEngine;
using UnityEngine.Rendering;

namespace Hermes
{
    [System.Serializable]
    public class HeadBobPreset
    {
        public float Amplitude = 0.5f;
        public float Frequency = 5f;

        [Space(10)]
        public float NoiseScale = 1f;
        public float MaxRotation = 30f;
    }


    public class FPHeadBob : FPControllerModule
    {
        float Amplitude = 0.5f;
        float Frequency = 5f;
        float NoiseScale = 1f;
        float MaxRotation = 30f;

        Vector3 PositionOffset;
        Quaternion RotationOffset = Quaternion.identity;

        // angle of the sine waves, incremented in the update method
        float angle = 0f;

        float SpeedRatio = 0f;


        [SerializeField] bool ApplyPositionOffset = true;
        [SerializeField] bool ApplyRotationOffset = true;
        [SerializeField] bool ApplyRotationNoiseOffset = true;


        public void SetValues(HeadBobPreset preset)
        {
            Amplitude = preset.Amplitude;
            Frequency = preset.Frequency;
            NoiseScale = preset.NoiseScale;
            MaxRotation = preset.MaxRotation;
        }


        protected override void Awake()
        {
            base.Awake();

            Controller.RequestCameraOffset += () =>
            {
                Controller.CameraPositionOffset += PositionOffset;
                Controller.CameraRotationOffset *= RotationOffset;
            };
        }

        private void Update()
        {
            if (Controller.Grounded)
            {
                UpdateValues();
                UpdateHeadBobbing();
            }
            else
            {
                PositionOffset = Vector3.Lerp(PositionOffset, Vector3.zero, 10f * Time.deltaTime);
                RotationOffset = Quaternion.Lerp(RotationOffset, Quaternion.identity, 10f * Time.deltaTime);
            }
        }


        void UpdateValues()
        {
            if (Controller.Sprinting)
            {
                SetValues(Preset.HeadBobSprint);

                SpeedRatio = Mathf.InverseLerp(Preset.WalkSpeed, Preset.SprintSpeed, Controller.HorizontalSpeed);
            }
            else if (Controller.Crouching)
            {
                SetValues(Preset.HeadBobCrouch);

                SpeedRatio = Mathf.InverseLerp(0f, Preset.CrouchSpeed, Controller.HorizontalSpeed);
            }
            else
            {
                SetValues(Preset.HeadBobWalk);

                SpeedRatio = Mathf.InverseLerp(0f, Preset.WalkSpeed, Controller.HorizontalSpeed);
            }
        }


        void UpdateHeadBobbing()
        {
            angle += Time.deltaTime * Frequency * SpeedRatio;

            float bobX = Mathf.Sin(angle) * Amplitude * SpeedRatio;

            // infinity sign pattern
            //float bobY = Mathf.Cos(angle) * Mathf.Sin(angle) * Amplitude * SpeedRatio;

            // bowditch curve pattern
            float bobY = Mathf.Cos(angle * 2f) * Amplitude * SpeedRatio;

            if (ApplyPositionOffset)
            {
                Vector3 targetOffset = new Vector3(bobX, bobY, 0f);

                PositionOffset = Vector3.Lerp(PositionOffset, targetOffset, 10f * Time.deltaTime);
            }


            float bobAngle = MaxRotation * Mathf.Sin(angle + 0.5f);
            float noise = Mathf.PerlinNoise1D(angle * NoiseScale);

            if (ApplyRotationOffset == false)
            {
                bobAngle = 0f;
            }

            if (ApplyRotationNoiseOffset)
            {
                bobAngle *= noise;
            }

            Quaternion stabilizationRot = Quaternion.LookRotation(Vector3.forward * 30f - PositionOffset);

            Quaternion TargetRotation = stabilizationRot * Quaternion.Euler(0f, 0f, bobAngle * SpeedRatio);

            RotationOffset = Quaternion.Lerp(RotationOffset, TargetRotation, 10f * Time.deltaTime);
        }

    }
}

