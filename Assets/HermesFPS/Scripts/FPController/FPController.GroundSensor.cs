using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hermes
{
	public partial class FPController
	{
		[Header("Ground Sensor")]

		[SerializeField] bool drawGroundSensorGizmos = false;

		bool drawDebugGUI = false;

		private bool wasGrounded = false;

		public bool Grounded
		{
			get
			{
				bool physGrounded = false;
				if (characterController != null)
				{
					physGrounded = characterController.isGrounded;
				}

				return GroundDistance <= Preset.GroundSensor.GroundedThreshold || physGrounded;
			}
		}


		public float GroundDistance { get; private set; }

		public Vector3 GroundNormal { get; private set; }

		public Collider GroundCollider { get; private set; }


		private void TryDrawGroundSensorGizmos()
		{
			if (drawGroundSensorGizmos == false)
			{
				return;
			}

			Gizmos.color = Color.red;

			Vector3 sensorStart = transform.position + Vector3.up * Preset.GroundSensor.SensorHeight;

			Gizmos.DrawWireSphere(sensorStart, Preset.GroundSensor.SensorRadius);

			Vector3 sensorEnd = sensorStart + Vector3.down * (GroundDistance + Preset.GroundSensor.SensorHeight);

			sensorEnd += Vector3.down * CharacterController.skinWidth;

			Gizmos.DrawLine(sensorStart, sensorEnd);

			Gizmos.color = Color.green;
			Gizmos.DrawLine(sensorEnd, sensorEnd + 2f * GroundNormal);
			Gizmos.DrawSphere(sensorEnd, 0.05f);
		}



		private void OnGUI()
		{
			if (drawDebugGUI == false)
			{
				return;
			}

			float guiScale = 2f;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * guiScale);

			GUILayout.BeginArea(new Rect(20, 20, 300, 400), GUI.skin.box);

			GUILayout.BeginVertical();

			GUILayout.Label($"Grounded: {Grounded}");
			GUILayout.Label($"Ground Distance: {GroundDistance}");
			GUILayout.Space(20);

			GUILayout.BeginHorizontal();

			GUILayout.Label($"Speed: {HorizontalSpeed} / {Movement.MaxSpeed}");

			if (Sprinting)
			{
				GUILayout.Label(" === SPRINTING!");
			}

			GUILayout.EndHorizontal();

			GUILayout.EndVertical();

			GUILayout.EndArea();
		}


		void UpdateGroundCheck()
		{
			if (Keyboard.current.numpad5Key.wasPressedThisFrame)
			{
				drawDebugGUI = !drawDebugGUI;
			}


			// Scanning the ground
			Preset.GroundSensor.ScanGround(transform.position, out float distance, out Vector3 normal, out Collider groundCol);

			GroundDistance = distance;
			GroundNormal = normal;
			GroundCollider = groundCol;

			// Also consider the CharacterController's built-in grounding as a fallback.
			// In some tight geometry (low ceilings / slides) the spherecast-based sensor
			// may be unreliable; the controller's isGrounded flag is a useful extra signal.
			bool physGrounded = false;
			if (characterController != null)
			{
				physGrounded = characterController.isGrounded;
			}

			if (!wasGrounded && (Grounded || physGrounded))
			{
				Landed?.Invoke();
			}

			wasGrounded = (Grounded || physGrounded);
		}

	}
}