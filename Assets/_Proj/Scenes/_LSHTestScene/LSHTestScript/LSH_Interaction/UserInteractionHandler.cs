using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// �ϴ� ���� �غ� ���� Ŭ�� �� Ư�� �ִϸ��̼� ����
/// �ᱹ �巡�״� Ŭ�� 1�� �� ���� �Ǵ� �� �̴� bool ������ 1���� �־?
/// </summary>
/// 

public class UserInteractionHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ILobbyInteractable interactable;
    private ILobbyDraggable draggable;
    private ILobbyPressable longPressable;

    private bool isPressing = false;
    private bool isDragging = false;
    private float pressTime = 0f;
    private Vector3 startPos;


    private void Start()
    {
        var mono = gameObject.GetComponent<BaseLobbyCharacterBehaviour>();
        if (interactable == null) interactable = mono as ILobbyInteractable;
        if (draggable == null) draggable = mono as ILobbyDraggable;
        if (longPressable == null) longPressable = mono as ILobbyPressable;
        if (interactable == null) Debug.Log("interactable null");
        if (draggable == null) Debug.Log("draggable null");
        if (longPressable == null) Debug.Log("longPressable null");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        draggable?.OnLobbyBeginDrag(eventData.position);
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        //Vector3 worldPos = eventData.pointerCurrentRaycast.worldPosition;
        draggable?.OnLobbyDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPressing) return;
        //Vector3 setPos = eventData.pointerCurrentRaycast.worldPosition;
        //transform.position = setPos;
        draggable?.OnLobbyEndDrag(eventData.position);
        isDragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPressing || isDragging) return;
        interactable.OnLobbyInteract();
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
            if (Time.time - pressTime >= 0.4f)
            {
                longPressable?.OnLobbyPress();
                isPressing = false;
            }
            yield return null;
        }
    }
}
