using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드래그 이동, 그리드 스냅, 겹침 체크, 바닥(Layer) 체크, Undo 스택
/// </summary>
public partial class EditModeController
{
    // 드래그 시작 직전 스냅
    private Snap? lastBeforeDrag;

    // ── Overlap 임시 버퍼(무할당) ──────────────────────────────────────
    // 필요 시 자동 확장
    private static Collider[] _ovlBuf = new Collider[64];

    #region ===== Move Plane & Drag =====

    /// <summary>드래그용 평면 생성(Y 고정)</summary>
    private void PrepareMovePlane()
    {
        float y = fixedY;
        if (lockYToInitial && CurrentTarget)
            y = CurrentTarget.position.y;

        movePlaneY = y;
        movePlane = new Plane(Vector3.up, new Vector3(0f, movePlaneY, 0f));
        movePlaneReady = true;
    }

    /// <summary>스크린 좌표 기반으로 실제 월드 이동</summary>
    private void DragMove(Vector2 screenPos)
    {
        if (!cam) return;
        if (!ScreenPosValid(screenPos)) return;
        if (!movePlaneReady) PrepareMovePlane();

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!movePlane.Raycast(ray, out float enter)) return;

        Vector3 hit = ray.GetPoint(enter);
        hit.y = movePlaneY;

        // 그리드 스냅
        if (snapToGrid)
            hit = SnapToGrid(hit);

        if (!CurrentTarget || CurrentTarget.position == hit) return;

        CurrentTarget.position = hit;
        movedDuringDrag = true;

        // 배치 유효성 검사(빠른 탈출 + 무할당)
        bool onGround = IsOverGround(hit);
        bool noOverlap = !OverlapsOthers(CurrentTarget);
        bool valid = onGround && noOverlap;
        currentPlacementValid = valid;

