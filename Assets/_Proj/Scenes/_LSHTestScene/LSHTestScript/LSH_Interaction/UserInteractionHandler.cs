using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 일단 먼저 해볼 것이 클릭 시 특정 애니메이션 동작
/// 결국 드래그는 클릭 1초 후 실행 되는 것 이니 bool 변수는 1개만 있어도?
/// </summary>
/// 

public class UserInteractionHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private IInteractable interactable;
    private IDraggable draggable;
    private ILongPressable longPressable;

    private bool isPressing = false;
    private bool isDragging = false;
    private float pressTime = 0f;
    private Vector3 startPos;


    private void Awake()
    {
        interactable = GetComponent<IInteractable>();
        draggable = GetComponent<IDraggable>();
        longPressable = GetComponent<ILongPressable>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        draggable?.OnDragStart(eventData.position);
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        //Vector3 worldPos = eventData.pointerCurrentRaycast.worldPosition;
        draggable?.OnDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        //Vector3 setPos = eventData.pointerCurrentRaycast.worldPosition;
        //transform.position = setPos;
        draggable?.OnDragEnd(eventData.position);
        isDragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPressing || isDragging) return;
        interactable.OnInteract();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        pressTime = Time.time;
        StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
    }

    private IEnumerator CheckLongPress()
    {
        while (isPressing)
        {
            if (Time.time - pressTime >= 0.15f)
            {
                longPressable?.OnLongPress();
                isPressing = false;
            }
            yield return null;
        }
    }
}
