using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class Joystick : MonoBehaviour
{
    [SerializeField] private Image bg;
    [SerializeField] private Image handle;
    public float moveRange = 75f; // 핸들 움직임 최대 반경(pixel)
    [SerializeField] private Image[] fourSlices; // 조이스틱 4방향 하이라이터

    private static float sharedSnapAngleThreshold = 13;
    private static bool sharedEnhanceFourDir = true;
    private float snapAngleThreshold = 13f;
    private bool enhanceFourDir;

    // 조이스틱 기본 위치
    private Vector3 initPos;
    public Vector3 InputDir { get; private set; }


    // 조이스틱 터치 위치 계산에 사용
    private RectTransform rectTransform;


    // CamControl 참조 및 상태 변수
    private CamControl camCon;
    public bool IsTwoFingerMode { get; private set; } = false;


    private Vector2 lastTwoFingerPos;

    // 패널 켜졌을 떄 조이스틱을 잠그기 위한 변수
    public bool IsLocked { get; set; } = false;

   
    IEnumerator Start()
    {
        snapAngleThreshold = sharedSnapAngleThreshold >= 0 ? sharedSnapAngleThreshold : snapAngleThreshold;
        enhanceFourDir = sharedEnhanceFourDir;
        

        rectTransform = GetComponent<RectTransform>();
        initPos = rectTransform.anchoredPosition;
        InputDir = Vector3.zero;


        camCon = Camera.main?.GetComponent<CamControl>();
        
        
        yield return null;
        onUiSetup?.Invoke(snapAngleThreshold, enhanceFourDir);

        ResetJoystick();
    }


    void Update()
    {
        #region 주석처리된 긴 코드 부분
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
        #endregion

        if (IsLocked)
        {
            ResetJoystick();
            return;
        }

        if (Touchscreen.current == null) return;


        // KHJ - Touch 모드 분기
        int touchCnt = 0;
        var touches = Touchscreen.current.touches;


        // 터치 몇 개 들어왔는지 카운팅
        for (int i = 0; i < touches.Count; i++)
        {
            if (touches[i].press.isPressed)
                touchCnt++;
        }

        // 모드 종료 확인 (터치 개수가 2개 미만이고, 모드가 켜져 있다면 종료하고 카메라 복귀)
        // 모드 종료 조건 (touchCount < 2)을 먼저 확인하고,
        // 모드가 활성화 상태(IsLookAroundMode == true)인지 확인하여 Reset을 실행
        // 두 손가락 터치 로직
        // touchCnt가 0 또는 1이 되었다면 카메라를 즉시 복귀 시켜야 하기 때문에 0, 1보다 반드시 먼저 실행돼야 함.
        if (touchCnt < 2 && IsTwoFingerMode == true)
        {
            ResetTwoFingerMode(); // IsTwoRingerMode = false <- 카메라 플레이어 추적 시작
            //ResetJoystick(); // ResetJoystick은 각 분기에서 한 번씩 처리 하므로 불필요함.
        }


        // 터치 입력이 아무 것도 안 들어왔을 때 조이스틱 리셋
        if (touchCnt == 0)
        {
            ResetJoystick();
            return;
        }

        // 두 손가락 터치 모드 시작 및 드래그
        if (touchCnt == 2)
        {
            // 조이스틱의 UI를 복귀시킴
            handle.rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchoredPosition = initPos;

            // 두 번째 터치가 시작되거나 이미 진행 중일 때
            var touch1 = Touchscreen.current.touches[0];
            var touch2 = Touchscreen.current.touches[1];

            if (touch1.press.isPressed && touch2.press.isPressed)
            {
                // 모드 시작 : 이미 모드가 종료되었거나 새로 시작하는 경우
                // 두 손가락 터치 모드 시작
                if (IsTwoFingerMode == false)
                {
                    IsTwoFingerMode = true;
                    // 두 손가락의 평균 위치를 초기 위치로 설정
                    lastTwoFingerPos = (touch1.position.ReadValue() + touch2.position.ReadValue()) / 2f;

                    // 카메라 플레이어 추적 중단
                    camCon?.SetFollowingPlayer(false);
                }
                // 두 손가락 드래그 처리
                Vector2 currTwoFingerPos = (touch1.position.ReadValue() + touch2.position.ReadValue()) / 2f;
                Vector2 dragDelta = currTwoFingerPos - lastTwoFingerPos;

                camCon?.LookAroundUpdate(dragDelta);
                lastTwoFingerPos = currTwoFingerPos;

                // Input의 입력을 0으로(드래그 할 때 플레이어가 움직이면 안 됨)
                InputDir = Vector3.zero;
                return; // 조이스틱 일반 로직 스킵
            }
        }

        // 일반 조이스틱 터치 로직(한 손가락 터치)
        if (touchCnt == 1)
        {
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
        }
    }

    //public void MoveToTouch(PointerEventData eventData)
    //{
    //    Vector2 pos;

    //    var parent = rectTransform.parent as RectTransform;
    //    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out pos))
    //    {
    //        rectTransform.anchoredPosition = pos;
    //    }
    //    Drag(eventData);
    //}


    // 터치가 드래그 될 때
    public void Drag(PointerEventData eventData)
    {
        RectTransform bgRect = bg.rectTransform;
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out Vector2 pos)) return;


        // 핸들 이동반경 제한
        Vector2 clamped = Vector2.ClampMagnitude(pos, moveRange);
        handle.rectTransform.anchoredPosition = clamped;

        

        Vector2 inputNormal = clamped.sqrMagnitude > 0.0001f ? (clamped / moveRange) : Vector2.zero;

        //핸들 각도 스냅(8방향 기준으로)
        Vector2 snapped = SnapDirection(inputNormal, enhanceFourDir);
        
        InputDir = new Vector3(snapped.x, 0, snapped.y);
    }

    private readonly static Vector2[] eightDir = new Vector2[8]
    {
                    Vector2.up,
                    (Vector2.up + Vector2.right).normalized,
                    Vector2.right,
                    (Vector2.down + Vector2.right).normalized,
                    Vector2.down,
                    (Vector2.down + Vector2.left).normalized,
                    Vector2.left,
                    (Vector2.up + Vector2.left).normalized
    };
    private Vector2 SnapDirection(Vector2 inputVector, bool enhanceFourdir)
    {
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 snapVector = eightDir[i];
                float currentThreshold = snapAngleThreshold;
                if (enhanceFourdir) { if (i % 2 == 0) { currentThreshold *= 2; } }
                if (Vector2.Angle(inputVector, snapVector) < currentThreshold)
                {
                    inputVector = snapVector * inputVector.magnitude;
                    break;
                }
            }

            
        }

        return inputVector;
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

        //핸들 각도 스냅(8방향 기준으로)
        inputNormal = SnapDirection(inputNormal, enhanceFourDir);

        FourDirHighlightUI(InputDir);
        InputDir = new Vector3(inputNormal.x, 0, inputNormal.y);
    }


    // KHJ -  두 손가락으로 터치한 상태로 드래그 할 경우, 카메라가 드래그하는 방향, 드래그 하는 만큼의 -방향으로 이동돼야 함. 이때, 플레이어가 화면 밖으로 벗어날 정도로 움직이면 안 됨.(현재 플레이어는 카메라의 정중앙에 위치하도록 되어 있음.). CamControl.cs에서 두 손가락 터치가 입력될 경우 Cam의 타겟을 플레이어에서 끊고 주변을 둘러볼 수 있게 해야 함. 손가락이 하나라도 떨어지면 다시 코코두기를 타겟팅.
    public void ResetTwoFingerMode()
    {
        IsTwoFingerMode = false;
        InputDir = Vector3.zero; // 조이스틱 재활성화 준비
        lastTwoFingerPos = Vector2.zero;
        camCon?.SetFollowingPlayer(true); // 즉시 플레이어 따라가기
    }

    public void ResetJoystick()
    {
        InputDir = Vector3.zero;
        handle.rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchoredPosition = initPos;
        for (int i = 0; i < fourSlices.Length; i++) fourSlices[i].enabled = false;
    }

    private void FourDirHighlightUI(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
        {
            for (int i = 0; i < fourSlices.Length; i++) fourSlices[i].enabled = false;
            return;
        }

        int idx;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z)) idx = dir.x > 0 ? 1 : 3; // Right 1 Left 3
        else idx = dir.z > 0 ? 0 : 2; // Up 0 Down 2

        for (int i = 0; i < fourSlices.Length; i++) fourSlices[i].enabled = (i == idx);
    }

    public void ApplyOptions(float snapAngleThreshold, bool enhanceFourDir)
    {
        this.snapAngleThreshold = snapAngleThreshold;
        this.enhanceFourDir = enhanceFourDir;

        sharedSnapAngleThreshold = snapAngleThreshold;
        sharedEnhanceFourDir = enhanceFourDir;
    }

    public Action<float, bool> onUiSetup;
}
