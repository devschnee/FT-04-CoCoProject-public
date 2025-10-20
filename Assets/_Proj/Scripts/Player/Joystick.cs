using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour
{
    [SerializeField] private Image bg;
    [SerializeField] private Image handle;
    public float moveRange = 75f; // 핸들 움직임 최대 반경(pixel)

    // 조이스틱 기본 위치
    private Vector3 initPos;
    public Vector3 InputDir { get; private set; }

    // 조이스틱 터치 위치 계산에 사용
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initPos = rectTransform.anchoredPosition;
        InputDir = Vector3.zero;
    }

    public void MoveToTouch(PointerEventData eventData)
    {
        Vector2 pos;
        
        var parent = rectTransform.parent as RectTransform;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out pos))
        {
            rectTransform.anchoredPosition = pos;
        }
        Drag(eventData);
    }

    // 터치가 드래그 될 때
    public void Drag(PointerEventData eventData)
    {
        RectTransform bgRect = bg.rectTransform;
        Vector2 pos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out pos)) return;

        // 핸들 이동반경 제한
        Vector2 clamped = Vector2.ClampMagnitude(pos, moveRange);
        handle.rectTransform.anchoredPosition = clamped;

        Vector2 inputNormal = clamped.sqrMagnitude > 0.0001f ? (clamped / moveRange) : Vector2.zero;
        InputDir = new Vector3(inputNormal.x, 0, inputNormal.y);
    }

    public void ResetJoystick()
    {
        InputDir = Vector3.zero;
        handle.rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchoredPosition = initPos;
    }
}