using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Moves an iris RectTransform toward the mouse position with a lazy lerp
/// and a clamped range of motion, so it stays subtle rather than snapping.
/// Pair this with a UI Mask (using the filled eye_mask sprite) as the
/// iris's parent for a hard visual clip as well — this script only handles
/// the *movement* clamp, the Mask handles the *render* clip.
///
/// Supports two tracking modes:
///
/// - Direct Screen Space: the eye's Canvas is the same one the player
///   actually sees (Screen Space - Overlay/Camera). Mouse position is read
///   straight from the input device.
///
/// - World Projected Screen: the eye's Canvas is rendered off-screen to a
///   RenderTexture by a dedicated UI camera, and that texture is displayed
///   on a mesh out in the 3D world (e.g. a monitor prop with CRT-style post
///   processing). In this mode we raycast from the player's real camera
///   through the mouse position into the world, find where that ray hits
///   the screen mesh, and convert the hit point's UV into the UI camera's
///   "virtual screen space" so the same RectTransform math applies.
///   Requires a MeshCollider (not a Box/primitive Collider) on the screen
///   mesh — only MeshCollider raycast hits return a usable textureCoord.
/// </summary>
[DisallowMultipleComponent]
public class PixelEyeTracker : MonoBehaviour
{
    public enum TrackingMode
    {
        DirectScreenSpace,
        WorldProjectedScreen
    }

    [Header("Tracking Mode")]
    [SerializeField] private TrackingMode trackingMode = TrackingMode.DirectScreenSpace;

    [Header("Eye References")]
    [Tooltip("RectTransform that defines the eye's bounds (e.g. the Mask object, or the outline object — should match the eye's on-screen size).")]
    [SerializeField] private RectTransform eyeBounds;

    [Tooltip("RectTransform of the iris Image (should be a child of the Mask object).")]
    [SerializeField] private RectTransform iris;

    [Header("Direct Screen Space (used when Tracking Mode = Direct Screen Space)")]
    [Tooltip("The Canvas the eye lives on.")]
    [SerializeField] private Canvas canvas;

    [Header("World Projected Screen (used when Tracking Mode = World Projected Screen)")]
    [Tooltip("The camera the PLAYER actually sees through — renders the 3D scene containing the projected screen mesh.")]
    [SerializeField] private Camera viewerCamera;
    [Tooltip("The dedicated camera that renders the eye's Canvas into a RenderTexture, which is then displayed on the screen mesh. This should be the same camera assigned as the Canvas's Render Camera.")]
    [SerializeField] private Camera uiRenderCamera;
    [Tooltip("MeshCollider on the in-world screen mesh that displays the UI's RenderTexture (must be a MeshCollider, not a Box/primitive Collider, so the raycast returns a valid UV).")]
    [SerializeField] private Collider screenCollider;
    [Tooltip("Layers the raycast is allowed to hit. Restrict this to just the screen mesh's layer so other props in the scene can't block it.")]
    [SerializeField] private LayerMask raycastMask = ~0;
    [Tooltip("Flip if the eye tracks backwards/mirrored horizontally — depends on your mesh's UV layout.")]
    [SerializeField] private bool flipU = false;
    [Tooltip("Flip if the eye tracks upside-down vertically — depends on your mesh's UV layout.")]
    [SerializeField] private bool flipV = false;
    [Tooltip("Shifts the UV center used for tracking, in UV units (typically -0.5 to 0.5). Use this if the mesh's UV (0.5, 0.5) doesn't line up with where the eye actually sits on the screen — e.g. the eye is a bit left/up of the mesh's true center. X: negative shifts the neutral gaze left, positive shifts it right. Y: negative shifts it down, positive shifts it up.")]
    [SerializeField] private Vector2 uvCenterOffset = Vector2.zero;
    [Tooltip("If true, the iris eases back to the eye's resting center when the mouse isn't pointed at the in-world screen at all.")]
    [SerializeField] private bool restCenterWhenNotHovering = true;

    [Tooltip("If true, the iris tracks across its FULL range of motion as the mouse crosses the FULL screen mesh (edge-to-edge). If false, uses Sensitivity Radius below for a gentler, dampened response instead.")]
    [SerializeField] private bool useFullScreenRange = true;

    [Header("Behaviour")]
    [Tooltip("0-1. Lower = lazier/slower eye movement, higher = snaps almost instantly.")]
    [SerializeField, Range(0.01f, 1f)] private float followSpeed = 0.06f;

