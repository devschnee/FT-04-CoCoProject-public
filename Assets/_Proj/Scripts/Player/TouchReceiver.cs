using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TouchReceiver : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Joystick joystick;

    // 현재 트래킹 중인 포인터 ID. 멀티터치 상황에서 이 터치만 추적
    private int trackingPointerId = -1;

    //  NOTE : 스크린 전체를 덮는 투명한 Image 컴포넌트 필요.
    void Awake()
    {
        if (joystick == null)
        {
            Debug.LogError("조이스틱 연결 안 됨");
            enabled = false;
        }
    }

    // 스크린 어디든 터치하면 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        // 다른 손가락 추적중이면 무시
        if (trackingPointerId != -1) return;

        trackingPointerId = eventData.pointerId;
        joystick.MoveToTouch(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(eventData.pointerId != trackingPointerId) return;
        joystick.Drag(eventData);
    }

    // 터치 끝날 때
    public void OnPointerUp(PointerEventData eventData)
    {
        // 트래킹 터치 해제
        if (eventData.pointerId != trackingPointerId) return;
        joystick.ResetJoystick();

        // 트래킹 종료
        trackingPointerId = -1;
    }

}