using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// A click/drag-to-set segmented bar, like the volume meters in retro TV/VCR
/// OSD menus. Deliberately NOT built on Toggle/ToggleGroup: those are for
/// exclusive single-selection (radio buttons), but this needs cumulative
/// fill � clicking segment 6 lights segments 1-6 together, not just segment
/// 6 alone. Instead, the whole bar is one clickable/draggable region that
/// computes a fill count directly from pointer position.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SegmentedSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Segments")]
    [Tooltip("Prefab for a single segment � just needs an Image component. Not a Button/Toggle.")]
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private int segmentCount = 10;
    [SerializeField] private Sprite segmentOffSprite;
    [SerializeField] private Sprite segmentOnSprite;

    [Header("Value")]
    [Tooltip("Starting value, 0-1 normalized.")]
    [SerializeField, Range(0f, 1f)] private float value = 1f;
    [Tooltip("If true, dragging across the bar updates the value continuously. If false, only clicking/releasing sets it.")]
    [SerializeField] private bool allowDrag = true;

    public UnityEvent<float> OnValueChanged;
    public UnityEvent OnSegmentStepChanged;
    public UnityEvent OnMuteToggled;

    private readonly List<Image> segments = new List<Image>();
    private int lastFilledCount = -1;
    private RectTransform rect;

    [Header("Toggle / Icon")]
    [Tooltip("Button that toggles mute on/off. Its Image component is used for sprite swapping automatically.")]
    [SerializeField] private UnityEngine.UI.Button toggleButton;
    [Tooltip("Sprite shown on the button when muted.")]
    [SerializeField] private Sprite mutedSprite;

    [Header("Volume Level Sprites (optional, used when unmuted)")]
    [Tooltip("Shown when unmuted and value <= 0.1. Falls back to Mid or High if not set.")]
    [SerializeField] private Sprite volumeLowSprite;
    [Tooltip("Shown when unmuted and value <= 0.6.")]
    [SerializeField] private Sprite volumeMidSprite;
    [Tooltip("Shown when unmuted and value > 0.6.")]
    [SerializeField] private Sprite volumeHighSprite;
    [Tooltip("If true, muting the slider sets value to 0 and restores previous value on unmute.")]
    [SerializeField] private bool muteOnDisable = true;

    private Image toggleImage;
    private Canvas parentCanvas;
    private bool isActive = true;
    private float storedValue = 1f;

    public float Value => value;
    public bool IsMuted => !isActive;

    private void Awake()
    {
        rect = (RectTransform)transform;
        BuildSegments();
        Refresh();
        // cache canvas to convert screen->local coordinates reliably
        parentCanvas = GetComponentInParent<Canvas>();
        // wire toggle button if present
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleButtonClicked);
            toggleButton.onClick.AddListener(ToggleButtonClicked);
            toggleImage = toggleButton.GetComponent<Image>();
            toggleButton.transition = Selectable.Transition.None;
        }

        // initialize sprite state
        UpdateToggleSpriteByValue();
    }

    private void BuildSegments()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        segments.Clear();

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject go = Instantiate(segmentPrefab, transform);
            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogError("SegmentedSlider: segmentPrefab needs an Image component.", this);
                continue;
            }
            segments.Add(img);
        }
    }

    public void SetValue(float newValue, bool notify = true)
    {
        value = Mathf.Clamp01(newValue);
        Refresh();
        if (notify) OnValueChanged?.Invoke(value);
    }

    private void Refresh()
    {
        int filled = Mathf.RoundToInt(value * segmentCount);
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].sprite = i < filled ? segmentOnSprite : segmentOffSprite;
        }
        if (lastFilledCount >= 0 && filled != lastFilledCount)
            OnSegmentStepChanged?.Invoke();
        lastFilledCount = filled;
        UpdateToggleSpriteByValue();
    }

    void UpdateToggleSpriteByValue()
    {
        if (toggleImage == null) return;

        if (!isActive)
        {
            if (mutedSprite != null)
                toggleImage.sprite = mutedSprite;
            return;
        }

        Sprite chosen = null;
        if (value <= 0.1f)
            chosen = volumeLowSprite;
        else if (value <= 0.6f)
            chosen = volumeMidSprite;
        else
            chosen = volumeHighSprite;

        // fall back through the levels if a sprite isn't assigned
        if (chosen == null) chosen = volumeHighSprite;
        if (chosen == null) chosen = volumeMidSprite;
        if (chosen == null) chosen = volumeLowSprite;

        if (chosen != null)
            toggleImage.sprite = chosen;
    }

    public void OnPointerDown(PointerEventData eventData) => HandlePointer(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (allowDrag) HandlePointer(eventData);
    }

    private void HandlePointer(PointerEventData eventData)
    {
        // first check if toggle button was provided -- let the button handle clicks so
        // it doesn't interfere with other UI elements.
        if (toggleButton != null && parentCanvas != null && EventSystem.current != null)
        {
            // perform a UI raycast at the pointer position to see if the top-most UI element
            // is the toggle button. If so, let the button handle the event.
            var gr = parentCanvas.GetComponent<GraphicRaycaster>();
            if (gr != null)
            {
                PointerEventData ped = new PointerEventData(EventSystem.current)
                {
                    position = eventData.position
                };

                var hits = new List<RaycastResult>();
                gr.Raycast(ped, hits);
                if (hits.Count > 0)
                {
                    var go = hits[0].gameObject;
                    if (go == toggleButton.gameObject || go.transform.IsChildOf(toggleButton.transform))
                    {
                        ToggleButtonClicked();
                        return;
                    }
                }
            }
        }

        // Before handling, perform a global EventSystem raycast and attempt to pick
        // a RaycastResult that belongs to our canvas or this slider. Use that
        // result's screenPosition so we follow the EventSystem's canonical mapping
        // (this helps when other systems offset the raw mouse position).
        Vector2 screenPosToUse = eventData.position;
        if (EventSystem.current != null)
        {
            PointerEventData globalPed = new PointerEventData(EventSystem.current) { position = eventData.position };
            var globalHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(globalPed, globalHits);

            RaycastResult? chosen = null;

            // Prefer the first hit that belongs to our canvas
            foreach (var h in globalHits)
            {
                var c = h.gameObject.GetComponentInParent<Canvas>();
                if (c == parentCanvas)
                {
                    chosen = h;
                    break;
                }
            }

            // If none on our canvas, prefer hits that are this slider or its toggle
            if (chosen == null)
            {
                foreach (var h in globalHits)
                {
                    var go = h.gameObject;
                    if (go == gameObject || go.transform.IsChildOf(transform)
                        || (toggleButton != null && (go == toggleButton.gameObject || go.transform.IsChildOf(toggleButton.transform))))
                    {
                        chosen = h;
                        break;
                    }
                }
            }

            if (chosen != null)
            {
                // keep using the raw event position for conversion but ensure the
                // camera used for ScreenPointToLocalPointInRectangle matches the
                // EventSystem's camera (eventData.pressEventCamera) below. This
                // avoids mixing screenPosition spaces coming from other canvases.
            }
            else
            {
                // If there were hits but none relevant, bail out to let other UI handle it
                if (globalHits.Count > 0)
                    return;
            }
        }

        // Fallback: compute pointer position against our rect. Use canvas camera for correct conversion.
        // Prefer the camera provided by the EventSystem for this pointer (pressEventCamera).
        // Fallback to the canvas world camera, then Camera.main. For ScreenSpaceOverlay
        // canvases pass null as the camera.
        Camera useCam = null;
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            useCam = null;
        }
        else
        {
            useCam = eventData.pressEventCamera ?? parentCanvas?.worldCamera ?? Camera.main;
        }

        // If the canvas has a scaling mode (e.g. CanvasScaler) that maps screen
        // pixels to canvas units, we need to account for that. Convert the
        // screen point into the canvas' local space using RectTransformUtility
        // with the canvas camera (or null for overlay). This gives us a local
        // point relative to the slider rect which we can map to 0..1 correctly.
        if (segments.Count > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosToUse, useCam, out Vector2 local))
        {
            RectTransform firstSeg = (RectTransform)segments[0].transform;
            RectTransform lastSeg = (RectTransform)segments[segments.Count - 1].transform;
            float segMinX = firstSeg.localPosition.x + firstSeg.rect.xMin;
            float segMaxX = lastSeg.localPosition.x + lastSeg.rect.xMax;

            float normalizedX = Mathf.InverseLerp(segMinX, segMaxX, local.x);
            if (!isActive) SetActive(true);
            SetValue(Mathf.Clamp01(normalizedX));
        }
    }

    public void SetActive(bool active)
    {
        if (isActive == active) return;
        isActive = active;
        if (!isActive)
        {
            storedValue = value;
            if (muteOnDisable)
                SetValue(0f, false);
        }
        else
        {
            if (muteOnDisable)
                SetValue(storedValue, false);
        }

        UpdateToggleSpriteByValue();
        OnValueChanged?.Invoke(value);
    }

    void ToggleButtonClicked()
    {
        SetActive(!isActive);
        OnMuteToggled?.Invoke();
    }
}