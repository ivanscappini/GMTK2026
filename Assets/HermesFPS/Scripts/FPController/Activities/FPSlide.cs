using UnityEngine;

namespace Hermes
{
    public class FPSlide : FPActivity
    {
        bool queued = false;

        float slideSpeed = 0f;
        float initialSlideSpeed = 0f;
        Vector3 slideDirection = Vector3.zero;
        float slideTimer = 0f;

        Vector3 pendingForwardVelocity = Vector3.zero;

        // cancellation/blend state
        bool cancelling = false;
        float cancelTimer = 0f;
        float cancelDuration = 0f;
        Vector3 cancelStartVelocity = Vector3.zero;
        Vector3 cancelTargetVelocity = Vector3.zero;

        // if player releases before MinSlideTime, mark pending cancel
        bool pendingCancel = false;

        // perfect jump tracking
        bool perfectJumpExit = false;

        // cooldown tracking
        float lastSlideEndTime = -999f;

        // when cancel is attempted but there's an obstacle overhead, push forward until clear
        bool pushingOut = false;
        float pushOutSpeed = 0f;
        float pushOutTimer = 0f;
        bool cancelRequestedDuringPush = false;

        public bool InPerfectWindow
        {
            get
            {
                if (!Active || initialSlideSpeed <= 0f) return false;
                float ratio = slideSpeed / initialSlideSpeed;
                return ratio <= Preset.SlidePerfectJumpStart && ratio >= Preset.SlidePerfectJumpEnd;
            }
        }

        CharacterController cc => Controller.CharacterController;

        public override bool CanStartActivity()
        {
            if (!Controller.Grounded)
                return false;

            if (!Controller.ActivityManager.IsActive<FPSprint>())
                return false;

            if (Controller.HorizontalSpeed < Preset.MinStartSpeed)
                return false;

            if (Time.time - lastSlideEndTime < Preset.SlideCooldown)
                return false;

            return true;
        }

        public override bool CanStopActivity()
        {
            return true;
        }

        protected override void Awake()
        {
            base.Awake();

            if (Controller != null)
            {
                Controller.Landed.AddListener(OnLanded);
                Controller.Jumped.AddListener(OnJumped);
            }
        }

        void OnJumped()
        {
            if (!Active) return;

            perfectJumpExit = InPerfectWindow;

            Manager.ForceStop(this);
        }

        private void Update()
        {
            if (!Active && !cancelling)
                return;

            if (Active)
            {
                // if we're pushing the player out from under a ceiling, override normal slide velocity
                if (pushingOut)
                {
                    pushOutTimer += Time.deltaTime;

                    // drive forward while pushing out
                    pendingForwardVelocity = Controller.transform.forward * pushOutSpeed;

                    // if cleared or timeout, attempt cancel
                    if (!IsCeilingAbove() || pushOutTimer >= Preset.SlidePushOutMaxDuration)
                    {
                        pushingOut = false;
                        pushOutTimer = 0f;

                        // proceed with cancellation now that we're clear (or timed out)
                        RequestCancel();
                    }

                    return;
                }

                slideTimer += Time.deltaTime;

                slideSpeed *= Mathf.Exp(-Preset.Friction * Time.deltaTime);

                Vector3 horizVel = slideDirection * slideSpeed;
                pendingForwardVelocity = Controller.transform.forward * Vector3.Dot(horizVel, Controller.transform.forward);

                if (!Controller.CrouchInput)
                {
                    if (slideTimer < Preset.MinSlideTime)
                    {
                        pendingCancel = true;
                        return;
                    }

                    RequestCancel();
                    return;
                }

                if (pendingCancel)
                    pendingCancel = false;

                if ((slideTimer >= Preset.MinSlideTime && slideSpeed <= Preset.StopSpeedThreshold) || slideTimer >= Preset.MaxSlideDuration)
                {
                    Manager.TryStop(this);
                }
            }
        }

        private void LateUpdate()
        {
            if (Active)
            {
                Controller.ForwardVelocity = pendingForwardVelocity;
                return;
            }

            if (cancelling)
            {
                cancelTimer += Time.deltaTime;
                float t = Mathf.Clamp01(cancelTimer / cancelDuration);
                float u = Mathf.SmoothStep(0f, 1f, t);

                Controller.ForwardVelocity = Vector3.Lerp(cancelStartVelocity, cancelTargetVelocity, u);

                if (t >= 1f)
                {
                    Controller.ForwardVelocity = cancelTargetVelocity;
                    cancelling = false;
                    cancelTimer = 0f;
                    cancelDuration = 0f;
                    cancelStartVelocity = Vector3.zero;
                    cancelTargetVelocity = Vector3.zero;

                    Controller.ActivityManager.TryStart<FPSprint>();
                }
            }
            else
            {
                pendingForwardVelocity = Vector3.zero;

                if (pendingCancel && slideTimer >= Preset.MinSlideTime)
                {
                    pendingCancel = false;
                    RequestCancel();
                }
            }
        }

