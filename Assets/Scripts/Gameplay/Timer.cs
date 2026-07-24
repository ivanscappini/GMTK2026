using Hermes;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    public static Timer Instance { get; private set; }

    [SerializeField] private float timerStartTime = 10f;
    [SerializeField] private float playbackSpeed = 1f;
    [HideInInspector] public UnityEvent<float> timerEvent;
    [HideInInspector] public UnityEvent countdownEnd;
    [HideInInspector] public UnityEvent countdownStart;
    [HideInInspector] public event Action OnTimerChanged;
    public event Action OnMoving;
    public event Action OnStoppedMoving;
    
    public float TimerStartTime
    {
        get => timerStartTime;
        set => timerStartTime = value;
    }
    public float PlaybackSpeed
    {
        get => playbackSpeed;
        set => playbackSpeed = Mathf.Max(0f, value);
    }
    public float TheTime
    {
        get => _time;
        set
        {
            _time = Mathf.Max(value, 0f);
            timerEvent?.Invoke(_time);
        }
    }
    private float _time;
    private Coroutine _countdownCoroutine;

    [SerializeField] private float timerDuration = 0.5f; // Duration of the timer in seconds
    private float timer;

    private bool isMoving = false;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        FPMovement.Instance.OnMoving += StartedMoving;
        FPMovement.Instance.OnStationary += StoppedMoving;
        StartCountdown(); //temporarily here - tie to event
    }

    private void Update()
    {
        if (isMoving && timer != 0)
        {
            timer = 0;
            OnMoving?.Invoke();
        }

        if (!isMoving)
        {
            StartTimer();
        }
    }

    private void StartCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
        }
        _countdownCoroutine = StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        countdownStart?.Invoke();

        TheTime = timerStartTime;

        while (TheTime > 0f)
        {
            TheTime -= Time.deltaTime * PlaybackSpeed;
            yield return null;
        }
        StopCountdown();
    }

    private void StopCountdown()
    {
        countdownEnd?.Invoke();
        _countdownCoroutine = null;
    }

    public void AddTime(float value)
    {
        TheTime += value;
        OnTimerChanged?.Invoke();
    }

    public void SubtractTime(float value)
    {
        TheTime -= value;
        OnTimerChanged?.Invoke();
    }

    public void SetTimerSpeed(float value)
    {
        PlaybackSpeed = value;
    }

    public void PauseCountdown()
    {
        SetTimerSpeed(0f);
    }

    private void StartedMoving()
    {
        isMoving = true;

        SetTimerSpeed(1f);
    }

    private void StoppedMoving()
    {
        isMoving = false;
    }

    private void StartTimer()
    {
        if (timer < timerDuration)
        {
            timer += Time.deltaTime;
        }
        else
        {
            timer = 0;
            OnStoppedMoving?.Invoke();
            SetTimerSpeed(2f);
        }
    }

    public void SetZeroTimer()
    {
        StopCoroutine(_countdownCoroutine);
        TheTime = 0f;
        StopCountdown();
    }
}