using Hermes;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnterFinalBox : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject canvasToHide;

    [SerializeField] private FPController controller;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Time.timeScale = 0;

        controller.LookEnabled = false;

        canvas.SetActive(true);
        canvasToHide.SetActive(false);
    }
}
