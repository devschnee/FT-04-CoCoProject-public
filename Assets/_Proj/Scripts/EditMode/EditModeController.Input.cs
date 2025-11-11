using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchES = UnityEngine.InputSystem.EnhancedTouch.Touch;

public partial class EditModeController
{
    // ===== EnhancedTouch 안전 헬퍼 =====
    private static bool IsETEnabled() => EnhancedTouchSupport.enabled;
    private static int ActiveTouchCount() => IsETEnabled() ? TouchES.activeTouches.Count : 0;

    private static Vector2 FirstTouchPos()
    {
        // EnhancedTouch가 켜져 있고 터치가 있을 때만 접근
        return (IsETEnabled() && TouchES.activeTouches.Count > 0)
            ? TouchES.activeTouches[0].screenPosition
            : Vector2.zero;
    }

    #region ===== Pointer Lifecycle (Tick) =====

    private void HandlePointerLifecycle()
    {
        if (IsPointerDownThisFrame() && !IsPointerOverUI())
            OnPointerDown();

        if (!pointerDown) return;

        OnPointerHeldOrDragged();

        if (IsPointerUpThisFrame())
            OnPointerUp();
    }

    #endregion

    #region ===== Pointer Down / Held / Up =====

    private void OnPointerDown()
    {
        pointerDown = true;
        pressScreenPos = GetPointerScreenPos();

        if (!ScreenPosValid(pressScreenPos))
        {
            pointerDown = false;
            return;
        }

        pressedHitTarget = RaycastDraggable(pressScreenPos);
        movePlaneReady = false;

        if (IsEditMode && pressedHitTarget)
            SelectTarget(pressedHitTarget);

        // 롱프레스 준비
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

    private void OnPointerHeldOrDragged()
    {
        if (IsEditMode && startedOnDraggable && CurrentTarget && IsPointerMoving())
        {
            if (IsHome(CurrentTarget))
                return;

            if (!isDragging)
            {
                isDragging = true;
                BlockOrbit = true;

                var snap = new Snap { pos = CurrentTarget.position, rot = CurrentTarget.rotation };
                lastBeforeDrag = snap;

                PrepareMovePlane();
                actionToolbar?.Hide();
            }

            var sp = GetPointerScreenPos();
            if (!ScreenPosValid(sp)) return;

            DragMove(sp);
        }
    }

    private void OnPointerUp()
    {
        pointerDown = false;

        longPressArmed = false;
        longPressTimer = 0f;

        if (isDragging)
        {
            isDragging = false;
            BlockOrbit = false;
            FinishDrag();
        }

        movedDuringDrag = false;
        startedOnDraggable = false;

        if (IsEditMode && CurrentTarget)
            UpdateToolbar();
    }

    #endregion

    #region ===== Long Press to Enter Edit Mode =====

    private void HandleLongPress()
    {
        if (!longPressArmed || IsEditMode || !pointerDown) return;

        Vector2 cur = GetPointerScreenPos();
        if (!ScreenPosValid(cur))
        {
            longPressArmed = false;
            return;
        }

        if ((cur - longPressStartPos).sqrMagnitude > longPressSlopPixels * longPressSlopPixels)
        {
            longPressArmed = false;
            return;
        }

        longPressTimer += Time.unscaledDeltaTime;
        if (longPressTimer >= longPressSeconds)
        {
            longPressArmed = false;
            SetEditMode(true, keepTarget: true);

            if (longPressTarget)
                SelectTarget(longPressTarget);
        }
    }

    #endregion

    #region ===== Orbit Block Maintenance =====

    private void MaintainOrbitBlockFlag()
    {
        if (!isDragging && BlockOrbit)
            BlockOrbit = false;
    }

    #endregion

    #region ===== Static Input Helpers =====

    private static Vector2 GetPointerScreenPos()
    {
        // 터치 우선, 없으면 마우스
        if (ActiveTouchCount() > 0)
            return FirstTouchPos();

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private static bool IsPointerDownThisFrame()
    {
        // 터치 Began 체크 (EnhancedTouch가 꺼져 있으면 0회전)
        for (int i = 0, c = ActiveTouchCount(); i < c; i++)
            if (TouchES.activeTouches[i].phase == UnityEngine.InputSystem.TouchPhase.Began)
                return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool IsPointerUpThisFrame()
    {
        for (int i = 0, c = ActiveTouchCount(); i < c; i++)
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
        for (int i = 0, c = ActiveTouchCount(); i < c; i++)
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
