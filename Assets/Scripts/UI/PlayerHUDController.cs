using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerHUDController : MonoBehaviour
{
    public static PlayerHUDController Instance { get; private set; }

    [SerializeField] private Timer CountdownTimer;
    [SerializeField] private TMP_Text timerText;
    [Header("Feedback")]
    [SerializeField] private TMP_Text floatingTextPrefab;
    [SerializeField] private Color addColor = Color.green;
    [SerializeField] private Color removeColor = Color.red;
    [Header("Audio")]
    [SerializeField] private AudioClip timerChangeSound;
    [SerializeField] private float floatDuration = 0.8f;
    [SerializeField] private float floatDistance = 0.01f;
    [SerializeField] private float colorPulseDuration = 0.6f;
    [Header("Slam Popup")]
    [SerializeField] private float slamDistance = 40f;
    [SerializeField] private float slamDuration = 0.1f;
    [SerializeField] private float floatingStartOffset = 5f;
    [SerializeField] private float floatingFontSize = 48f;
    [SerializeField] private float timerScaleAmount = 1.4f;
    [SerializeField] private float timerScaleDuration = 0.22f;
    [SerializeField] private float floatingLingerDuration = 0.1f;
    [SerializeField] private float impactOffset = 5f; // how far below the timer the popup should land on impact

    private Canvas _parentCanvas;
    private RectTransform _canvasRect;
    private Color _defaultTimerColor;
    private Coroutine _pulseCoroutine;
    private Coroutine _animateCoroutine;
    private Coroutine _scaleCoroutine;
    private AudioSource _audioSource;

    // internal displayed time used for animations
    private float _displayTime = 0f;
    private bool _isAnimatingTime = false;
    private float _lastKnownTime = 0f;

    private void Update()
    {
        if (CountdownTimer == null || timerText == null) return;
        // keep display time in sync when not animating
        if (!_isAnimatingTime)
        {
            _displayTime = Mathf.Max(0f, CountdownTimer.TheTime);
        }

        WriteTimeToText(_displayTime);
    }

    private void Awake()
    {
        Instance = this;
        if (timerText != null)
            _defaultTimerColor = timerText.color;

        // auto-find timer instance if not assigned in inspector
        if (CountdownTimer == null && Timer.Instance != null)
            CountdownTimer = Timer.Instance;

        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas == null)
        {
            _parentCanvas = FindObjectOfType<Canvas>();
        }

        if (_parentCanvas != null)
            _canvasRect = _parentCanvas.GetComponent<RectTransform>();

        // ensure an AudioSource is available for playing timer change sounds
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        if (_audioSource != null)
            _audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (CountdownTimer != null)
        {
            CountdownTimer.OnTimerChanged += HandleTimerChanged;
            _lastKnownTime = CountdownTimer.TheTime;
            _displayTime = _lastKnownTime;
        }
    }

    private void OnDisable()
    {
        if (CountdownTimer != null)
        {
            CountdownTimer.OnTimerChanged -= HandleTimerChanged;
        }
    }

    private void WriteTimeToText(float t)
    {
        int totalSeconds = Mathf.FloorToInt(t);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int centis = Mathf.FloorToInt((t - totalSeconds) * 100f);

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centis);
    }

    private void HandleTimerChanged()
    {
        if (CountdownTimer == null) return;

        float newTime = CountdownTimer.TheTime;
        float delta = newTime - _lastKnownTime;
        _lastKnownTime = newTime;
        // animate the timer display briefly (pause real countdown while animating)
        if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
        _animateCoroutine = StartCoroutine(AnimateTimeChange(_displayTime, newTime, Mathf.Clamp(Mathf.Abs(delta) * 0.05f + 0.25f, 0.15f, 0.6f)));
    }

    IEnumerator PulseColor(Color targetColor, float duration)
    {
        if (timerText == null) yield break;

        Debug.Log($"PulseColor start color={targetColor}");
        float t = 0f;
        timerText.color = targetColor;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            timerText.color = Color.Lerp(targetColor, _defaultTimerColor, t / duration);
            yield return null;
        }
        timerText.color = _defaultTimerColor;
    }

    IEnumerator AnimateTimeChange(float from, float to, float duration)
    {
        if (CountdownTimer == null) yield break;

        float originalSpeed = CountdownTimer.PlaybackSpeed;
        CountdownTimer.SetTimerSpeed(0f);

        _isAnimatingTime = true;
        // no pending pulses here; impact callback will handle visual pulse/scale
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, timer / duration);
            _displayTime = Mathf.Lerp(from, to, u);
            yield return null;
        }
        _displayTime = to;
        _isAnimatingTime = false;

        CountdownTimer.SetTimerSpeed(originalSpeed);
    }

    // Called externally to show floating +/- text. worldPosition is the pickup world position.
    // onImpact is invoked when the floating text reaches the timer.
    public void ShowTimeChangePopup(float delta, Vector3 worldPosition, System.Action onImpact = null)
    {
        // determine impact color early so it can be used for styling
        Color impactColor = delta >= 0f ? addColor : removeColor;
        if (_canvasRect == null)
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas == null) _parentCanvas = FindObjectOfType<Canvas>();
            if (_parentCanvas != null) _canvasRect = _parentCanvas.GetComponent<RectTransform>();
        }

        if (_canvasRect == null)
        {
            Debug.LogWarning("PlayerHUDController: no Canvas found for floating text popup.");
            return;
        }

        Camera cam = _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? (_parentCanvas.worldCamera != null ? _parentCanvas.worldCamera : Camera.main) : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, cam, out localPoint);

        TMP_Text inst = null;
        RectTransform rt = null;

        // prefer parenting to the same parent as the timer text so anchored positions match
        RectTransform timerParent = timerText != null && timerText.rectTransform.parent != null ? timerText.rectTransform.parent as RectTransform : _canvasRect;

        if (floatingTextPrefab != null)
        {
            inst = Instantiate(floatingTextPrefab, timerParent);
            rt = inst.GetComponent<RectTransform>();
            // position will be set relative to timer parent later
            // ensure prefab styling matches settings
            inst.fontSize = floatingFontSize;
            inst.alignment = TextAlignmentOptions.Center;
            // match font to the timer text if available
            if (timerText != null && timerText.font != null)
                inst.font = timerText.font;
        }
        else
        {
            // create a temporary TextMeshProUGUI element if no prefab assigned
            var go = new GameObject("FloatingTimeText", typeof(RectTransform));
            go.transform.SetParent(timerParent, false);
            rt = go.GetComponent<RectTransform>();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            // prefer using the timer's font so popups visually match the timer
            if (timerText != null && timerText.font != null)
                tmp.font = timerText.font;
            else if (TMPro.TMP_Settings.defaultFontAsset != null)
                tmp.font = TMPro.TMP_Settings.defaultFontAsset;
            tmp.fontSize = 36f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.color = Color.white;

            inst = tmp;
        }

        string sign = delta >= 0f ? "+" : "-";
        inst.text = string.Format("{0}{1:0.0}", sign, Mathf.Abs(delta));

        // style
        inst.fontSize = floatingFontSize;
        // make sure the TMP instance uses a unique material so color changes don't affect other text
        if (inst.fontMaterial != null)
        {
            // assign an instance of the material so we can tweak face/outline without touching the shared material
            var mat = new Material(inst.fontMaterial);
            // try to set common TMP shader properties used for face/outline
            if (mat.HasProperty("_FaceColor")) mat.SetColor("_FaceColor", impactColor);
            if (mat.HasProperty("_OutlineColor")) mat.SetColor("_OutlineColor", impactColor);
            inst.fontMaterial = mat;
        }
        // disable vertex gradients which can override the vertex color
        inst.enableVertexGradient = false;
        inst.color = impactColor;
        // set pivot so anchoredPosition is relative to bottom-center
        if (rt != null) rt.pivot = new Vector2(0.5f, 0f);
        // bring to front so it is not occluded
        inst.transform.SetAsLastSibling();

        // compute target anchored position for the timer text in same parent space
        Vector2 timerScreen = RectTransformUtility.WorldToScreenPoint(cam, timerText.transform.position);
        Vector2 timerAnchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(timerParent, timerScreen, cam, out timerAnchored);

        // start below the timer using the timer text height so it sits visibly under it
        float timerHeight = 0f;
        if (timerText != null && timerText.rectTransform != null)
            timerHeight = timerText.rectTransform.rect.height;

        // start further below the timer so it's clearly visible
        float extraDrop = 20f;
        Vector2 startAnchored = timerAnchored + Vector2.down * (floatingStartOffset + timerHeight * 1.2f + extraDrop);
        rt.anchoredPosition = startAnchored;

        // adjust target so the slam lands slightly below the timer's anchored position (use timer height)
        Vector2 impactTarget = timerAnchored + Vector2.down * (impactOffset + timerHeight * 0.6f + extraDrop * 0.5f);

        Debug.Log($"ShowTimeChangePopup delta={delta} impactColor={impactColor} start={startAnchored} impact={impactTarget} parent={timerParent.name}");

        StartCoroutine(BobThenSlam(inst, rt, startAnchored, impactTarget, floatingLingerDuration, 10f, 0.12f, slamDuration, onImpact, impactColor));
    }

    IEnumerator BobThenSlam(TMP_Text inst, RectTransform rt, Vector2 startAnchored, Vector2 impactAnchored, float lingerDuration, float downDistance, float downDuration, float slamDuration, System.Action onImpact, Color impactColor)
    {
        // linger
        float lingerT = 0f;
        while (lingerT < lingerDuration)
        {
            lingerT += Time.unscaledDeltaTime;
            yield return null;
        }

        // move down slightly
        Vector2 downPos = startAnchored + Vector2.down * downDistance;
        float t = 0f;
        while (t < downDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / downDuration);
            rt.anchoredPosition = Vector2.Lerp(startAnchored, downPos, u);
            yield return null;
        }

        // slam up to impactAnchored
        yield return StartCoroutine(SlamAndFade(inst, rt, downPos, impactAnchored, slamDuration, () =>
        {
            // visual pulse and scale
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseColor(impactColor, colorPulseDuration));
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(PulseScale(timerScaleAmount, timerScaleDuration));

            // invoke the onImpact callback provided by caller (e.g. TimePickup)
            onImpact?.Invoke();
        }));
    }

    IEnumerator FlyToTimerAndFade(TMP_Text inst, RectTransform rt, Vector2 endAnchored, float duration)
    {
        if (timerText == null) yield break;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = endAnchored;
        float timer = 0f;
        Color orig = inst.color;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float u = timer / duration;
            rt.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, u));
            inst.color = new Color(orig.r, orig.g, orig.b, 1f - u);
            yield return null;
        }
        Destroy(inst.gameObject);
    }

    IEnumerator SlamAndFade(TMP_Text inst, RectTransform rt, Vector2 downAnchored, Vector2 timerAnchored, float duration, System.Action onImpact = null)
    {
        // linger at full alpha for a moment
        Color orig = inst.color;
        float linger = floatingLingerDuration;
        float t = 0f;
        while (t < linger)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // then slam up to the timerAnchored
        float slamTimer = 0f;
        Vector2 start = downAnchored;
        Vector2 end = timerAnchored;
        while (slamTimer < duration)
        {
            slamTimer += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(slamTimer / duration);
            float ease = 1f - (1f - u) * (1f - u);
            rt.anchoredPosition = Vector2.Lerp(start, end, ease);
            yield return null;
        }

        // Invoke the impact callback (which will run pulses and apply the time change)
        Debug.Log($"Impact at color={orig}");
        // play configured sound on impact
        if (_audioSource != null && timerChangeSound != null)
        {
            _audioSource.PlayOneShot(timerChangeSound);
        }
        onImpact?.Invoke();

        // start fading after impact
        float fadeTimer = 0f;
        float fadeDuration = 0.25f;
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float fu = Mathf.Clamp01(fadeTimer / fadeDuration);
            inst.color = new Color(orig.r, orig.g, orig.b, 1f - fu);
            yield return null;
        }

        Destroy(inst.gameObject);
    }

    IEnumerator PulseScale(float targetScale, float duration)
    {
        if (timerText == null) yield break;
        RectTransform rt = timerText.rectTransform;
        Vector3 original = rt.localScale;
        float half = duration * 0.5f;

        float t = 0f;
        // scale up
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float u = t / half;
            rt.localScale = Vector3.Lerp(original, original * targetScale, Mathf.SmoothStep(0f, 1f, u));
            yield return null;
        }

        t = 0f;
        // scale back
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float u = t / half;
            rt.localScale = Vector3.Lerp(original * targetScale, original, Mathf.SmoothStep(0f, 1f, u));
            yield return null;
        }

        rt.localScale = original;
    }
}