        void RequestCancel()
        {
            if (cancelling) return;

            // if there's an obstacle above the player's head, start pushing them forward until clear
            if (IsCeilingAbove())
            {
                // begin push-out behavior
                pushingOut = true;
                pushOutTimer = 0f;
                // push speed equals current forward speed or a configured minimum
                pushOutSpeed = Mathf.Max(Controller.ForwardVelocity.magnitude, Preset.SlidePushOutMinSpeed);
                // remember that a cancel was requested so we cancel after push
                cancelRequestedDuringPush = true;
                return;
            }

            cancelling = true;
            cancelTimer = 0f;
            cancelDuration = Mathf.Max(0.01f, Preset.SlideExitBlendTime);
            cancelStartVelocity = Controller.ForwardVelocity;
            cancelTargetVelocity = cancelStartVelocity * Preset.SlideCancelMomentum;

            Manager.ForceStop(this);
        }

        public override void OnActivityStarted()
        {
            base.OnActivityStarted();

            Controller.ActivityManager.ForceStop<FPSprint>();

            slideDirection = Controller.transform.forward.normalized;
            slideSpeed = Mathf.Max(Controller.HorizontalSpeed + Preset.BoostAmount, 0f);
            initialSlideSpeed = slideSpeed;
            slideTimer = 0f;
            queued = false;
            perfectJumpExit = false;

            Vector3 horizVel = slideDirection * slideSpeed;
            pendingForwardVelocity = Controller.transform.forward * Vector3.Dot(horizVel, Controller.transform.forward);

            cancelling = false;
            cancelTimer = 0f;
            cancelDuration = 0f;
            cancelStartVelocity = Vector3.zero;
            cancelTargetVelocity = Vector3.zero;
            pendingCancel = false;
        }

        public override void OnActivityStopped()
        {
            base.OnActivityStopped();

            if (perfectJumpExit)
            {
                perfectJumpExit = false;
                cancelling = true;
                cancelTimer = 0f;
                cancelDuration = 0.05f;
                cancelStartVelocity = Controller.ForwardVelocity;
                cancelTargetVelocity = cancelStartVelocity * Preset.SlidePerfectJumpBoost;

                // FOV punch so the player feels the boost
                if (Controller.Camera != null)
                    Controller.Camera.Lens.FieldOfView += 4f;
            }
            else if (!cancelling)
            {
                cancelling = true;
                cancelTimer = 0f;
                cancelDuration = Mathf.Max(0.01f, Preset.SlideExitBlendTime);
                cancelStartVelocity = Controller.ForwardVelocity;
                cancelTargetVelocity = cancelStartVelocity * Preset.SlideCancelMomentum;
            }

            lastSlideEndTime = Time.time;
            // if we were pushing out and a cancel was requested during that, ensure we cancel now
            if (pushOutTimer > 0f && cancelRequestedDuringPush)
            {
                cancelRequestedDuringPush = false;
                // start normal cancelling blend
                cancelling = true;
                cancelTimer = 0f;
                cancelDuration = Mathf.Max(0.01f, Preset.SlideExitBlendTime);
                cancelStartVelocity = Controller.ForwardVelocity;
                cancelTargetVelocity = cancelStartVelocity * Preset.SlideCancelMomentum;
            }
        }

        void OnLanded()
        {
            if (queued)
            {
                Manager.TryStart(this);
                queued = false;
            }
        }

        public void QueueSlide()
        {
            if (!Controller.Grounded && Controller.SprintInput)
            {
                queued = true;
            }
        }

        public void CancelQueued()
        {
            queued = false;
        }

        bool IsCeilingAbove()
        {
            // cast from the camera position (approx head) upward to check for obstacles up to standing height
            Vector3 origin = Controller.CameraTransform.position;
            float standingHeight = Preset.StandHeight;
            float currentHeadLocalY = Controller.CameraTransform.localPosition.y;
            float rayDistance = Mathf.Max(0f, standingHeight - currentHeadLocalY);

            return Physics.Raycast(origin, Vector3.up, rayDistance, Preset.ObstacleLayerMask);
        }
    }
}
