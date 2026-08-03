using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Attach to a UI element representing a menu option. It will tint the
// target TMP_Text grey by default and white on hover.
public class MenuOptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color hoverColor = Color.white;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TMP_Text>(true);

        if (targetText != null)
            targetText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.color = normalColor;
    }
}
