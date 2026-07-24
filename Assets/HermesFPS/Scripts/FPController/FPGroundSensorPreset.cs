using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    [CreateAssetMenu(fileName = "FPGroundSensorPreset", menuName = "HermesFPS/FPController/Ground Sensor Preset")]
    public class FPGroundSensorPreset : ScriptableObject
    {
        [Header("Ground Sensor Parameters")]
        [Tooltip("Height of the ground sensor from the character's position.")]
        public float SensorHeight = 0.5f;

        [Tooltip("For best results, set this to the character controller's radius plus skin width")]
        public float SensorRadius = 0.4f;

        public float SensorMaxDistance = 500f;

        public LayerMask GroundLayerMask = Physics.DefaultRaycastLayers;

        [Tooltip("Character is considered grounded if distance to ground is below this value")]
        public float GroundedThreshold = 0.1f;

        public void ScanGround(Vector3 characterPosition, out float GroundDistance, out Vector3 GroundNormal)
        {
            Ray ray = new Ray(characterPosition + Vector3.up * SensorHeight, Vector3.down);

            if (Physics.SphereCast(ray, SensorRadius, out RaycastHit thickHit, SensorMaxDistance))
            {
                GroundDistance = Mathf.Max(thickHit.distance - SensorHeight, 0f);

                if (Physics.Raycast(ray, out RaycastHit hit, SensorHeight * 2f, GroundLayerMask))
                {
                    GroundNormal = hit.normal;
                }
                else
                {
                    GroundNormal = Vector3.up;
                }
            }
            else
            {
                GroundDistance = Mathf.Infinity;
                GroundNormal = Vector3.up;
            }
        }
    }
}
