using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    void Update()
    {
        //#if UNITY_EDITOR
        //        Touch touch = new();
        //        Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        //        if (Mouse.current.leftButton.wasPressedThisFrame)
        //        {
        //            touch.phase = TouchPhase.Began;
        //            touch.position = pos;

        //        }
        //        if (Mouse.current.leftButton.isPressed)
        //        {
        //            touch.phase = TouchPhase.Moved;
        //            touch.position = pos;
        //        }
        //        if (Mouse.current.leftButton.wasReleasedThisFrame)
        //        {
        //            touch.phase = TouchPhase.Ended;
        //            touch.position = pos;
        //        }

        //        if (touch.phase == TouchPhase.Began) //터치가 시작될 때
        //        {
        //            //가상 조이스틱 전체를 터치 위치로 옮겨야 함.
        //            transform.position = pos;
        //            Drag(pos);
        //        }
        //        else if (touch.phase <= TouchPhase.Stationary) //를 제외하고 터치가 이동중/정지중일 때
        //        {
        //            //가상 조이스틱 핸들을 터치 위치로 옮기되, 범위를 넘어가지 않게 하면 됨.
        //            Drag(pos);
        //        }
        //        else //까지 제외하고 터치가 종료/취소될 때
        //        {
        //            ResetJoystick();
        //        }

        //#elif UNITY_ANDROID
        //if (Touchscreen.current == null || Touchscreen.current.touches.Count < 1)
        //{
        //    Debug.Log("터치 없음");
        //    return;
        //}

        var touch = Touchscreen.current.touches[0];
        //터치 위치로 레이를 쏴서, UI요소가 있으면 리턴시키기 근데이제 조이스틱 자체는 제외해줘야 함
        if (touch.press.wasReleasedThisFrame)//터치가 종료/취소될 때
        {
            ResetJoystick();
        }
        if (touch.press.isPressed) //를 제외하고 터치가 이동중/정지중일 때
        {
            //터치 시작 위치로 레이를 쏴서, UI요소가 하나라도 검출되면 리턴
            List<RaycastResult> results = new();
            
            PointerEventData data = new(EventSystem.current);
            data.position = touch.startPosition.ReadValue();
            EventSystem.current.RaycastAll(data, results);
            foreach (var r in results)
            {
                if (r.gameObject)
                {
                    return;
                }
            }

            transform.position = Vector2.Distance(transform.position, touch.startPosition.ReadValue()) < 150 ? transform.position : touch.startPosition.ReadValue();

            //여기까지 왔다면, 터치 시작 위치가 UI요소 위가 아닌 것임.
            //transform.position = touch.startPosition.ReadValue();

            //가상 조이스틱 핸들을 터치 위치로 옮기되, 범위를 넘어가지 않게 하면 됨.
            Drag((Vector3)touch.position.ReadValue() - transform.position);
        }
        
       // TODO : 두 손가락으로 터치하면 플레이어의 입력을 0으로 막고, 이 상태에서 드래그 한다면 TwoFingerDrag()를 호출해야 함.

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

    //HACK: 1028 - 강욱: Drag()메서드 오버로드합니다. Vector2를 매개변수로 갖게 하여 터치된 위치를 기준으로 작동하도록 합니다.
    public void Drag(Vector2 pos)
    {
        //RectTransform bgRect = bg.rectTransform;
        //Vector2 pos;
        //if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out pos)) return;

        // 핸들 이동반경 제한
        Vector2 clamped = Vector2.ClampMagnitude(pos, moveRange);
        handle.rectTransform.anchoredPosition = clamped;

        Vector2 inputNormal = clamped.sqrMagnitude > 0.0001f ? (clamped / moveRange) : Vector2.zero;
        InputDir = new Vector3(inputNormal.x, 0, inputNormal.y);
    }

    // TODO : 두 손가락으로 터치한 상태로 드래그 할 경우, 카메라가 드래그하는 방향, 드래그 하는 만큼의 -방향으로 이동돼야 함. 이때, 플레이어가 화면 밖으로 벗어날 정도로 움직이면 안 됨.(현재 플레이어는 카메라의 정중앙에 위치하도록 되어 있음.). CamControl.cs에서 두 손가락 터치가 입력될 경우 Cam의 타겟을 플레이어에서 끊고 주변을 둘러볼 수 있게 해야 함. 손가락이 하나라도 떨어지면 다시 코코두기를 타겟팅.
    public void TwoFingerDrag()
    {

    }

    public void ResetJoystick()
    {
        InputDir = Vector3.zero;
        handle.rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchoredPosition = initPos;
    }
}