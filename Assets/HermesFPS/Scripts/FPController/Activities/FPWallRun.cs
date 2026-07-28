using UnityEngine;

namespace Hermes
{
    public class FPWallRun : FPActivity
    {
        float runTimer = 0f;
        Vector3 wallNormal = Vector3.zero;
        Vector3 wallForward = Vector3.zero;
        Collider wallCollider = null;

        public Vector3 WallNormal => wallNormal;
        public Vector3 WallForward => wallForward;
        public Collider WallCollider => wallCollider;

        float lastDetachTime = -999f;
        bool requireRepress = false;
        public bool RequiresRepress => requireRepress;

        float currentTilt = 0f;
        float wallSide = 0f;

        [Header("Input")]
        [Tooltip("Time (seconds) the jump must be held before attaching to a wall-run. Short taps trigger a wall-jump instead.")]
        [SerializeField] float HoldToWallRunThreshold = 0.1f;

        bool pendingStart = false;
        float pendingStartTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            Controller.RequestCameraOffset += ApplyCameraTilt;
        }

        void OnDestroy()
        {
            if (Controller != null)
                Controller.RequestCameraOffset -= ApplyCameraTilt;
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

        bool IsWallValid(out RaycastHit hit, Vector3 preferNormal = default)
        {
            hit = default;
            float bestScore = float.MinValue;
            bool found = false;

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
                    if (ValidateWallHit(h))
                    {
                        float score = ScoreHit(h, preferNormal);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            hit = h;
                            found = true;
                        }
                    }
                }

                var rayHits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (var rh in rayHits)
                {
                    if (ValidateWallHit(rh))
                    {
                        float score = ScoreHit(rh, preferNormal);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            hit = rh;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        float ScoreHit(RaycastHit h, Vector3 preferNormal)
        {
            float score = -h.distance;
            if (preferNormal.sqrMagnitude > 0.01f)
                score += Vector3.Dot(h.normal, preferNormal) * 2f;
            return score;
        }

        bool ValidateWallHit(RaycastHit h)
        {
            if (h.collider == null) return false;
            if (h.collider.transform.IsChildOf(Controller.transform) || h.collider.gameObject == Controller.gameObject)
                return false;
            if (h.distance < 0.01f) return false;

            float upDot = Mathf.Abs(Vector3.Dot(h.normal.normalized, Vector3.up));
            if (upDot > 0.3f) return false;

            if (!string.IsNullOrEmpty(Preset.WallRunAllowedTag) && !h.collider.gameObject.CompareTag(Preset.WallRunAllowedTag))
                return false;

            return true;
        }

        bool IsAlignedWithWall(Vector3 normal)
        {
            Vector3 wf = Vector3.Cross(normal, Vector3.up).normalized;

            Vector3 horizVel = Controller.ForwardVelocity;
            horizVel.y = 0f;

            if (horizVel.sqrMagnitude < 0.1f)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(Controller.CameraTransform.forward, Vector3.up).normalized;
                return Mathf.Abs(Vector3.Dot(camFwd, wf)) >= Preset.WallRunMinAlignment;
            }

            return Mathf.Abs(Vector3.Dot(horizVel.normalized, wf)) >= Preset.WallRunMinAlignment;
        }

        public override bool CanStartActivity()
        {
            if (Controller.Grounded) return false;
            if (Time.time - lastDetachTime < Preset.WallRunReattachDelay) return false;
            if (!IsWallValid(out var hit)) return false;
            if (!IsAlignedWithWall(hit.normal)) return false;
            return true;
        }

        public override void OnActivityStarted()
        {
            base.OnActivityStarted();
            runTimer = 0f;

            if (!IsWallValid(out var hit))
            {
                Manager.ForceStop(this);
                return;
            }

            wallNormal = hit.normal;
            wallCollider = hit.collider;
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(wallForward, Controller.CameraTransform.forward) < 0f)
                wallForward = -wallForward;

            wallSide = Vector3.Dot(Controller.transform.right, -wallNormal) > 0f ? 1f : -1f;

            Vector3 currentHoriz = Controller.ForwardVelocity;
            currentHoriz.y = 0f;
            Vector3 projected = currentHoriz - Vector3.Dot(currentHoriz, wallNormal) * wallNormal;

            float speed = Mathf.Max(projected.magnitude, Preset.WallRunSpeed);
            Controller.ForwardVelocity = projected.normalized * speed;
            Controller.VerticalSpeed = 0f;
        }

        private void Update()
        {
            float targetTilt = Active ? Preset.WallRunCameraTilt * wallSide : 0f;
            currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, Preset.WallRunCameraTiltSpeed * Time.deltaTime);

            bool holding = Controller.JumpInput;

