using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// 입력 & 포인터 라이프사이클
/// - Pointer Down → 대상 선택
/// - Drag 중일 때 오브젝트 이동
/// - Pointer Up → 드래그 종료
/// - 롱프레스 → 편집모드 진입
/// </summary>
public partial class EditModeController
{
    #region ===== Pointer Lifecycle (Tick) =====

    /// <summary>한 프레임 안에서 포인터의 전체 흐름 처리</summary>
    private void HandlePointerLifecycle()
    {
        // Down
        if (IsPointerDownThisFrame() && !IsPointerOverUI())
            OnPointerDown();

        if (!pointerDown) return;

        // Hold / Drag
        OnPointerHeldOrDragged();

        // Up
        if (IsPointerUpThisFrame())
            OnPointerUp();
    }

    #endregion


    #region ===== Pointer Down / Held / Up =====

    /// <summary>포인터가 눌렸을 때 초기화</summary>
    private void OnPointerDown()
    {
        pointerDown = true;
        pressScreenPos = GetPointerScreenPos();

        if (!ScreenPosValid(pressScreenPos))
        {
            pointerDown = false;
            return;
        }

        // 눌린 지점에서 드래그 가능한 오브젝트가 있는지
        pressedHitTarget = RaycastDraggable(pressScreenPos);
        movePlaneReady = false;

        // 이미 편집모드라면 눌린 걸 선택
        if (IsEditMode && pressedHitTarget)
            SelectTarget(pressedHitTarget);

        // 롱프레스 준비 (편집모드가 아니고, 특정 대상 위에서 눌렸을 때만)
        longPressArmed = false;
        longPressTimer = 0f;
        if (!IsEditMode && longPressTarget)
        {
            var hit = RaycastTransform(pressScreenPos);
            if (hit == longPressTarget)
            {
                longPressArmed = true;
                longPressStartPos = pressScreenPos;
            }
        }

        startedOnDraggable = IsEditMode && pressedHitTarget;

        isDragging = false;
        movedDuringDrag = false;
        currentPlacementValid = true;
    }

    /// <summary>포인터를 누른 상태에서 움직이는 동안</summary>
    private void OnPointerHeldOrDragged()
    {
        // 편집모드 + 드래그 가능한 곳에서 시작 + 실제로 움직임
        if (IsEditMode && startedOnDraggable && CurrentTarget && IsPointerMoving())
        {
            // 🔒 집은 이동 금지 (0,0,0 고정 정책)
            if (IsHome(CurrentTarget))
                return;

            // 드래그 시작 진입
            if (!isDragging)
            {
                isDragging = true;
                BlockOrbit = true; // 카메라 회전 막기

                // 드래그 시작 시점 스냅 (Undo용)
                var snap = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };
                lastBeforeDrag = snap;

                PrepareMovePlane();
                actionToolbar?.Hide();
            }

            var sp = GetPointerScreenPos();
            if (!ScreenPosValid(sp)) return;

            // 실제 이동은 Drag 파셜에서
            DragMove(sp);
        }
    }

    /// <summary>포인터를 뗐을 때</summary>
    private void OnPointerUp()
    {
        pointerDown = false;

        // 롱프레스 해제
        longPressArmed = false;
        longPressTimer = 0f;

        // 드래그가 끝났다면 마무리
        if (isDragging)
        {
            isDragging = false;
            BlockOrbit = false;
            FinishDrag();
        }

        movedDuringDrag = false;
        startedOnDraggable = false;

        // 다시 툴바 위치 갱신
        if (IsEditMode && CurrentTarget)
            UpdateToolbar();
    }

    #endregion


    #region ===== Long Press to Enter Edit Mode =====

    /// <summary>길게 눌러서 편집모드로 들어가는 처리</summary>
    private void HandleLongPress()
    {
        if (!longPressArmed || IsEditMode || !pointerDown) return;

        Vector2 cur = GetPointerScreenPos();
        if (!ScreenPosValid(cur))
        {
            longPressArmed = false;
            return;
        }

        // 롱프레스 중 흔들림이 너무 크면 취소
        if ((cur - longPressStartPos).sqrMagnitude > longPressSlopPixels * longPressSlopPixels)
        {
            longPressArmed = false;
            return;
        }

        longPressTimer += Time.unscaledDeltaTime;
        if (longPressTimer >= longPressSeconds)
        {
            // 편집모드 진입
            longPressArmed = false;
            SetEditMode(true, keepTarget: true);

            if (longPressTarget)
                SelectTarget(longPressTarget);
        }
    }

    #endregion


    #region ===== Orbit Block Maintenance =====

    /// <summary>드래그가 끝나면 자동으로 오비트 차단 해제</summary>
    private void MaintainOrbitBlockFlag()
    {
        if (!isDragging && BlockOrbit)
            BlockOrbit = false;
    }

    #endregion


    #region ===== Static Input Helpers =====

    private static Vector2 GetPointerScreenPos()
    {
        if (TouchES.activeTouches.Count > 0)
            return TouchES.activeTouches[0].screenPosition;

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private static bool IsPointerDownThisFrame()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Began)
                return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool IsPointerUpThisFrame()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
        {
            var ph = TouchES.activeTouches[i].phase;
            if (ph == UnityEngine.InputSystem.TouchPhase.Ended ||
                ph == UnityEngine.InputSystem.TouchPhase.Canceled)
                return true;
        }
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }

    private static bool IsPointerMoving()
    {
        for (int i = 0; i < TouchES.activeTouches.Count; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                return true;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return Mouse.current.delta.ReadValue().sqrMagnitude > 0f;

        return false;
    }

    private static bool ScreenPosValid(Vector2 sp)
    {
        if (float.IsNaN(sp.x) || float.IsNaN(sp.y) || float.IsInfinity(sp.x) || float.IsInfinity(sp.y))
            return false;
        return sp.x >= 0 && sp.y >= 0 && sp.x <= Screen.width && sp.y <= Screen.height;
    }

    private static bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        Vector2 pos = GetPointerScreenPos();
        var data = new PointerEventData(EventSystem.current) { position = pos };
        var results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }

    #endregion


    #region ===== Raycast Helpers =====

    private Transform RaycastDraggable(Vector2 screenPos)
    {
        if (!cam) return null;
        if (!ScreenPosValid(screenPos)) return null;

        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableMask)
            ? hit.transform
            : null;
    }

    private Transform RaycastTransform(Vector2 screenPos)
    {
        if (!cam) return null;
        if (!ScreenPosValid(screenPos)) return null;

        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0)
            ? hit.transform
            : null;
    }

    #endregion
}
