using UnityEngine;

namespace Hermes
{
    // Wall jump activity: triggered when the player presses jump while facing a wall in air.
    // Applies an upward and away impulse and ends immediately.
    public class FPWallJump : FPActivity
    {
        // tuning moved to FPControllerPreset
        [Header("Detection")]
        // detection values come from Preset
        
        [Header("Behaviour")]
        float lastWallJumpTime = -999f;

        Vector3 lastWallNormal = Vector3.zero;
        public Vector3 LastWallNormal => lastWallNormal;

        // Track wall jump state for systems that need it beyond the instantaneous activity frame
        float wallJumpActiveUntil = -1f;
        [Tooltip("How long (seconds) the wall-jump is considered 'active' for animation/alignment purposes")]
        [SerializeField] float WallJumpActiveDuration = 0.4f;
        public bool IsWallJumpActive => Time.time < wallJumpActiveUntil;

        protected override void Awake()
        {
            base.Awake();

            // listen for jump attempts
            Controller.TryJump += OnTryJump;
        }

        private void Start()
        {
            if (Manager == null) return;
            var jump = Manager.GetActivity<FPJump>();
            if (jump != null)
            {
                RemoveFromBlockedByList(jump);
                AddToInterruptList(jump);
            }
        }

        bool IsFacingWall(out RaycastHit hit)
        {
            hit = default;
            Vector3 origin = Controller.transform.position + Vector3.up * 1f;
            float dist = Preset.WallCheckDistance;
            float sphereRadius = 0.15f;

            Transform t = Controller.transform;
            Vector3[] dirs = new Vector3[]
            {
                t.forward, -t.forward,
                t.right, -t.right,
                (t.forward + t.right).normalized,
                (t.forward - t.right).normalized,
                (-t.forward + t.right).normalized,
                (-t.forward - t.right).normalized,
                Controller.CameraTransform.forward,
            };

            RaycastHit bestHit = default;
            float bestDist = float.MaxValue;
            bool found = false;

            foreach (var dir in dirs)
            {
                if (Physics.SphereCast(origin, sphereRadius, dir, out var h, dist - sphereRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (ValidateWallHit(h) && h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        bestHit = h;
                        found = true;
                    }
                }

                var rayHits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (var rh in rayHits)
                {
                    if (ValidateWallHit(rh) && rh.distance < bestDist)
                    {
                        bestDist = rh.distance;
                        bestHit = rh;
                        found = true;
                    }
                }
            }

            if (found)
            {
                hit = bestHit;
                return true;
            }

            return false;
        }

        bool ValidateWallHit(RaycastHit h)
        {
            if (h.collider == null)
                return false;
            if (h.collider.transform.IsChildOf(Controller.transform) || h.collider.gameObject == Controller.gameObject)
                return false;
            if (h.distance < 0.01f)
                return false;
            float upDot = Mathf.Abs(Vector3.Dot(h.normal.normalized, Vector3.up));
            if (upDot > 0.6f)
                return false;
            if (!string.IsNullOrEmpty(Preset.WallRunAllowedTag) && h.collider.gameObject.CompareTag(Preset.WallRunAllowedTag))
                return false;
            return true;
        }

        void OnTryJump()
        {
            if (Controller.Grounded) return;
            if (Active) return;
            if (Time.time - lastWallJumpTime < Preset.WallJumpCooldown) return;

            if (IsFacingWall(out var hit))
            {
                lastWallNormal = hit.normal;
                Controller.JumpConsumed = true;

                if (Manager.TryStart(this))
                    lastWallJumpTime = Time.time;
            }
        }

        public override bool CanStartActivity()
        {
            if (Controller.Grounded)
                return false;

            if (!IsFacingWall(out var hit))
                return false;

            lastWallNormal = hit.normal;
            return true;
        }

        public override void OnActivityStarted()
        {
            base.OnActivityStarted();

            // compute vertical jump velocity from preset jump height and gravity, scaled by multiplier
            float baseJumpV = Mathf.Sqrt(Mathf.Max(0f, Preset.JumpHeight) * -2f * Physics.gravity.y * Preset.GravityScale);
            Controller.VerticalSpeed = baseJumpV * Preset.WallUpVelocityMultiplier;

            // apply horizontal away impulse along the wall normal and a component toward the camera
            Vector3 away = Vector3.ProjectOnPlane(-lastWallNormal, Vector3.up).normalized;
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = Controller.transform.forward; // fallback
            }

            Vector3 camFwd = Controller.CameraTransform.forward;
            camFwd = Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized;

            // build directional mix using configurable weights so away from wall is dominant
            Vector3 mixedDir = (away * Preset.WallAwayWeight + camFwd * Preset.WallCamWeight);
            if (mixedDir.sqrMagnitude <= 0.0001f)
                mixedDir = away;
            mixedDir.Normalize();

            // impulse along mixed direction scaled by AwayVelocity, plus a small camera-forward nudge
            Vector3 impulse = mixedDir * Preset.WallAwayVelocity + camFwd * Preset.WallCameraPush;

            // Preserve existing horizontal momentum and add impulse
            Vector3 currentFwd = Controller.ForwardVelocity;
            Vector3 currentHoriz = Vector3.ProjectOnPlane(currentFwd, Vector3.up);
            Vector3 newHoriz = currentHoriz + impulse;

            // enforce a minimum horizontal speed so wall-jump always gives some momentum
            float newMag = newHoriz.magnitude;
            if (newMag < Preset.WallMinSpeed)
            {
                if (newHoriz.sqrMagnitude <= 0.0001f)
                {
                    // fallback to away direction
                    newHoriz = mixedDir * Preset.WallMinSpeed;
                }
                else
                {
                    newHoriz = newHoriz.normalized * Preset.WallMinSpeed;
                }
            }

            // clamp horizontal magnitude to a reasonable max so wall-jump doesn't send player flying
            float maxHoriz = Preset.SprintSpeed * 1.5f;
            if (newHoriz.magnitude > maxHoriz)
            {
                newHoriz = newHoriz.normalized * maxHoriz;
            }

            Controller.ForwardVelocity = newHoriz;

            // notify jump event
            Controller.Jumped?.Invoke();

            // keep normal available for animation alignment during the active window
            wallJumpActiveUntil = Time.time + WallJumpActiveDuration;

            // activity is instantaneous - stop immediately
            Manager.ForceStop(this);
        }
    }
}