    [Tooltip("How far (in tracked-screen pixels) the mouse needs to move from the eye before the iris reaches full deflection. Higher = gentler response. Only used when Use Full Screen Range is off (Direct Screen Space mode always uses this).")]
    [SerializeField] private float sensitivityRadius = 500f;

    [Tooltip("0-1. Scales how far within the outline the iris is allowed to travel. Lower values keep it closer to center for a subtler effect.")]
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
        if (eyeBounds == null || iris == null) return;
        if (!TryGetMouseScreenPosition(out Vector2 mouseScreenPos)) return;

        Vector2 halfSize = eyeBounds.rect.size * 0.5f;
        float maxX = Mathf.Max(0f, halfSize.x - irisRadiusPx) * maxDriftScale;
        float maxY = Mathf.Max(0f, halfSize.y - irisRadiusPx) * maxDriftScale;

        bool haveTarget;

        if (trackingMode == TrackingMode.DirectScreenSpace)
        {
            haveTarget = TryGetLocalMouseDirect(mouseScreenPos, out Vector2 localMouse);
            if (haveTarget)
            {
                Vector2 norm = new Vector2(localMouse.x / sensitivityRadius, localMouse.y / sensitivityRadius);
                float mag = norm.magnitude;
                if (mag > 1f) norm /= mag;
                targetPos = new Vector2(norm.x * maxX, norm.y * maxY);
            }
        }
        else
        {
            haveTarget = TryGetScreenUV(mouseScreenPos, out Vector2 uv);
            if (haveTarget)
            {
                if (useFullScreenRange)
                {
                    // Map UV [0,1] directly to [-1,1], so mouse at one edge of the
                    // screen mesh puts the iris at one edge of its own range, and
                    // the opposite edge puts it at the opposite edge — full sweep.
                    Vector2 norm = uv * 2f - Vector2.one;
                    float mag = norm.magnitude;
                    if (mag > 1f) norm /= mag;
                    targetPos = new Vector2(norm.x * maxX, norm.y * maxY);
                }
                else
                {
                    int rtWidth = uiRenderCamera.targetTexture != null ? uiRenderCamera.targetTexture.width : uiRenderCamera.pixelWidth;
                    int rtHeight = uiRenderCamera.targetTexture != null ? uiRenderCamera.targetTexture.height : uiRenderCamera.pixelHeight;
                    Vector2 virtualScreenPoint = new Vector2(uv.x * rtWidth, uv.y * rtHeight);

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(eyeBounds, virtualScreenPoint, uiRenderCamera, out Vector2 localMouse))
                    {
                        Vector2 norm = new Vector2(localMouse.x / sensitivityRadius, localMouse.y / sensitivityRadius);
                        float mag = norm.magnitude;
                        if (mag > 1f) norm /= mag;
                        targetPos = new Vector2(norm.x * maxX, norm.y * maxY);
                    }
                    else
                    {
                        haveTarget = false;
                    }
                }
            }
        }

        if (!haveTarget && trackingMode == TrackingMode.WorldProjectedScreen && restCenterWhenNotHovering)
        {
            targetPos = Vector2.zero;
        }

        currentPos = Vector2.Lerp(currentPos, targetPos, followSpeed);
        iris.anchoredPosition = currentPos;
    }

    private bool TryGetLocalMouseDirect(Vector2 mouseScreenPos, out Vector2 localMouse)
    {
        localMouse = default;
        if (canvas == null) return false;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(eyeBounds, mouseScreenPos, cam, out localMouse);
    }

    /// <summary>
    /// Raycasts from the viewer camera through the mouse position and returns
    /// the UV coordinate (0-1 across the screen mesh) that the ray hit, flipped
    /// per the inspector toggles. Requires screenCollider to be a MeshCollider
    /// on a mesh whose UVs span cleanly 0-1 across just the visible screen area
    /// — NOT a collider on an entire multi-part prop, whose UV atlas likely
    /// devotes only a small, oddly-placed island to the actual screen.
    /// </summary>
    private bool TryGetScreenUV(Vector2 mouseScreenPos, out Vector2 uv)
    {
        uv = default;
        if (viewerCamera == null || screenCollider == null) return false;
        if (trackingMode == TrackingMode.WorldProjectedScreen && !useFullScreenRange && uiRenderCamera == null) return false;

        Ray ray = viewerCamera.ScreenPointToRay(mouseScreenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastMask)) return false;
        if (hit.collider != screenCollider) return false;

        uv = hit.textureCoord;
        if (flipU) uv.x = 1f - uv.x;
        if (flipV) uv.y = 1f - uv.y;
        uv -= uvCenterOffset;
        return true;
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