using UnityEngine;
using TMPro;
using Hermes;

// Simple debug UI to display current horizontal speed from FPMovement.
// Assign an optional TextMeshProUGUI in the inspector. If none is assigned,
// an OnGUI label will be drawn instead.
public class DebugSpeedUI : MonoBehaviour
{
    [Header("Optional UI")] 
    [SerializeField] private TMP_Text speedText;

    [Header("OnGUI fallback")]
    [SerializeField] private bool useOnGUIIfNoText = true;
    [SerializeField] private Vector2 guiPosition = new Vector2(10f, 10f);
    [SerializeField] private GUIStyle guiStyle;

    void Update()
    {
        if (speedText != null)
        {
            if (FPMovement.Instance != null)
            {
                var mv = FPMovement.Instance;
                if (mv != null)
                {
                    var hv = mv.HorizontalVelocity;
                    float total = hv.magnitude;                              // total ground speed
                    float forward = Vector3.Dot(hv, mv.transform.forward);  // forward progress (+/-)
                    float strafe = Vector3.Dot(hv, mv.transform.right);     // lateral speed
                    speedText.text = string.Format("Total={0:0.00}  Forward={1:0.00}  Strafe={2:0.00}", total, forward, strafe);
                }
            }
            else
            {
                speedText.text = "FPMovement.Instance == null";
            }
        }
    }

    void OnGUI()
    {
        if (!useOnGUIIfNoText) return;
        if (speedText != null) return; // prefer TMP if assigned

        if (FPMovement.Instance == null)
        {
            GUI.Label(new Rect(guiPosition.x, guiPosition.y, 300f, 20f), "FPMovement.Instance == null");
            return;
        }

        if (guiStyle == null)
        {
            guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = 16;
            guiStyle.normal.textColor = Color.white;
        }

        var mv = FPMovement.Instance;
        float horizontal = mv.HorizontalSpeed;
        float forward = Vector3.Dot(mv.ForwardVelocity, mv.transform.forward);
        float strafe = Vector3.Dot(mv.HorizontalVelocity, mv.transform.right);

        string txt = string.Format("H={0:0.00}  F={1:0.00}  S={2:0.00}", horizontal, forward, strafe);
        GUI.Label(new Rect(guiPosition.x, guiPosition.y, 400f, 30f), txt, guiStyle);
    }
}
