using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;

namespace Hermes
{
    public partial class FPController
    {
        public float GroundDistance { get; private set; } = 0f;

        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        private bool wasGrounded = false;

        public bool Grounded => GroundDistance <= Preset.GroundSensorPreset.GroundedThreshold;

        void UpdateGroundCheck()
        {
            // scanning the ground
            Preset.GroundSensorPreset.ScanGround(transform.position, out float distance, out Vector3 normal);

            GroundDistance = distance;
            GroundNormal = normal;

            if (!wasGrounded && Grounded)
            {
                Landed?.Invoke();
            }

            wasGrounded = Grounded;
        }
    }
}