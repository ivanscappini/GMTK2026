using UnityEngine;

namespace Hermes
{
    public class FPCameraLand : FPActivity
    {
        enum EState
        {
            Land,
            Recovery
        }

        EState state = EState.Land;
        float stateTimer = 0f;

        [SerializeField, Min(0f)] float MaxOffset = 1.0f;

        [SerializeField, Min(1f)] float MaxVerticalSpeed = 15f;

        [Header("Land State")]
        [SerializeField, Min(0.01f)] float LandDuration = 0.2f;
        [SerializeField] AnimationCurve LandCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Recovery State")]
        [SerializeField] float RecoverySmoothness = 1f;

        private Vector3 targetOffset;
        private Vector3 currentOffset;

        protected override void Awake()
        {
            base.Awake();

            Controller.Landed.AddListener(OnLanded);
            Controller.RequestCameraOffset += () =>
            {
                Controller.CameraPositionOffset += currentOffset;
            };
        }

        private void Update()
        {
            stateTimer += Time.deltaTime;
            
            if (Active)
            {
                if (state == EState.Land)
                {
                    float t = LandCurve.Evaluate(stateTimer / LandDuration);

                    currentOffset = Vector3.LerpUnclamped(Vector3.zero, targetOffset, t); //unclamped to allow for overshooting on the animation curve
                }
                else if (state == EState.Recovery)
                {
                    currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, RecoverySmoothness * Time.deltaTime);
                }

                if (state == EState.Land && stateTimer >= LandDuration)
                {
                    ChangeState(EState.Recovery);
                }
                else if (state == EState.Recovery && Vector3.Distance(currentOffset, Vector3.zero) <= 0.05f)
                {
                    TryStop(this);
                }
            }
        }

        protected override void StopActivity()
        {
            
        }

        void OnLanded()
        {
            float scale = Mathf.InverseLerp(0f, MaxVerticalSpeed, Mathf.Abs(Controller.VerticalVelocity));

            targetOffset = new Vector3(0f, -MaxOffset * scale, 0f);

            TryStart(this);

            ChangeState(EState.Land);
        }

        void ChangeState(EState newState)
        {
            state = newState;

            stateTimer = 0f;
        }
    }
}
