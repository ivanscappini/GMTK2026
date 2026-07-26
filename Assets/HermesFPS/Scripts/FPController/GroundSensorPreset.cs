using UnityEngine;


namespace Hermes
{
	[CreateAssetMenu(menuName = "Hermes/Ground Sensor Preset")]
	public class GroundSensorPreset : ScriptableObject
	{
		[Tooltip("Starting height of the sensor relative to the feet of the character")]
		public float SensorHeight = 0.5f;

		[Tooltip("For best results, set this to the character controller's radius plus skin width.")]
		public float SensorRadius = 0.4f;

		public float SensorMaxDistance = 500f;

		public LayerMask GroundLayerMask = Physics.DefaultRaycastLayers;

		[Tooltip("Character is considered as grounded if distance to ground is below this value")]
		public float GroundedThreshold = 0.1f;



		public void ScanGround(Vector3 characterPosition, out float GroundDistance, out Vector3 GroundNormal, out Collider GroundCollider)
		{
			Ray ray = new Ray(characterPosition + Vector3.up * SensorHeight, Vector3.down);

			if (Physics.SphereCast(ray,
				SensorRadius,
				out RaycastHit thickHit,
				SensorMaxDistance,
				GroundLayerMask))
			{

				GroundDistance = Mathf.Max(thickHit.distance - SensorHeight, 0f);
				GroundCollider = thickHit.collider;

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
				GroundNormal = Vector3.up;
				GroundDistance = Mathf.Infinity;
				GroundCollider = null;
			}
		}


	}
}

