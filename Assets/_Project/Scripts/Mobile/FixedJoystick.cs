using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FixedJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image handle;
    [SerializeField] private float handleRange = 1f;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private Vector2 inputVector;

    private void Start()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (handle == null)
            handle = transform.GetChild(0).GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out position);

        inputVector = position / (background.rectTransform.sizeDelta.x * handleRange);
        inputVector = Vector2.ClampMagnitude(inputVector, 1f);

        if (handle != null)
            handle.rectTransform.anchoredPosition = inputVector * (background.rectTransform.sizeDelta.x * handleRange);

        Horizontal = inputVector.x;
        Vertical = inputVector.y;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handle != null)
            handle.rectTransform.anchoredPosition = Vector2.zero;
        Horizontal = 0;
        Vertical = 0;
    }
}