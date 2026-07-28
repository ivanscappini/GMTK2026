using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Moves an iris RectTransform toward the mouse position with a lazy lerp
/// and a clamped range of motion, so it stays subtle rather than snapping
/// wildly for small UI elements. Pair this with a UI Mask (using the filled
/// eye_mask sprite) as the iris's parent for a hard visual clip as well —
/// this script handles the *movement* clamp, the Mask handles the *render* clip.
/// </summary>
[DisallowMultipleComponent]
public class PixelEyeTracker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform that defines the eye's bounds (e.g. the Mask object, or the outline object — should match the eye's on-screen size).")]
    [SerializeField] private RectTransform eyeBounds;

    [Tooltip("RectTransform of the iris Image (should be a child of the Mask object).")]
    [SerializeField] private RectTransform iris;

    [Tooltip("The parent Canvas. Needed to correctly convert screen space to local UI space.")]
    [SerializeField] private Canvas canvas;

    [Header("Behaviour")]
    [Tooltip("0-1. Lower = lazier/slower eye movement, higher = snaps almost instantly.")]
    [SerializeField, Range(0.01f, 1f)] private float followSpeed = 0.06f;

    [Tooltip("How far (in screen pixels) the mouse needs to move from the eye before the iris reaches its full deflection. Higher = gentler response, since the mouse has to travel further to have the same effect. For a small UI element, keep this much larger than the element itself.")]
    [SerializeField] private float sensitivityRadius = 500f;

    [Tooltip("0-1. Scales how far within the outline the iris is allowed to travel. 1 = can reach almost to the edge, lower values keep it closer to center for a subtler effect.")]
    [SerializeField, Range(0.05f, 1f)] private float maxDriftScale = 0.5f;

    [Tooltip("Radius (in canvas pixels) of the iris sprite, used to keep its edge inside the outline rather than just its center.")]
    [SerializeField] private float irisRadiusPx = 6f;

    private Vector2 targetPos;
    private Vector2 currentPos;

    private void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (eyeBounds == null || iris == null || canvas == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!TryGetMouseScreenPosition(out Vector2 mouseScreenPos)) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                eyeBounds, mouseScreenPos, cam, out Vector2 localMouse))
        {
            Vector2 halfSize = eyeBounds.rect.size * 0.5f;
            float maxX = Mathf.Max(0f, halfSize.x - irisRadiusPx) * maxDriftScale;
            float maxY = Mathf.Max(0f, halfSize.y - irisRadiusPx) * maxDriftScale;

            // Normalize against sensitivityRadius (a real screen distance), NOT
            // against the eye's own tiny half-size — otherwise a small element
            // saturates to full deflection the instant the mouse leaves it.
            Vector2 norm = new Vector2(
                localMouse.x / sensitivityRadius,
                localMouse.y / sensitivityRadius
            );

            float mag = norm.magnitude;
            if (mag > 1f) norm /= mag;

            targetPos = new Vector2(norm.x * maxX, norm.y * maxY);
        }

        currentPos = Vector2.Lerp(currentPos, targetPos, followSpeed);
        iris.anchoredPosition = currentPos;
    }

    /// <summary>
    /// Reads the mouse's screen position from whichever input backend is
    /// active in Player Settings > Active Input Handling, so this script
    /// works whether the project uses the new Input System, the legacy
    /// Input Manager, or both.
    /// </summary>
    private bool TryGetMouseScreenPosition(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }
        screenPos = default;
        return false;
#else
        screenPos = Input.mousePosition;
        return true;
#endif
    }
}