        if (CurrentTarget.TryGetComponent<Draggable>(out var drag))
        {
            drag.SetInvalid(!valid);
            drag.SetHighlighted(true);
        }
    }

    /// <summary>드래그 종료 시 호출</summary>
    private void FinishDrag()
    {
        if (!IsEditMode || !CurrentTarget) return;

        // 1) 유효하지 않은 위치면 원래대로
        if (!currentPlacementValid)
        {
            if (lastBeforeDrag.HasValue)
            {
                CurrentTarget.position = lastBeforeDrag.Value.pos;
                CurrentTarget.rotation = lastBeforeDrag.Value.rot;
                Physics.SyncTransforms(); // 물리/충돌 동기화
            }

            if (CurrentTarget.TryGetComponent<Draggable>(out var drag0))
            {
                drag0.SetInvalid(false);
                drag0.SetHighlighted(true);
            }
        }
        // 2) 유효 + 실제로 움직였으면 Undo 기록
        else if (movedDuringDrag && lastBeforeDrag.HasValue)
        {
            var stack = GetOrCreateHistory(CurrentTarget);
            stack.Push(lastBeforeDrag.Value);
            TrimHistoryIfNeeded(stack);

            hasUnsavedChanges = true;
            UpdateUndoUI();
        }

        movedDuringDrag = false;
        lastBeforeDrag = null;
        currentPlacementValid = true;
    }

    #endregion ===== Move Plane & Drag =====


    #region ===== Grid & Ground =====

    /// <summary>그리드 스냅</summary>
    private Vector3 SnapToGrid(Vector3 world)
    {
        float Snap(float v, float origin) => Mathf.Round((v - origin) / gridSize) * gridSize + origin;

        world.x = Snap(world.x, gridOrigin.x);
        world.z = Snap(world.z, gridOrigin.z);
        return world;
    }

    /// <summary>해당 위치가 Ground 레이어 위인지</summary>
    private bool IsOverGround(Vector3 worldPos)
    {
        if (!requireGround) return true;

        Vector3 origin = new Vector3(worldPos.x, worldPos.y + groundProbeUp, worldPos.z);
        float dist = groundProbeUp + groundProbeDown;

        return Physics.Raycast(origin, Vector3.down, out _, dist, groundMask, QueryTriggerInteraction.Ignore);
    }

    #endregion ===== Grid & Ground =====


    #region ===== Overlap Check (NonAlloc) =====

    /// <summary>
    /// 다른 오브젝트와 겹치는지 검사 (무할당 버전)
    /// - OverlapBoxNonAlloc 로 후보 수집 → 필요 시 버퍼 확장
    /// - 실제 충돌은 ComputePenetration 으로 확정
    /// </summary>
    private bool OverlapsOthers(Transform t)
    {
        if (!t) return false;

        var myCols = t.GetComponentsInChildren<Collider>();
        if (myCols == null || myCols.Length == 0) return false;
        if (!TryGetCombinedBoundsFromColliders(myCols, out Bounds myBounds)) return false;

        var center = myBounds.center;
        var half = myBounds.extents;

        // 후보 수집 (draggableMask만)
        int count = Physics.OverlapBoxNonAlloc(
            center,
            half,
            _ovlBuf,
            Quaternion.identity,
            draggableMask,
            QueryTriggerInteraction.Ignore
        );

        // 버퍼가 모자라면 2배로 확장해 한 번 더
        if (count == _ovlBuf.Length)
        {
            _ovlBuf = new Collider[_ovlBuf.Length * 2];
            count = Physics.OverlapBoxNonAlloc(
                center, half, _ovlBuf, Quaternion.identity, draggableMask, QueryTriggerInteraction.Ignore);
        }

        if (count <= 0) return false;

        for (int i = 0; i < count; i++)
        {
            var other = _ovlBuf[i];
            if (!other || !other.enabled) continue;
            if (IsSameRootOrChild(t, other.transform)) continue;

            // 트리거 제외
            if (other.isTrigger) continue;

            // 세부 충돌 체크
            for (int m = 0; m < myCols.Length; m++)
            {
                var my = myCols[m];
                if (!my || !my.enabled || my.isTrigger) continue;

                if (Physics.ComputePenetration(
                        my, my.transform.position, my.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out _, out float dist))
                {
                    if (dist > overlapEpsilon)
                        return true;
                }
            }
        }

        return false;
    }
    private bool OverlapsWithLayerMask(Transform t, LayerMask mask)
    {
        if (!t) return false;

        var myCols = t.GetComponentsInChildren<Collider>();
        if (myCols == null || myCols.Length == 0) return false;
        if (!TryGetCombinedBoundsFromColliders(myCols, out Bounds myBounds)) return false;

        var center = myBounds.center;
        var half = myBounds.extents;

        int count = Physics.OverlapBoxNonAlloc(
            center,
            half,
            _ovlBuf,
            Quaternion.identity,
            mask,
            QueryTriggerInteraction.Ignore
        );

        if (count == _ovlBuf.Length)
        {
            _ovlBuf = new Collider[_ovlBuf.Length * 2];
            count = Physics.OverlapBoxNonAlloc(
                center, half, _ovlBuf, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
        }

        if (count <= 0) return false;

        for (int i = 0; i < count; i++)
        {
            var other = _ovlBuf[i];
            if (!other || !other.enabled) continue;
            if (IsSameRootOrChild(t, other.transform)) continue;
            if (other.isTrigger) continue;

            for (int m = 0; m < myCols.Length; m++)
            {
                var my = myCols[m];
                if (!my || !my.enabled || my.isTrigger) continue;

                if (Physics.ComputePenetration(
                        my, my.transform.position, my.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out _, out float dist))
                {
                    if (dist > overlapEpsilon)
                        return true;
                }
            }
        }

        return false;
    }

    // 🔥 새로 추가: 집 설치 시 막는 레이어(Editable, InLobbyObject) 전용
    private bool OverlapsHomeBlockers(Transform t)
    {
        // Layer 이름으로 마스크 구성 (프로젝트에서 레이어 이름 정확히 "Editable", "InLobbyObject"인지 확인)
        int mask = LayerMask.GetMask("Editable", "InLobbyObject");
        if (mask == 0) return false; // 레이어 없으면 그냥 통과

        return OverlapsWithLayerMask(t, mask);
    }
    private static bool IsSameRootOrChild(Transform root, Transform other)
        => other == root || (other && other.IsChildOf(root));

    private static bool TryGetCombinedBoundsFromColliders(Collider[] cols, out Bounds combined)
    {
        combined = default;
        bool hasAny = false;
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (!c || !c.enabled) continue;

            var b = c.bounds;
            if (!hasAny) { combined = b; hasAny = true; }
            else combined.Encapsulate(b);
        }
        return hasAny;
    }

    #endregion ===== Overlap Check (NonAlloc) =====


    #region ===== Undo =====

    public void UndoLastMove()
    {
        if (!CurrentTarget) return;

        if (history.TryGetValue(CurrentTarget, out var stack) && stack.Count > 0)
        {
            Snap prev = stack.Peek();

            // 현재값 백업
            Vector3 curPos = CurrentTarget.position;
            Quaternion curRot = CurrentTarget.rotation;

            // 되돌리기
            CurrentTarget.position = prev.pos;
            CurrentTarget.rotation = prev.rot;
            Physics.SyncTransforms();

            // 되돌렸더니 또 겹치면 취소
            if (OverlapsOthers(CurrentTarget))
            {
                CurrentTarget.position = curPos;
                CurrentTarget.rotation = curRot;
                Physics.SyncTransforms();

                if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
                {
                    dragFail.SetInvalid(true);
                    dragFail.SetHighlighted(true);
                }
                Debug.Log("[Undo] 이전 상태가 겹쳐서 되돌릴 수 없습니다.");
                return;
            }

            stack.Pop();

            if (CurrentTarget.TryGetComponent<Draggable>(out var dragOk))
            {
                dragOk.SetInvalid(false);
                dragOk.SetHighlighted(true);
            }

            hasUnsavedChanges = true;
            Debug.Log("[Undo] 되돌리기 성공");
        }

        UpdateUndoUI();
    }

    public void ClearCurrentHistory()
    {
        if (!CurrentTarget) return;
        if (history.ContainsKey(CurrentTarget))
            history[CurrentTarget].Clear();
        UpdateUndoUI();
    }

    private Stack<Snap> GetOrCreateHistory(Transform t)
    {
        if (!history.TryGetValue(t, out var stack))
        {
            stack = new Stack<Snap>(8);
            history[t] = stack;
        }
        return stack;
    }

    private void TrimHistoryIfNeeded(Stack<Snap> stack)
    {
        if (undoMax <= 0) return;
        if (stack.Count <= undoMax) return;

        // 최신(Top) 유지, 오래된 것부터 제거
        var tmp = stack.ToArray();        // [old ... new]
        System.Array.Reverse(tmp);        // [new ... old]
        int keep = Mathf.Min(undoMax, tmp.Length);

        stack.Clear();
        for (int i = 0; i < keep; i++)
            stack.Push(tmp[i]);
    }

    private void UpdateUndoUI()
    {
        if (!undoButton) return;

        if (!IsEditMode)
        {
            undoButton.interactable = false;
            return;
        }

        bool canUndo = false;
        if (CurrentTarget && history.TryGetValue(CurrentTarget, out var stack))
            canUndo = stack != null && stack.Count > 0;

        undoButton.interactable = canUndo;
    }

    #endregion ===== Undo =====
}
