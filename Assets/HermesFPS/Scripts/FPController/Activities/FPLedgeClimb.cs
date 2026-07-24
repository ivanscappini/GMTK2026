using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hermes
{
    public enum ELedgeClimbType
    {
        Step = 0,
        Vault = 1,
        Climb = 2
    }


    public class FPLedgeClimb : FPActivity
    {
        public float WallCheckDistance = 0.7f;

        [SerializeField] AnimationCurve ClimbEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        Vector3 ledgeTop = Vector3.zero;

        [HideInInspector] public Vector3 LedgeTop => ledgeTop;

        public ELedgeClimbType LedgeClimbType { get; private set; }


        Vector3 CameraPosition => Controller.CameraTransform.position;

        Vector3 ForwardVector => Controller.transform.forward;


        [Header("Climb Duration")]
        [SerializeField] Vector2 DurationRangeClimb = new Vector2(0.7f, 1.2f);
        [SerializeField] Vector2 DurationRangeVault = new Vector2(0.5f, 0.8f);
        [SerializeField] Vector2 DurationRangeStep = new Vector2(0.3f, 0.6f);


        public float ClimbDuration { get; private set; }
        private float climbTimer = 0f;
        private Vector3 climbStartPosition = Vector3.zero;



        [Header("Bezier Curve")]
        [SerializeField] float BezierControlPointDistance = 1f;

        [Header("Bezier Visualization")]
        [SerializeField] bool VisualizeBezierCurve = false;

        [SerializeField] Vector3 VisualizedLocalStartPos = new Vector3(0f, 0, 1f);
        [SerializeField] Vector3 VisualizedLocalTargetPos = new Vector3(0f, 2f, 2f);


        #region Unity Methods


        private void OnDrawGizmos()
        {
            if (VisualizeBezierCurve == false)
            {
                return;
            }

            Vector3 startPosition = transform.TransformPoint(VisualizedLocalStartPos);
            Vector3 endPosition = transform.TransformPoint(VisualizedLocalTargetPos);

            Vector3 controlPoint1 = ComputeBezierControlPoint1(startPosition);
            Vector3 controlPoint2 = ComputeBezierControlPoint2(startPosition, endPosition);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPosition, 0.05f);
            Gizmos.DrawSphere(endPosition, 0.05f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(controlPoint1, 0.05f);
            Gizmos.DrawLine(startPosition, controlPoint1);
            Gizmos.DrawSphere(controlPoint2, 0.05f);
            Gizmos.DrawLine(endPosition, controlPoint2);

            Gizmos.color = Color.green;

            int pointCount = 30;

            Vector3 previousPoint = startPosition;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (float)pointCount;

                Vector3 point = Bezier(t, startPosition, controlPoint1, endPosition, controlPoint2);

                Gizmos.DrawLine(previousPoint, point);

                previousPoint = point;
            }
        }


        private void Update()
        {
            if (!Active)
            {
                CheckLedges();
            }
            else
            {
                UpdateClimbing();
            }
        }

        #endregion


        #region Activity Override Methods

        public override void OnActivityStarted()
        {
            climbTimer = 0f;
            climbStartPosition = Controller.transform.position;

            Controller.MovementEnabled = false;
            Controller.CharacterController.enabled = false;

            ComputeClimbDuration();
        }

        public override void OnActivityStopped()
        {
            Controller.MovementEnabled = true;
            Controller.CharacterController.enabled = true;
        }

        #endregion


        #region Bezier Utility Methods

        static Vector3 Bezier(float t, Vector3 startPoint, Vector3 controlPoint1, Vector3 endPoint, Vector3 controlPoint2)
        {
            float it = 1f - t;
            float it2 = it * it;
            float t2 = t * t;

            return it2 * it * startPoint
                     + 3f * it2 * t * controlPoint1
                     + 3f * it * t2 * controlPoint2
                     + t2 * t * endPoint;
        }


        Vector3 ComputeBezierControlPoint1(Vector3 startPoint)
        {
            return startPoint + Vector3.up * BezierControlPointDistance;
        }

        Vector3 ComputeBezierControlPoint2(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 direction = (endPoint - startPoint).normalized;

            return endPoint + (Vector3.up * 1.5f - direction).normalized * BezierControlPointDistance;
        }



        Vector3 Bezier(float t, Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 controlPoint1 = ComputeBezierControlPoint1(startPoint);
            Vector3 controlPoint2 = ComputeBezierControlPoint2(startPoint, endPoint);

            return Bezier(t, startPoint, controlPoint1, endPoint, controlPoint2);
        }

        #endregion


        #region Collision Detection Methods

        bool RaycastWall(float offset = 0f)
        {
            Ray ray = new Ray(CameraPosition - Vector3.up * offset, ForwardVector);

            if (Physics.Raycast(ray, WallCheckDistance, Preset.ObstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return false;
        }


        bool IsFacingWall(out float offset)
        {
            for (offset = 0f; offset <= 1.7f; offset += 0.3f)
            {
                if (RaycastWall(offset))
                {
                    return true;
                }
            }

            return false;
        }


        void CheckLedges()
        {
            bool movingForward = Vector3.Dot(Controller.HorizontalVelocity, Controller.transform.forward) > 0f;

            if (!movingForward)
            {
                return;
            }


            if (!IsFacingWall(out float wallCheckOffset))
            {
                return;
            }


            // Getting the ledge top
            Vector3 rayOrigin = CameraPosition + Vector3.up * 1f + ForwardVector * WallCheckDistance;

            // we start the ray at the offset where a wall was detected
            // this allows proper detection of shorter walls or walls below the waist.
            rayOrigin -= Vector3.up * wallCheckOffset;

            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit ledgeHit, 1.5f, Preset.ObstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                // checking if the surface is fairly flat, and there is no overlap with anything.
                bool flatSurface = Vector3.Dot(Vector3.up, ledgeHit.normal) > 0.8f;
                bool overlap = CheckOverlap(ledgeHit.point);

                if (flatSurface && !overlap)
                {
                    this.ledgeTop = ledgeHit.point;

                    float localLedgeHeight = ledgeTop.y - Controller.transform.position.y;

                    if (localLedgeHeight <= 0.5f)
                    {
                        // this is a step-over
                        LedgeClimbType = ELedgeClimbType.Step;

                        if (Controller.Grounded)
                        {
                            return;
                        }

                    }
                    else if (localLedgeHeight <= 1f)
                    {
                        // this is a vault
                        LedgeClimbType = ELedgeClimbType.Vault;
                    }
                    else
                    {
                        // this is a climb
                        LedgeClimbType = ELedgeClimbType.Climb;

                        if (Controller.Grounded)
                        {
                            return;
                        }
                    }

                    Manager.TryStart(this);
                }
            }
        }


        bool CheckOverlap(Vector3 ledgeTop)
        {
            float radius = 0.5f;
            float skin = 0.05f;  // Small offset to prevent false positives
            float playerHeight = 2f;

            Vector3 bottom = ledgeTop + Vector3.up * radius;
            Vector3 top = ledgeTop + Vector3.up * (playerHeight - radius);

            return Physics.CheckCapsule(bottom, top, radius - skin, Preset.ObstacleLayerMask, QueryTriggerInteraction.Ignore);
        }

        #endregion


        #region Climbing Methods


        void ComputeClimbDuration()
        {


            Vector2 range = new Vector2();

            float forwardSpeed = Controller.ForwardVelocity.magnitude;

            float speed = forwardSpeed;


            float speedRatio = Mathf.InverseLerp(0f, Preset.SprintSpeed, forwardSpeed);

            if (LedgeClimbType == ELedgeClimbType.Climb)
            {
                range = DurationRangeClimb;

                speed *= 0.5f;
            }
            else if (LedgeClimbType == ELedgeClimbType.Vault)
            {
                range = DurationRangeVault;

                speed *= 1.35f;
            }
            else
            {
                range = DurationRangeStep;

                speed *= 0.9f;
            }

            ClimbDuration = Mathf.Lerp(range.y, range.x, speedRatio);
            ClimbDuration = Mathf.Max(0.01f, ClimbDuration);

            Controller.ForwardVelocity = Controller.transform.forward * speed;

            //float distance = Vector3.Distance(climbStartPosition, ledgeTop) * 1.2f;

            //ClimbDuration = distance / Mathf.Max(forwardSpeed, 0.001f);

            //ClimbDuration = Mathf.Clamp(ClimbDuration, 0.4f, 1.5f);

            //Debug.Log($"Climb Duration = {ClimbDuration}");
        }


        void UpdateClimbing()
        {
            Controller.VerticalSpeed = 0f;

            float actualClimbDuration = ClimbDuration;

            float t = climbTimer / actualClimbDuration;

            t = ClimbEaseCurve.Evaluate(t);


            if (LedgeClimbType == ELedgeClimbType.Climb)
            {
                // bezier curve approach
                Controller.transform.position = Bezier(t, climbStartPosition, ledgeTop);
            }
            else
            {
                // linear approach
                Controller.transform.position = Vector3.Lerp(climbStartPosition, ledgeTop, t);
            }


            float distance = Vector3.Distance(Controller.transform.position, LedgeTop);

            if (climbTimer >= actualClimbDuration || distance <= 0.1f)
            {
                Manager.TryStop(this);

                return;
            }

            climbTimer += Time.deltaTime;
        }

        #endregion


    }
}