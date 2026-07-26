using UnityEngine;

namespace Hermes
{
    // Wall run activity: hold-to-run along a vertical wall. Starts when player is near an allowed wall and holds jump.
    public class FPWallRun : FPActivity
    {
        float runTimer = 0f;
        Vector3 wallNormal = Vector3.zero;
        Vector3 wallForward = Vector3.zero;
        Collider wallCollider = null;
        // expose read-only access for animation/code
        public Vector3 WallNormal => wallNormal;
        public Vector3 WallForward => wallForward;
        public Collider WallCollider => wallCollider;
        float lastKickTime = -999f;
        Quaternion cameraInitialLocalRot = Quaternion.identity;
        Quaternion cameraParentStartRot = Quaternion.identity;
        float cameraAlignTimer = 0f;
        float cameraAlignDuration = 0.15f;
        [Header("Input")]
        [Tooltip("Time (seconds) the jump must be held before attaching to a wall-run. Short taps will perform a wall-jump instead.")]
        [SerializeField] float HoldToWallRunThreshold = 0.35f;

        bool pendingStart = false;
        float pendingStartTimer = 0f;

        protected override void Awake()
        {
            base.Awake();

            // listen for jump attempts / releases
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

        bool IsWallValid(out RaycastHit hit, bool requireTag = true)
        {
            hit = default;

            Vector3 origin = Controller.transform.position + Vector3.up * 1f;
            float dist = Preset.WallCheckDistance;
            float sphereRadius = 0.15f;

            Vector3[] dirs = new Vector3[]
            {
                Controller.transform.right,
                -Controller.transform.right,
                (Controller.transform.forward + Controller.transform.right).normalized,
                (Controller.transform.forward - Controller.transform.right).normalized,
                (-Controller.transform.forward + Controller.transform.right).normalized,
                (-Controller.transform.forward - Controller.transform.right).normalized,
            };

            foreach (var dir in dirs)
            {
                if (Physics.SphereCast(origin, sphereRadius, dir, out var h, dist - sphereRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (ValidateWallHit(h, origin, dir, requireTag))
                    {
                        hit = h;
                        return true;
                    }
                }

                var rayHits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (var rh in rayHits)
                {
                    if (ValidateWallHit(rh, origin, dir, requireTag))
                    {
                        hit = rh;
                        return true;
                    }
                }
            }

            return false;
        }

        bool ValidateWallHit(RaycastHit h, Vector3 origin, Vector3 dir, bool requireTag = true)
        {
            if (h.collider == null)
                return false;
            if (h.collider.transform.IsChildOf(Controller.transform) || h.collider.gameObject == Controller.gameObject)
                return false;
            if (h.distance < 0.01f)
                return false;

            float upDot = Mathf.Abs(Vector3.Dot(h.normal.normalized, Vector3.up));
            if (upDot > 0.3f)
                return false;

            if (requireTag && !string.IsNullOrEmpty(Preset.WallRunAllowedTag) && !h.collider.gameObject.CompareTag(Preset.WallRunAllowedTag))
                return false;

            return true;
        }

        void OnTryJump()
        {
            // not used: wall-run is started/stopped by checking Controller.JumpInput in Update (hold behavior)
        }

        public override bool CanStartActivity()
        {
            // can start in air only
            if (Controller.Grounded) return false;

            if (!IsWallValid(out var hit)) return false;

            return true;
        }

        public override void OnActivityStarted()
        {
            base.OnActivityStarted();

            runTimer = 0f;

            // determine wall direction
            if (!IsWallValid(out var hit))
            {
                Manager.ForceStop(this);
                return;
            }

            wallNormal = hit.normal;
            wallCollider = hit.collider;
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

            // align forward with camera direction if needed
            if (Vector3.Dot(wallForward, Controller.CameraTransform.forward) < 0f) wallForward = -wallForward;

            // lock body's forward to wallForward while allowing limited camera yaw
            Controller.ForwardVelocity = wallForward * Preset.WallRunSpeed;

            // store camera local rotation so we can smoothly clamp relative yaw
            cameraInitialLocalRot = Controller.CameraTransform.localRotation;
            // prepare smooth alignment: capture starting parent rotation and reset timer
            cameraParentStartRot = Controller.CameraTransform.parent.rotation;
            cameraAlignTimer = 0f;
        }

        private void Update()
        {
            bool holding = Controller.JumpInput;

            if (!Active)
            {
                // Cancel pending if landed or not holding
                if (Controller.Grounded || !holding)
                {
                    if (pendingStart && !holding)
                    {
                        // Released during pending → tap → fire wall-jump
                        pendingStart = false;
                        if (Time.frameCount != Controller.LastTryJumpFrame)
                        {
                            Controller.LastTryJumpFrame = Time.frameCount;
                            Controller.TryJump?.Invoke();
                        }
                    }
                    pendingStart = false;
                    return;
                }

                // Airborne and holding jump: start pending near runnable walls only
                if (!pendingStart)
                {
                    if (IsWallValid(out _))
                    {
                        pendingStart = true;
                        pendingStartTimer = 0f;
                    }
                    return;
                }

                // Pending active: only accumulate timer toward wall-run on runnable walls
                if (IsWallValid(out _))
                {
                    pendingStartTimer += Time.deltaTime;
                    if (pendingStartTimer >= HoldToWallRunThreshold
                        && Time.time - lastKickTime >= Preset.WallRunReattachDelay)
                    {
                        if (Manager.TryStart(this))
                        {
                            pendingStart = false;
                        }
                    }
                }

                return;
            }

            // === Active wall run ===
            runTimer += Time.deltaTime;

            if (runTimer >= Preset.WallRunMaxDuration)
            {
                Manager.ForceStop(this);
                return;
            }

            if (!holding)
            {
                KickOff(default);
                lastKickTime = Time.time;
                return;
            }

            RaycastHit hitNow;
            if (!IsWallValid(out hitNow))
            {
                KickOff(hitNow);
                lastKickTime = Time.time;
                return;
            }

            wallNormal = hitNow.normal;
            wallCollider = hitNow.collider;
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(wallForward, Controller.CameraTransform.forward) < 0f) wallForward = -wallForward;

            Vector3 targetHoriz = wallForward * Preset.WallRunSpeed;
            Vector3 current = Controller.ForwardVelocity;
            Vector3 blended = Vector3.Lerp(current, targetHoriz, Mathf.Clamp01(Preset.WallRunBlend * Time.deltaTime));

            Controller.ForwardVelocity = blended;
            Controller.VerticalSpeed = Mathf.Lerp(Controller.VerticalSpeed, Preset.WallRunLiftRate, Mathf.Clamp01(Preset.WallRunBlend * Time.deltaTime));

            float maxYaw = Preset.WallRunCameraYawLimit;
            var camParent = Controller.CameraTransform.parent;
            Vector3 flatForward = Vector3.ProjectOnPlane(camParent.forward, Vector3.up).normalized;
            float angle = Vector3.SignedAngle(wallForward, flatForward, Vector3.up);

            float clamped = Mathf.Clamp(angle, -maxYaw, maxYaw);
            Quaternion targetParentRot = Quaternion.LookRotation(Quaternion.Euler(0f, clamped, 0f) * wallForward, Vector3.up);

            if (cameraAlignTimer < cameraAlignDuration)
            {
                cameraAlignTimer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cameraAlignTimer / cameraAlignDuration));
                camParent.rotation = Quaternion.Slerp(cameraParentStartRot, targetParentRot, t);
            }
            else
            {
                camParent.rotation = Quaternion.Slerp(camParent.rotation, targetParentRot, Preset.WallRunCameraBlend * Time.deltaTime);
            }
        }