            if (!Active)
            {
                if (requireRepress)
                {
                    if (!holding)
                        requireRepress = false;
                    pendingStart = false;
                    return;
                }

                if (Controller.Grounded || !holding)
                {
                    if (pendingStart && !holding)
                    {
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

                if (!pendingStart)
                {
                    if (IsWallValid(out var hit) && IsAlignedWithWall(hit.normal))
                    {
                        pendingStart = true;
                        pendingStartTimer = 0f;
                    }
                    return;
                }

                if (IsWallValid(out var h2) && IsAlignedWithWall(h2.normal))
                {
                    pendingStartTimer += Time.deltaTime;
                    if (pendingStartTimer >= HoldToWallRunThreshold
                        && Time.time - lastDetachTime >= Preset.WallRunReattachDelay)
                    {
                        if (Manager.TryStart(this))
                            pendingStart = false;
                    }
                }

                return;
            }

            // === Active wall run ===
            runTimer += Time.deltaTime;

            if (runTimer >= Preset.WallRunMaxDuration)
            {
                Detach();
                return;
            }

            if (!holding)
            {
                KickOff();
                return;
            }

            if (!IsWallValid(out var hitNow, wallNormal))
            {
                KickOff();
                return;
            }

            float normalDot = Vector3.Dot(wallNormal, hitNow.normal);
            if (normalDot < Preset.WallRunMinNormalDot)
            {
                KickOff();
                return;
            }

            Vector3 rayOrigin = Controller.transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, wallForward, out RaycastHit forwardHit, 0.8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!forwardHit.collider.transform.IsChildOf(Controller.transform)
                    && forwardHit.collider.gameObject != Controller.gameObject)
                {
                    float upDot = Mathf.Abs(Vector3.Dot(forwardHit.normal, Vector3.up));
                    float normalSimilarity = Vector3.Dot(forwardHit.normal, wallNormal);
                    if (upDot < 0.3f && normalSimilarity < 0.7f)
                    {
                        KickOff();
                        return;
                    }
                }
            }

            wallNormal = hitNow.normal;
            wallCollider = hitNow.collider;
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(wallForward, Controller.CameraTransform.forward) < 0f)
                wallForward = -wallForward;

            Vector3 currentHoriz = Controller.ForwardVelocity;
            currentHoriz.y = 0f;
            Vector3 projected = currentHoriz - Vector3.Dot(currentHoriz, wallNormal) * wallNormal;

            Vector3 target = wallForward * Preset.WallRunSpeed;
            Controller.ForwardVelocity = Vector3.Lerp(projected, target, Preset.WallRunBlend * Time.deltaTime);
            Controller.VerticalSpeed += Physics.gravity.y * Preset.GravityScale * Preset.WallRunGravityScale * Time.deltaTime;
            if (Preset.WallRunLiftSpeed != 0f)
                Controller.VerticalSpeed = Mathf.MoveTowards(Controller.VerticalSpeed, Preset.WallRunLiftSpeed, Mathf.Abs(Preset.WallRunLiftSpeed) * 3f * Time.deltaTime);
        }

        void ApplyCameraTilt()
        {
            if (Mathf.Abs(currentTilt) > 0.01f)
                Controller.CameraRotationOffset *= Quaternion.Euler(0f, 0f, currentTilt);
        }

        void Detach()
        {
            requireRepress = true;
            lastDetachTime = Time.time;
            Manager.ForceStop(this);
        }

        void KickOff()
        {
            Controller.VerticalSpeed = Preset.WallRunKickOffVerticalSpeed;

            Vector3 camFwd = Vector3.ProjectOnPlane(Controller.CameraTransform.forward, Vector3.up).normalized;
            Vector3 away = Vector3.ProjectOnPlane(wallNormal, Vector3.up).normalized;

            Vector3 kickDir = (away * Preset.WallAwayWeight + camFwd * Preset.WallCamWeight).normalized;

            Vector3 currentHoriz = Vector3.ProjectOnPlane(Controller.ForwardVelocity, Vector3.up);
            Vector3 newHoriz = currentHoriz + kickDir * Preset.WallAwayVelocity + camFwd * Preset.WallCameraPush;

            float maxHoriz = Preset.SprintSpeed * 1.5f;
            if (newHoriz.magnitude > maxHoriz)
                newHoriz = newHoriz.normalized * maxHoriz;
            if (newHoriz.magnitude < Preset.WallMinSpeed)
                newHoriz = newHoriz.normalized * Preset.WallMinSpeed;

            Controller.ForwardVelocity = newHoriz;

            requireRepress = true;
            lastDetachTime = Time.time;
            Manager.ForceStop(this);
        }

        public override void OnActivityStopped()
        {
            base.OnActivityStopped();
            requireRepress = true;
            runTimer = 0f;
            wallNormal = Vector3.zero;
            wallForward = Vector3.zero;
            wallCollider = null;
            pendingStart = false;
        }
    }
}
