using Hermes;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class MonsterBehaviour : MonoBehaviour
{
    [Header("Transparency Settings")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;

    SplineAnimate splineAnimator;

    Material mat;

    private bool isFaster = false;

    private void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        splineAnimator = GetComponent<SplineAnimate>();

        Timer.Instance.OnMoving += ResetSpeed;
        Timer.Instance.OnStoppedMoving += DoubleSpeed;
        Timer.Instance.OnTimerChanged += RecalculateElapsedTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player has entered the monster's trigger area!");
            StartCoroutine(ChangeTransparency(other.gameObject));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player has exited the monster's trigger area!");
            StopCoroutine(ChangeTransparency(other.gameObject));
            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0f);
        }
    }

    IEnumerator ChangeTransparency(GameObject obj)
    {
        while (true)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            float alpha = 1 - Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));

            //Debug.Log(alpha);

            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, alpha);
            yield return null;
        }
    }

    private void DoubleSpeed()
    {
        if (isFaster) return;
        isFaster = true;
        splineAnimator.Duration /= 2f;
        splineAnimator.ElapsedTime /= 2f;   // Adjust ElapsedTime to maintain the same position on the spline
    }

    private void ResetSpeed()
    {
        if (!isFaster) return;
        isFaster = false;
        splineAnimator.Duration *= 2f;
        splineAnimator.ElapsedTime *= 2f;   // Adjust ElapsedTime to maintain the same position on the spline
    }

    private void RecalculateElapsedTime()
    {

        float pickedTime = (Timer.Instance.TimerStartTime - splineAnimator.ElapsedTime) - Timer.Instance.TheTime;
        splineAnimator.ElapsedTime += pickedTime;
    }

}