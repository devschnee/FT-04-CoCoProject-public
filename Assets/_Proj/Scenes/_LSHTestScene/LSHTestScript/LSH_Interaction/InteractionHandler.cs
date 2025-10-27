using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 일단 먼저 해볼 것이 클릭 시 특정 애니메이션 동작
/// 
/// </summary>
/// 

public class InteractionHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private IInteractable interactable;
    private IDraggable draggable;
    private ILongPressable longPressable;

    private bool isDragging = false;
    private bool isPressing = false;
    private float pressTime = 0f;

    private void Awake()
    {
        interactable = GetComponent<IInteractable>();
        draggable = GetComponent<IDraggable>();
        longPressable = GetComponent<ILongPressable>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        Vector3 worldPos = eventData.pointerCurrentRaycast.worldPosition;
        draggable?.OnDrag(worldPos);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;
        interactable.OnInteract();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = false;
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
            if (Time.time - pressTime >= 1f)
            {
                longPressable?.OnLongPress();
                isPressing = false;
            }
            yield return null;
        }
    }
}
