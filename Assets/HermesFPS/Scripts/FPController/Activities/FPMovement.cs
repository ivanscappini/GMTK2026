using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hermes
{
	public class FPMovement : FPActivity
	{
		Vector2 MoveInput => Controller.MoveInput;
		bool Grounded => Controller.Grounded;
		CharacterController characterController => Controller.CharacterController;


		public float MaxSpeed
		{
			get
			{
				if (Controller.Crouching)
				{
					return Preset.CrouchSpeed;
				}

				if (Controller.Sprinting)
				{
					return Preset.SprintSpeed;
				}

				return Preset.WalkSpeed;
			}
		}


		float MaxStrafeSpeed
		{
			get
			{
				float baseSpeed;
				if (Controller.Crouching)
				{
					baseSpeed = Preset.StrafeCrouchSpeed;
				}
				else if (Controller.Sprinting)
				{
					baseSpeed = Preset.StrafeSprintSpeed;
				}
				else
				{
					baseSpeed = Preset.StrafeWalkSpeed;
				}

				// allow an optional air multiplier
				if (!Grounded)
				{
					return baseSpeed * Preset.AirStrafeSpeedMultiplier;
				}

				return baseSpeed;
			}
		}

		private Vector3 velocity;

		public Vector3 Velocity => velocity;

		/// <summary>
		/// The Y component of velocity.
		/// </summary>
		public float VerticalSpeed
		{
			get => Velocity.y;

			set
			{
				velocity.y = value;
			}
		}


		Vector3 forwardVelocity;
		Vector3 rightVelocity;


		public Vector3 ForwardVelocity => forwardVelocity;


		/// <summary>
		/// Velocity without the Y component. Computed every frame.
		/// </summary>
		public Vector3 HorizontalVelocity { get; private set; }

		/// <summary>
		/// Computed every frame.
		/// </summary>
		public float HorizontalSpeed
		{
			get; private set;
		}

		public void SetForwardVelocity(Vector3 velocity)
		{
			forwardVelocity = velocity;
		}


		#region Activity Override Methods

		public override void OnActivityStarted()
		{
			Controller.MovementToggled.Invoke(true);
		}

		public override void OnActivityStopped()
		{
			Controller.MovementToggled.Invoke(false);

			VerticalSpeed = 0f;
			HorizontalVelocity = Vector3.zero;
		}

		#endregion


		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			Controller.Movement = this;
		}

		private void Update()
		{
			if (!Active && Controller.MovementEnabled)
			{
				Manager.TryStart(this);
			}
			else if (Active && !Controller.MovementEnabled)
			{
				Manager.TryStop(this);
			}



			if (Active)
			{
				MoveUpdate();
			}
		}

		#endregion



		void ComputeForwardVelocity()
		{
			Vector3 motion = transform.forward * MoveInput.y;
			motion.y = 0f;
			motion.Normalize();

			// choose acceleration/deceleration depending on grounded state
			float accel = Preset.Acceleration * (Grounded ? 1f : Preset.AirAccelerationMultiplier);
			float decelWhenOpposite = Preset.Deceleration; // braking should remain responsive
			float decelWhenIdle = Preset.Deceleration * (Grounded ? 1f : Preset.AirDecelerationMultiplier);

			if (motion.sqrMagnitude >= 0.01f)
			{
				forwardVelocity = Vector3.MoveTowards(forwardVelocity, motion * MaxSpeed, accel * Time.deltaTime);
			}

			// Determine whether we are braking (opposite direction) or simply idle (no input).
			float dot = Vector3.Dot(forwardVelocity, motion);

			if (motion.sqrMagnitude < 0.01f || dot < 0f)
			{
				// decelerating / braking
				float decelToUse = dot < 0f ? decelWhenOpposite : decelWhenIdle;
				forwardVelocity = Vector3.MoveTowards(forwardVelocity, Vector3.zero, decelToUse * Time.deltaTime);
			}


			// eliminating sideways velocity
			Vector3 sidewaysVelocity = transform.right * Vector3.Dot(transform.right, forwardVelocity);

			forwardVelocity -= sidewaysVelocity;
		}


		void ComputeRightVelocity()
		{
			Vector3 motion = transform.right * MoveInput.x;
			motion.y = 0f;
			motion.Normalize();


			float strafeAccelBase = Preset.StrafeAcceleration;
			float strafeAccel = strafeAccelBase * (Grounded ? 1f : Preset.AirStrafeAccelerationMultiplier);
			float decelWhenOpposite = Preset.Deceleration;
			float decelWhenIdle = Preset.Deceleration * (Grounded ? 1f : Preset.AirDecelerationMultiplier);


			if (motion.sqrMagnitude >= 0.01f)
			{
				rightVelocity = Vector3.MoveTowards(rightVelocity, motion * MaxStrafeSpeed, strafeAccel * Time.deltaTime);
			}

			float dot = Vector3.Dot(rightVelocity, motion);

			if (motion.sqrMagnitude < 0.01f || dot < 0f)
			{
				float decelToUse = dot < 0f ? decelWhenOpposite : decelWhenIdle;
				rightVelocity = Vector3.MoveTowards(rightVelocity, Vector3.zero, decelToUse * Time.deltaTime);
			}

			// eliminating forward velocity
			Vector3 fwdVelocity = transform.forward * Vector3.Dot(transform.forward, rightVelocity);

			rightVelocity -= fwdVelocity;
		}



		void MoveUpdate()
		{
			ComputeForwardVelocity();
			ComputeRightVelocity();

			HorizontalVelocity = forwardVelocity + rightVelocity;
			

			if (Grounded && VerticalSpeed <= 0.01f)
			{
				VerticalSpeed = -3f;
			}
			else
			{
				VerticalSpeed += Physics.gravity.y * Preset.GravityScale * Time.deltaTime;
			}


			velocity = new Vector3(HorizontalVelocity.x, VerticalSpeed, HorizontalVelocity.z);


			CollisionFlags flags = characterController.Move(Velocity * Time.deltaTime);


			if ((flags & CollisionFlags.Above) != 0 && VerticalSpeed > 0.01f)
			{
				VerticalSpeed = 0f;
			}

			HorizontalSpeed = HorizontalVelocity.magnitude;
		}
	}
}