        public override void OnActivityStopped()
        {
            base.OnActivityStopped();

            runTimer = 0f;
            wallNormal = Vector3.zero;
            wallForward = Vector3.zero;
            wallCollider = null;
            pendingStart = false;
        }

        void KickOff(RaycastHit hit)
        {
            // compute vertical jump velocity from preset jump height and gravity (reduced to avoid excessive loft)
            float baseJumpV = Mathf.Sqrt(Mathf.Max(0f, Preset.JumpHeight) * -2f * Physics.gravity.y * Preset.GravityScale);
            Controller.VerticalSpeed = baseJumpV * (Preset.WallUpVelocityMultiplier * 0.75f);

            // compute horizontal kick direction: favor away-from-wall more strongly, with camera as secondary
            Vector3 away = Vector3.zero;
            if (hit.collider != null)
            {
                away = Vector3.ProjectOnPlane(-hit.normal, Vector3.up).normalized;
            }

            Vector3 camFwd = Controller.CameraTransform.forward;
            camFwd = Vector3.ProjectOnPlane(camFwd, Vector3.up).normalized;

            Vector3 kickDir;
            if (away.sqrMagnitude > 0.0001f)
            {
                // favor camera-forward more strongly than away from wall
                kickDir = (away * 0.3f + camFwd * 0.7f).normalized;
            }
            else
            {
                kickDir = camFwd;
            }

            float kickSpeed = Preset.WallAwayVelocity * 1.3f;

            // preserve some existing horizontal momentum and add the kick
            Vector3 currentHoriz = Vector3.ProjectOnPlane(Controller.ForwardVelocity, Vector3.up);
            Vector3 newHoriz = currentHoriz + kickDir * kickSpeed;

            // clamp horizontal magnitude
            float maxHoriz = Preset.SprintSpeed * 1.5f;
            if (newHoriz.magnitude > maxHoriz) newHoriz = newHoriz.normalized * maxHoriz;

            Controller.ForwardVelocity = newHoriz;

            // stop the activity immediately
            Manager.ForceStop(this);
        }
    }
}
