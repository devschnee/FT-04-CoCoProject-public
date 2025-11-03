using UnityEngine;

/// <summary>
/// EditModeController - Toolbar / Selection 파트
/// (partial 분리용)
/// 
/// 요구사항
/// 1. 편집모드에서 "처음" 오브젝트를 선택하면 InfoPanel 이 자동으로 떠 있어야 한다.
///    → 그래야 설명 버튼을 처음 눌렀을 때도 바로 반응하는 것처럼 보임
/// 2. 그 다음에 "다른" 오브젝트를 선택하면 InfoPanel 은 닫힌다.
///    → 오브젝트가 바뀌면 설명도 초기화
/// 3. 설명 버튼(툴바 Info)을 누르면 현재 선택된 오브젝트 정보만 토글
/// 4. 인벤에서 꺼낸 오브젝트면 OK/Cancel 이 있는 툴바, 씬에 있는 거면 기본 툴바
/// </summary>
public partial class EditModeController
{
    /// <summary>
    /// InfoPanel 을 우리가 "자동으로" 한 번이라도 띄웠는지
    /// (처음 선택에서만 자동으로 띄우고 그 다음부턴 안 띄우려고)
    /// </summary>
    private bool infoPanelAutoOpened = false;

    /// <summary>
    /// 오브젝트 선택 로직
    /// </summary>
    public void SelectTarget(Transform t)
    {
        bool targetChanged = (CurrentTarget != t);

        // 1) 이전 대상 하이라이트 해제
        if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var prev))
        {
            prev.SetInvalid(false);
            prev.SetHighlighted(false);
        }

        // 2) 새 대상 설정
        CurrentTarget = t;

        // 3) 새 대상 하이라이트
        if (CurrentTarget && CurrentTarget.TryGetComponent<Draggable>(out var now))
        {
            now.SetInvalid(false);
            now.SetHighlighted(true);
        }

        // 4) InfoPanel 처리
        var panel = InfoPanel.FindInScene();

        if (IsEditMode && CurrentTarget)
        {
            // 4-1) 아직 한 번도 자동으로 안 켜봤으면 → 지금 선택된 애 정보로 한 번 켠다
            if (!infoPanelAutoOpened && panel)
            {
                ShowInfoOfCurrentTarget(panel);
                infoPanelAutoOpened = true;
            }
            // 4-2) 그 다음부터는 "다른 오브젝트"로 바뀌면 닫아준다
            else if (targetChanged && panel && panel.IsVisible)
            {
                panel.Hide();
            }
        }
        else
        {
            // 편집모드가 아니면 그냥 닫아둠
            if (panel && panel.IsVisible)
                panel.Hide();
        }

        // 5) 툴바 / Undo 갱신
        UpdateToolbar();
        UpdateUndoUI();
    }

    /// <summary>
    /// 현재 선택된 오브젝트의 Deco 정보를 InfoPanel에 보여준다.
    /// (형식 맞춰서 Show)
    /// </summary>
    private void ShowInfoOfCurrentTarget(InfoPanel panel)
    {
        if (!CurrentTarget)
        {
            panel.Show("정보 없음", "선택된 오브젝트가 없습니다.");
            return;
        }

        var tag = CurrentTarget.GetComponentInParent<PlaceableTag_Deco>()
               ?? CurrentTarget.GetComponentInChildren<PlaceableTag_Deco>();

        if (tag == null)
        {
            panel.Show("정보 없음", "이 오브젝트에는 PlaceableTag_Deco(decoId)가 없습니다.");
            return;
        }

        if (DecoInventoryRuntime.I == null || DecoInventoryRuntime.I.DB == null)
        {
            panel.Show($"Deco {tag.decoId}", "인벤토리/데이터베이스를 찾지 못했습니다.");
            return;
        }

        var db = DecoInventoryRuntime.I.DB;
        var data = db.decoList.Find(d => d != null && d.deco_id == tag.decoId);

        if (data == null)
        {
            panel.Show($"Deco {tag.decoId}", "데이터베이스에 이 decoId가 없습니다.");
            return;
        }

        var title = string.IsNullOrEmpty(data.deco_name) ? $"Deco {data.deco_id}" : data.deco_name;
        var desc = string.IsNullOrEmpty(data.deco_desc) ? "설명이 없습니다." : data.deco_desc;

        panel.Show(title, desc);
    }

    // ─────────────────────────────────────────────────────
    // 툴바 표시
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 현재 선택/편집 상태에 맞게 툴바를 보여준다
    /// </summary>
    private void UpdateToolbar()
    {
        if (!actionToolbar) return;

        // 편집모드가 아니거나 타겟이 없으면 숨김
        if (!IsEditMode || !CurrentTarget)
        {
            actionToolbar.Hide();
            return;
        }

        // 인벤에서 꺼낸 임시 오브젝트냐?
        bool isInventoryTemp = IsInventoryTempObject(CurrentTarget);

        if (isInventoryTemp)
            ShowToolbar_ForInventoryTemp(CurrentTarget);
        else
            ShowToolbar_ForSceneObject(CurrentTarget);
    }

    /// <summary>
    /// 씬에 원래 있던 오브젝트용 툴바
    /// </summary>
    private void ShowToolbar_ForSceneObject(Transform target)
    {
        if (!actionToolbar) return;

        actionToolbar.Show(
            target: target,
            worldCamera: cam,
            onInfo: OnToolbarInfo,
            onRotate: OnToolbarRotate,
            onInventory: OnToolbarStore,
            onOk: null,
            onCancel: null
        );
    }

    /// <summary>
    /// 인벤에서 막 꺼낸 임시 오브젝트용 툴바 (OK/Cancel 있음)
    /// </summary>
    private void ShowToolbar_ForInventoryTemp(Transform target)
    {
        if (!actionToolbar) return;

        actionToolbar.Show(
            target: target,
            worldCamera: cam,
            onInfo: OnToolbarInfo,
            onRotate: OnToolbarRotate,
            onInventory: OnToolbarStore,
            onOk: OnToolbarOk,
            onCancel: OnToolbarCancel
        );
    }

    // ─────────────────────────────────────────────────────
    // 툴바 버튼 콜백
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Info 버튼: 현재 선택된 오브젝트 정보 토글
    /// - 패널은 이미 처음에 한 번 자동으로 떠 있어야 하므로 여기서는 토글만
    /// </summary>
    private void OnToolbarInfo()
    {
        if (!CurrentTarget) return;

        var panel = InfoPanel.FindInScene();
        if (!panel)
        {
            Debug.LogWarning("[EditModeController] InfoPanel을 씬에서 찾지 못했습니다.");
            return;
        }

        var tag = CurrentTarget.GetComponentInParent<PlaceableTag_Deco>()
               ?? CurrentTarget.GetComponentInChildren<PlaceableTag_Deco>();

        if (tag == null)
        {
            panel.Toggle("정보 없음", "이 오브젝트에는 PlaceableTag_Deco(decoId)가 없습니다.");
            return;
        }

        if (DecoInventoryRuntime.I == null || DecoInventoryRuntime.I.DB == null)
        {
            panel.Toggle($"Deco {tag.decoId}", "인벤토리/데이터베이스를 찾지 못했습니다.");
            return;
        }

        var db = DecoInventoryRuntime.I.DB;
        var data = db.decoList.Find(d => d != null && d.deco_id == tag.decoId);

        if (data == null)
        {
            panel.Toggle($"Deco {tag.decoId}", "데이터베이스에 이 decoId가 없습니다.");
            return;
        }

        var title = string.IsNullOrEmpty(data.deco_name) ? $"Deco {data.deco_id}" : data.deco_name;
        var desc = string.IsNullOrEmpty(data.deco_desc) ? "설명이 없습니다." : data.deco_desc;

        panel.Toggle(title, desc);
    }

    /// <summary>
    /// Rotate 버튼: 90도 회전 + 겹치면 롤백 + Undo 기록
    /// </summary>
    private void OnToolbarRotate()
    {
        if (!CurrentTarget) return;

        // Undo용 원본 저장
        var originalSnap = new Snap
        {
            pos = CurrentTarget.position,
            rot = CurrentTarget.rotation
        };

        // 90도 회전
        CurrentTarget.Rotate(0f, 90f, 0f, Space.World);

        // 회전했는데 겹치면 원복
        if (OverlapsOthers(CurrentTarget))
        {
            CurrentTarget.position = originalSnap.pos;
            CurrentTarget.rotation = originalSnap.rot;

            if (CurrentTarget.TryGetComponent<Draggable>(out var dragFail))
            {
                dragFail.SetInvalid(true);
                dragFail.SetHighlighted(true);
            }

            Debug.Log("[Rotate] 겹쳐서 회전을 취소했습니다.");
            return;
        }

        // Undo 스택에 기록
        var stack = GetOrCreateHistory(CurrentTarget);
        stack.Push(originalSnap);
        TrimHistoryIfNeeded(stack);
        UpdateUndoUI();

        // 시각 피드백
        if (CurrentTarget.TryGetComponent<Draggable>(out var dragOk))
        {
            dragOk.SetInvalid(false);
            dragOk.SetHighlighted(true);
        }

        hasUnsavedChanges = true;
    }

    /// <summary>
    /// 보관 버튼: 씬 오브젝트/임시 오브젝트 상관없이 인벤으로 되돌림
    /// </summary>
    private void OnToolbarStore()
    {
        if (!CurrentTarget) return;

        int decoId = 0;
        var tag = CurrentTarget.GetComponent<PlaceableTag_Deco>();
        if (tag != null) decoId = tag.decoId;

        var go = CurrentTarget.gameObject;

        // 먼저 선택 해제 + 툴바 숨김
        SelectTarget(null);
        actionToolbar?.Hide();

        // 실제 오브젝트 삭제
        Object.Destroy(go);

        // 인벤 수량 +1
        if (decoId != 0 && DecoInventoryRuntime.I != null)
            DecoInventoryRuntime.I.Add(decoId, 1);

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }

    /// <summary>
    /// OK 버튼: 인벤에서 꺼낸 임시 오브젝트를 "진짜 배치"로 확정
    /// </summary>
    private void OnToolbarOk()
    {
        if (!CurrentTarget) return;

        // 마지막으로 유효성 체크
        bool valid = IsOverGround(CurrentTarget.position) && !OverlapsOthers(CurrentTarget);
        if (!valid)
        {
            if (CurrentTarget.TryGetComponent<Draggable>(out var dragBad))
            {
                dragBad.SetInvalid(true);
                dragBad.SetHighlighted(true);
            }
            Debug.Log("[EditModeController] 겹치거나 바닥이 아니어서 확정할 수 없습니다.");
            return;
        }

        // 임시 마커 제거 → 이제 씬 상의 정식 오브젝트
        MarkAsInventoryTemp(CurrentTarget, false);

        // 선택 해제 + 툴바 숨김
        SelectTarget(null);
        actionToolbar?.Hide();

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }

    /// <summary>
    /// Cancel 버튼: 인벤에서 꺼낸 걸 다시 인벤으로
    /// </summary>
    private void OnToolbarCancel()
    {
        if (!CurrentTarget) return;

        int decoId = 0;
        var tag = CurrentTarget.GetComponent<PlaceableTag_Deco>();
        if (tag != null) decoId = tag.decoId;

        var go = CurrentTarget.gameObject;

        // 선택 해제 + 툴바 숨김
        SelectTarget(null);
        actionToolbar?.Hide();

        // 실제 삭제
        Object.Destroy(go);

        // 인벤 수량 롤백
        if (decoId != 0 && DecoInventoryRuntime.I != null)
            DecoInventoryRuntime.I.Add(decoId, 1);

        hasUnsavedChanges = true;
        pendingFromInventory = null;
    }

    // ─────────────────────────────────────────────────────
    // 인벤 임시 마커 유틸
    // ─────────────────────────────────────────────────────

    /// <summary>이 오브젝트가 인벤에서 막 꺼낸 임시 오브젝트인지</summary>
    private bool IsInventoryTempObject(Transform tr)
    {
        if (!tr) return false;
        return tr.gameObject.GetComponent<InventoryTempMarker>() != null;
    }

    /// <summary>임시 마커 붙이기/제거</summary>
    private void MarkAsInventoryTemp(Transform tr, bool on)
    {
        if (!tr) return;
        var m = tr.GetComponent<InventoryTempMarker>();
        if (on)
        {
            if (!m) tr.gameObject.AddComponent<InventoryTempMarker>();
        }
        else
        {
            if (m) Object.Destroy(m);
        }
    }
}
