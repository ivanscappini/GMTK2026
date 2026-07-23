using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    public static Timer Instance { get; private set; }
    
    [SerializeField] private float timerStartTime = 10f;
    [HideInInspector] public UnityEvent<float> timerEvent;
    [HideInInspector] public UnityEvent countdownEnd;
    [HideInInspector] public UnityEvent countdownStart;

    private float _time;
    private Coroutine _countdownCoroutine;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    private void Start()
    {
        StartCountdown(); //temporarily here - tie to event
    }

    private void StartCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
        }
        _countdownCoroutine = StartCoroutine(Countdown());
    }

    public void AddTime(float value)
    {
        TheTime += value;
    }

    public void SubtractTime(float value)
    {
        TheTime -= value;
    }
    
    private IEnumerator Countdown()
    {
        countdownStart?.Invoke();
        
        TheTime = timerStartTime;

        while (TheTime > 0f)
        {
            TheTime -= Time.deltaTime;
            yield return null; 
        }
        StopCountdown();
    }

    private void StopCountdown()
    {
        countdownEnd?.Invoke();
        _countdownCoroutine = null;
    }
}